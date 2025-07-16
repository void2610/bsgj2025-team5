using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;
using LitMotion;
using System.Threading;
using LitMotion.Extensions;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("必須参照")] [Tooltip("プレイヤーの参照")] [SerializeField]
    private Player player;

    [Tooltip("プレイヤーカメラの参照")] [SerializeField]
    private PlayerCamera playerCamera;

    [Tooltip("UIのキャンバス")] [SerializeField] private Canvas uiCanvas;
    [Tooltip("敵の参照")] [SerializeField] private EnemyAI enemyAI;

    [Header("ゲーム設定")] [SerializeField] private float countDownDuration = 180f;
    [SerializeField] private Vector3 defaultRespawnPosition;

    [Header("SE設定")] [SerializeField] private SeData timePenaltySe;
    [SerializeField] private SeData timeBonusSe;
    [SerializeField] private SeData itemGetSe;
    [SerializeField] private SeData gameClearSe1;
    [SerializeField] private SeData gameClearSe2;

    [Header("クリア演出用設定")] [SerializeField] private GameObject particlePrefab; // パーティクルPrefab
    [SerializeField] private GameObject separatedGashaPrefab; // 2つに分かれたガシャ玉モデルのPrefab

    [SerializeField] private GameObject foxGameObjectPrefab; // ガシャ玉のなかの狐オブジェクトのPrefab

    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";
    private readonly float _fallTimePenalty = 20f;

    private readonly ReactiveProperty<float> _onTimeChangedInternal = new();
    private readonly Subject<float> _onHappenTimePenalty = new();
    private readonly Subject<float> _onHappenTimeBonus = new();
    private readonly ReactiveProperty<int> _itemCount = new(0);

    private Rigidbody _playerRigidbody;
    private Vector3 _respawnPosition;
    private bool _isGameEnded;
    private bool _isGameStarted = false;

    private CancellationTokenSource _clearEffectCts; // LitmotionのTweenを管理するためのCancellationTokenSource

    public ReadOnlyReactiveProperty<float> OnTimeChanged => _onTimeChangedInternal;
    public Observable<float> OnHappenTimePenalty => _onHappenTimePenalty.AsObservable();
    public Observable<float> OnHappenTimeBonus => _onHappenTimeBonus.AsObservable();
    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;
    public Player Player => player;

    public void AddItemCount(Vector3 itemPositon)
    {
        _itemCount.Value++;
        _respawnPosition = itemPositon; //最後に取得したアイテムの位置をリスポーン地点にする
        if (_itemCount.Value >= 5)
        {
            SaveCurrentTime();
            GameClear().Forget();
            return;
        }

        SeManager.Instance.PlaySe(itemGetSe);
    }

    private void SaveCurrentTime()
    {
        PlayerPrefs.SetFloat(REMAINING_TIME_AT_CLEAR, _onTimeChangedInternal.Value);
        PlayerPrefs.Save();
        _isGameEnded = true;
    }

    public void GameOver()
    {
        SceneManager.LoadScene("GameOverScene");
    }

    public void Fall()
    {
        DecreasePenaltyTime(_fallTimePenalty);
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        // 速度と慣性をリセット
        _playerRigidbody = player.GetComponent<Rigidbody>();
        _playerRigidbody.transform.position = _respawnPosition;
        _playerRigidbody.linearVelocity = Vector3.zero;
        _playerRigidbody.angularVelocity = Vector3.zero;
    }

    private void ResetRespawnPosition()
    {
        // 初期リスポーン地点を設定
        _respawnPosition = defaultRespawnPosition;
    }

    private async UniTask GameClear()
    {
        _isGameEnded = true;

        // プレイヤーの動きを完全に停止
        player.StopMovement();
        await UniTask.Delay(100);
        Vector3 currentGashaPosition = player.transform.position; // 後で削除されるガシャ玉の位置を保持するため

        // カメラをプレイヤーの正面に移動させる
        await CameraForwardAsync();

        await UniTask.Delay(500);

        // 力が溜まるSE再生開始
        UniTask powerChargeSeTask = SeManager.Instance.PlaySeAsync(gameClearSe1, pitch: 1.0f, important: true);


        // プレイヤーのガシャ玉を振動させる

        var shakeMotion = LMotion.Shake.Create(0.1f, 0.1f, 1f)
            .WithLoops(-1, LoopType.Flip)
            .WithFrequency(20)
            .WithDampingRatio(0.1f)
            .WithRandomSeed(123)
            .BindToPositionX(player.transform);

        // パーティクル再生開始

        // パーティクル生成位置は振動するPlayerオブジェクトの位置
        GameObject particleInstance = Instantiate(particlePrefab, currentGashaPosition, Quaternion.identity);
        var particleManager = particleInstance.GetComponent<ParticleSystem>();


        // プレイヤーのガシャ玉を削除
        if (player != null)
        {
            MeshRenderer playerMeshRenderer = player.GetComponent<MeshRenderer>();
            if (playerMeshRenderer != null)
            {
                playerMeshRenderer.enabled = false;
            }
        }

        // 2つに分かれたガシャ玉モデルを生成
        GameObject separatedGashaInstance = null;

        // 削除されたPlayer（ガシャ玉）があった位置に生成する
        separatedGashaInstance = Instantiate(separatedGashaPrefab, currentGashaPosition, Quaternion.identity);

        shakeMotion.Cancel(); // 振動を停止

        // 新しいガシャ玉を振動開始
        shakeMotion = LMotion.Shake.Create(0.1f, 0.1f, 1f)
            .WithLoops(-1, LoopType.Flip)
            .WithFrequency(15)
            .WithDampingRatio(0.2f)
            .WithRandomSeed(456)
            .BindToPositionX(separatedGashaInstance.transform);


        await UniTask.Delay(500);

        // パーティクルを停止
        particleManager.Stop();

        await UniTask.Delay(500);

        // 力が溜まるSEが終わるまで待機
        await powerChargeSeTask;


        shakeMotion.Cancel(); // 振動を停止

        // 2秒待機
        await UniTask.Delay(2000);


        // separatedGashaPrefabの子オブジェクトそれぞれに力を加える
        float baseForce = 6f; // 飛ぶ勢いを調整
        float upwardsForceMultiplier = 1.0f; // 上方向への力を調整

        foreach (Transform child in separatedGashaInstance.transform)
        {
            Rigidbody childRb = child.GetComponent<Rigidbody>();

            childRb.isKinematic = false; // 物理演算を有効に
            childRb.useGravity = true; // 重力を使用（必要であれば）

            // ランダムな方向ベクトルを生成
            Vector3 randomHorizontal =
                new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized;
            Vector3 forceDirection = (randomHorizontal + Vector3.up * upwardsForceMultiplier).normalized;

            // 力を加える
            childRb.AddForce(forceDirection * baseForce, ForceMode.Impulse);
            childRb.AddTorque(UnityEngine.Random.insideUnitSphere * 15f, ForceMode.Impulse); // 回転も加える
        }


        // ガシャ玉が割れるSE再生
        await SeManager.Instance.PlaySeAsync(gameClearSe2, pitch: 1.0f, important: true);

        // ちょっと待機
        await UniTask.Delay(2500, cancellationToken: this.GetCancellationTokenOnDestroy());

        // シーン遷移 
        await IrisShot.StartIrisOut(uiCanvas);
        SceneManager.LoadScene("ClearScene");
    }

    private async UniTask CameraForwardAsync()
    {
        // カメラの通常更新を停止
        playerCamera.SetIntroMode(true);

        // 演出開始時のプレイヤーの位置を取得
        var foxPosition = foxGameObjectPrefab.transform.position;
        var foxForward = foxGameObjectPrefab.transform.forward;
        
        // 正面の位置と回転を計算
        var finalCameraPosition = foxPosition - foxForward * 3.5f + Vector3.up;
        var finalCameraRotation = Quaternion.LookRotation(foxPosition - finalCameraPosition);
        
        var startPosition = playerCamera.transform.position;
        var startRotation = playerCamera.transform.rotation;
        
        // 位置と回転のアニメーションを同時実行
        var positionTask = LMotion.Create(startPosition, finalCameraPosition, 3.5f)
            .WithEase(Ease.InOutQuart)
            .Bind(pos => playerCamera.transform.position = pos)
            .ToUniTask();
            
        var rotationTask = LMotion.Create(startRotation, finalCameraRotation, 3.5f)
            .WithEase(Ease.InOutQuart)
            .Bind(rot => playerCamera.transform.rotation = rot)
            .ToUniTask();
        
        // 両方のアニメーションが完了するまで待機
        await UniTask.WhenAll(positionTask, rotationTask);
        
    }

    public void GoToTitleScene()
    {
        GoToTitleSceneAsync().Forget();
    }

    private async UniTask GoToTitleSceneAsync()
    {
        await IrisShot.StartIrisOut(uiCanvas);
        SceneManager.LoadScene("TitleScene");
    }

    public void DecreaseCurrentTime(float v)
    {
        float actualDecreaseAmount = Math.Max(0, v);
        _onTimeChangedInternal.Value -= actualDecreaseAmount;
    }

    private void DecreasePenaltyTime(float v)
    {
        SeManager.Instance.PlaySe(timePenaltySe);
        var actualDecreaseAmount = Math.Max(0, v);
        _onTimeChangedInternal.Value -= actualDecreaseAmount;
        _onHappenTimePenalty.OnNext(v);
    }

    public void IncreaseTime(float amount)
    {
        SeManager.Instance.PlaySe(timeBonusSe);
        var actualIncreaseAmount = Math.Max(0, amount);
        _onTimeChangedInternal.Value =
            Math.Min(_onTimeChangedInternal.Value + actualIncreaseAmount, countDownDuration * 2f);
        _onHappenTimeBonus.OnNext(actualIncreaseAmount);
    }

    protected override void Awake()
    {
        base.Awake();

        // スポーン地点を初期化
        ResetRespawnPosition();
        //タイマーを初期化
        _onTimeChangedInternal.Value = countDownDuration;
        _isGameEnded = false;
    }

    private async UniTaskVoid Start()
    {
        var gameStartSequence = new GameStartSequence(player, playerCamera, uiCanvas);

        player.SetInputEnabled(false);
        await gameStartSequence.StartSequenceAsync();
        player.SetInputEnabled(true);

        // 演出完了後にゲーム開始フラグを設定
        _isGameStarted = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UIManager.Instance.TogglePause();
        }

        // ゲーム開始前または終了後はタイマーを更新しない
        if (!_isGameStarted || _isGameEnded) return;

        DecreaseCurrentTime(Time.deltaTime);
        if (_onTimeChangedInternal.Value <= 0f)
        {
            _onTimeChangedInternal.Value = 0f;
            _isGameEnded = true;
            GameOver();
        }
    }
}
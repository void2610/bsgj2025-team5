using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("必須参照")] 
    [Tooltip("プレイヤーの参照")]
    [SerializeField] private Player player;
    [Tooltip("プレイヤーカメラの参照")] 
    [SerializeField] private PlayerCamera playerCamera;
    [Tooltip("UIのキャンバス")]
    [SerializeField] private Canvas uiCanvas;
    [Tooltip("敵の参照")] 
    [SerializeField] private EnemyAI enemyAI;

    [Header("ゲーム設定")]
    [SerializeField] private float countDownDuration = 180f;
    [SerializeField] private Vector3 defaultRespawnPosition;

    [Header("SE設定")] [SerializeField] private SeData timePenaltySe;
    [SerializeField] private SeData timeBonusSe;
    [SerializeField] private SeData itemGetSe;

    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";
    private const float FALL_TIME_PENALTY = 20f;

    private readonly ReactiveProperty<float> _onTimeChangedInternal = new();
    private readonly Subject<float> _onHappenTimePenalty = new();
    private readonly Subject<float> _onHappenTimeBonus = new();
    private readonly ReactiveProperty<int> _itemCount = new(0);

    private Rigidbody _playerRigidbody;
    private Vector3 _respawnPosition;
    private bool _isGameEnded;
    private bool _isGameStarted = false;

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
        DecreasePenaltyTime(FALL_TIME_PENALTY);
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
        
        // GameClearSequenceのインスタンスを作成
        var gameClearSequence = new GameClearSequence(
            player,
            playerCamera,
            uiCanvas
        );
        
        // クリア演出を実行
        await gameClearSequence.StartSequenceAsync();
        
        // シーン遷移 
        await IrisShot.StartIrisOut(uiCanvas);
        SceneManager.LoadScene("ClearScene");
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
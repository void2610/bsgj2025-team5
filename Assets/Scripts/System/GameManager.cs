using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Tooltip("プレイヤーの参照。ゲーム開始時に設定する必要があります")] [SerializeField]
    private Player player;

    [Tooltip("敵の参照。ゲーム開始時に設定する必要があります")] [SerializeField]
    private EnemyAI enemyAI;

    // カウントダウンタイマーの初期時間
    [SerializeField] private float countDownDuration = 180f;

    // 残り時間。変更を通知するためにReactivePropertyにしている
    private readonly ReactiveProperty<float> _onTimeChangedInternal = new();
    
    // 外部から残り時間を参照するためのプロパティ (読み取り専用)
    public ReadOnlyReactiveProperty<float> OnTimeChanged => _onTimeChangedInternal;

    // 落下ペナルティが発生したことを通知するためのSubject
    private readonly Subject<float> _onHappenTimePenalty = new Subject<float>();

    // 落下ペナルティを外部からはObservable<float>として購読可能にする
    public Observable<float> OnHappenTimePenalty => _onHappenTimePenalty.AsObservable();

    // PlayerPrefsに登録する残り時間のキー
    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";

    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;

    private readonly ReactiveProperty<int> _itemCount = new(0);

    public ReadOnlyReactiveProperty<float> ClosestEnemyDistance { get; private set; }

    public Player Player => player;

    private readonly float _fallTimePenalty = 20f;

    private Rigidbody _playerqRigidbody;

    [SerializeField] private Vector3 defaultRespawnPosition;

    private Vector3 _respawnPosition;

    // ゲーム終了フラグ
    private bool _isGameEnded = false;

    public void AddItemCount(Vector3 itemPositon)
    {
        _itemCount.Value++;
        _respawnPosition = itemPositon; //最後に取得したアイテムの位置をリスポーン地点にする
        if (_itemCount.Value >= 5)
        {
            // クリア時の残りタイムを保存する
            SaveCurrentTime();
            GameClear();
        }
    }

    public void SaveCurrentTime()
    {
        // PlayerPrefsに残り時間を保存
        PlayerPrefs.SetFloat(REMAINING_TIME_AT_CLEAR, _onTimeChangedInternal.Value);
        PlayerPrefs.Save();

        Debug.Log($"クリア時の残り時間としてPlayerPrefsに保存しました: {_onTimeChangedInternal.Value:F2}秒");
        _isGameEnded = true;
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
        SceneManager.LoadScene("GameOverScene");
    }

    public void Fall()
    {
        Debug.Log("Fall method called."); // 追加
        // 残り時間を減らしてプレイヤーをリスポーンさせる
        DecreaseCurrentTime(_fallTimePenalty);
        RespownPlayer();
    }

    private void RespownPlayer()
    {
        // playerのRigidbodyを束縛する
        _playerqRigidbody = player.GetComponent<Rigidbody>();

        Debug.Log($"リスポーン地点: 最後に拾ったアイテムの位置 ({_respawnPosition})");

        // リスポーン地点に移動させる
        _playerqRigidbody.transform.position = _respawnPosition;
        // 速度と慣性をリセット
        _playerqRigidbody.linearVelocity = Vector3.zero;
        _playerqRigidbody.angularVelocity = Vector3.zero;
    }

    private void ResetRespawnPosition()
    {
        // 初期リスポーン地点を設定
        _respawnPosition = defaultRespawnPosition;
        Debug.Log("初期リスポーン位置をセットしました。");
    }

    // アイテムカウントによるゲームクリア処理
    public void GameClear()
    {
        Debug.Log("Game Clear!");
        SceneManager.LoadScene("ClearScene");
    }

    public void GoToTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void DecreaseCurrentTime(float v)
    {
        // 減少量を0以上にクリップ
        float actualDecreaseAmount = Math.Max(0, v);

        _onTimeChangedInternal.Value -= actualDecreaseAmount;

        // 減少量が20f以上の場合にOnHappenTimePenaltyを通知
        if (actualDecreaseAmount >= 20f)
        {
            Debug.Log(
                $"DecreaseCurrentTime: Large penalty of {actualDecreaseAmount} detected. Notifying OnHappenTimePenalty."); // デバッグログ追加
            _onHappenTimePenalty.OnNext(actualDecreaseAmount);
        }
    }

    protected override void Awake()
    {
        base.Awake();

        // R3を使った敵との距離計算
        ClosestEnemyDistance = ObservableExtensions
            .Select(Observable.EveryUpdate(),
                _ => Vector3.Distance(player.transform.position, enemyAI.transform.position))
            .ToReadOnlyReactiveProperty(float.MaxValue);

        // スポーン地点を初期化
        ResetRespawnPosition();
        //タイマーを初期化
        _onTimeChangedInternal.Value = countDownDuration;
        _isGameEnded = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UIManager.Instance.TogglePause();
        }

        if (_isGameEnded) return;
        // 残り時間を数える
        DecreaseCurrentTime(Time.deltaTime);
        if (_onTimeChangedInternal.Value <= 0f)
        {
            _onTimeChangedInternal.Value = 0f;
            Debug.Log("タイマーが0秒になりました");
            _isGameEnded = true;
            GameOver();
        }
    }
}
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("必須参照")]
    [Tooltip("プレイヤーの参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private Player player;
    
    [Tooltip("敵の参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private EnemyAI enemyAI;
    
    [Header("ゲーム設定")]
    [SerializeField] private float countDownDuration = 180f;
    [SerializeField] private Vector3 defaultRespawnPosition;
    
    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";
    private readonly float _fallTimePenalty = 20f;
    
    private readonly ReactiveProperty<float> _onTimeChangedInternal = new();
    private readonly Subject<float> _onHappenTimePenalty = new();
    private readonly ReactiveProperty<int> _itemCount = new(0);
    
    private Rigidbody _playerRigidbody;
    private Vector3 _respawnPosition;
    private bool _isGameEnded;
    
    public ReadOnlyReactiveProperty<float> OnTimeChanged => _onTimeChangedInternal;
    public Observable<float> OnHappenTimePenalty => _onHappenTimePenalty.AsObservable();
    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;
    public ReadOnlyReactiveProperty<float> ClosestEnemyDistance { get; private set; }
    public Player Player => player;

    public void AddItemCount(Vector3 itemPositon)
    {
        _itemCount.Value++;
        _respawnPosition = itemPositon; //最後に取得したアイテムの位置をリスポーン地点にする
        if (_itemCount.Value >= 5)
        {
            SaveCurrentTime();
            GameClear();
        }
    }

    public void SaveCurrentTime()
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

    public void GameClear()
    {
        SceneManager.LoadScene("ClearScene");
    }

    public void GoToTitleScene()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void DecreaseCurrentTime(float v)
    {
        float actualDecreaseAmount = Math.Max(0, v);
        _onTimeChangedInternal.Value -= actualDecreaseAmount;
    }

    private void DecreasePenaltyTime(float v)
    {
        float actualDecreaseAmount = Math.Max(0, v);
        _onTimeChangedInternal.Value -= actualDecreaseAmount;
        _onHappenTimePenalty.OnNext(v);
    }

    public void IncreaseTime(float amount)
    {
        float actualIncreaseAmount = Math.Max(0, amount);
        _onTimeChangedInternal.Value = Math.Min(_onTimeChangedInternal.Value + actualIncreaseAmount, countDownDuration * 2f);
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
        
        DecreaseCurrentTime(Time.deltaTime);
        if (_onTimeChangedInternal.Value <= 0f)
        {
            _onTimeChangedInternal.Value = 0f;
            _isGameEnded = true;
            GameOver();
        }
    }
}
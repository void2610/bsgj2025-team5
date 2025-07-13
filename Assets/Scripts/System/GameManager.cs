using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using Cysharp.Threading.Tasks;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Header("必須参照")]
    [Tooltip("プレイヤーの参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private Player player;
    
    [Tooltip("UIのキャンバス")]
    [SerializeField] private Canvas uiCanvas;
    
    [Tooltip("敵の参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private EnemyAI enemyAI;
    
    [Header("ゲーム設定")]
    [SerializeField] private float countDownDuration = 180f;
    [SerializeField] private Vector3 defaultRespawnPosition;
    
    [Header("SE設定")]
    [SerializeField] private SeData timePenaltySe;
    [SerializeField] private SeData timeBonusSe;
    [SerializeField] private SeData itemGetSe;
    [SerializeField] private SeData gameClearSe1;
    [SerializeField] private SeData gameClearSe2;
    
    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";
    private readonly float _fallTimePenalty = 20f;
    
    private readonly ReactiveProperty<float> _onTimeChangedInternal = new();
    private readonly Subject<float> _onHappenTimePenalty = new();
    private readonly Subject<float> _onHappenTimeBonus = new();
    private readonly ReactiveProperty<int> _itemCount = new(0);
    
    private Rigidbody _playerRigidbody;
    private Vector3 _respawnPosition;
    private bool _isGameEnded;
    
    public ReadOnlyReactiveProperty<float> OnTimeChanged => _onTimeChangedInternal;
    public Observable<float> OnHappenTimePenalty => _onHappenTimePenalty.AsObservable();
    public Observable<float> OnHappenTimeBonus => _onHappenTimeBonus.AsObservable();
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
        
        await SeManager.Instance.PlaySeAsync(gameClearSe1, pitch: 1.0f, important: true);
        await UniTask.Delay(200);
        await SeManager.Instance.PlaySeAsync(gameClearSe2, pitch: 1.0f, important: true);
        await UniTask.Delay(500);
        SceneManager.LoadScene("ClearScene");
    }
    
    public void GoToTitleScene()
    {
        GoToTitleSceneAsync().Forget();
    }

    private async UniTask GoToTitleSceneAsync()
    {
        await IrisShot.StartIrisIn(uiCanvas);
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
        _onTimeChangedInternal.Value = Math.Min(_onTimeChangedInternal.Value + actualIncreaseAmount, countDownDuration * 2f);
        _onHappenTimeBonus.OnNext(actualIncreaseAmount);
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

    private void Start()
    {
        IrisShot.StartIrisIn(uiCanvas).Forget();
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
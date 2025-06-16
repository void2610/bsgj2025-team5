using System.Collections.Generic;
using System.Linq;
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

  [Tooltip("カウントダウンタイマーの参照。ゲーム開始時に設定する必要があります")] [SerializeField]
  private CountdownTimer countdownTimer;

  public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;

  private readonly ReactiveProperty<int> _itemCount = new(0);

  public ReadOnlyReactiveProperty<float> ClosestEnemyDistance { get; private set; }

  public Player Player => player;

  private readonly float _fallTimePenalty = -20f;

  private Rigidbody _playerqRigidbody;

  [SerializeField] private Vector3 defaultRespawnPosition;
  private Vector3 _respawnPosition;

  public void AddItemCount(Vector3 itemPositon)
  {
    _itemCount.Value++;
    _respawnPosition = itemPositon; //最後に取得したアイテムの位置をリスポーン地点にする
    if (_itemCount.Value >= 5)
    {
      // クリア時の残りタイムを保存する
      countdownTimer.SaveCurrentTime();
      GameClear();
    }
  }

  public void GameOver()
  {
    Debug.Log("Game Over");
    SceneManager.LoadScene("GameOverScene");
  }

  public void Fall()
  {
    Debug.Log("Fall Penalty: " + _fallTimePenalty);
    // 残り時間を減らしてプレイヤーをリスポーンさせる
    countdownTimer.OperateCurrentTime(_fallTimePenalty);
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

    protected override void Awake()
    {
        base.Awake();

        // R3を使った敵との距離計算
        ClosestEnemyDistance = ObservableExtensions
            .Select(Observable.EveryUpdate(), _ => Vector3.Distance(player.transform.position, enemyAI.transform.position))
            .ToReadOnlyReactiveProperty(float.MaxValue);

        // スポーン地点を初期化
        ResetRespawnPosition();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UIManager.Instance.TogglePause();
        }
    }
}
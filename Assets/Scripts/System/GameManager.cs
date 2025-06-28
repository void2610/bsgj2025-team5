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

  // カウントダウンタイマーの初期時間
  [SerializeField] private float countDownDuration = 180f;

  // 現在の残り時間
  private float _currentTime;

  // 残り時間変更時に通知するためのイベント
  public event Action<float> OnTimeChanged;

  // 他から残り時間を参照するためのプロパティ
  public float CurrentTimeValue => _currentTime;

  // ゲーム終了フラグ
  private bool _isGameEnded = false;

  // PlayerPrefsに登録する残り時間のキー
  private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";

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
      SaveCurrentTime();
      GameClear();
    }
  }

  public void SaveCurrentTime()
  {
    // PlayerPrefsに残り時間を保存
    PlayerPrefs.SetFloat(REMAINING_TIME_AT_CLEAR, _currentTime);
    PlayerPrefs.Save();

    Debug.Log($"クリア時の残り時間としてPlayerPrefsに保存しました: {_currentTime:F2}秒");
    _isGameEnded = true;
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
    OperateCurrentTime(_fallTimePenalty);
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

  public void OperateCurrentTime(float v)
  {
    _currentTime += v;
    // UIを更新する
    OnTimeChanged?.Invoke(_currentTime);
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
    //タイマーを初期化
    _currentTime = countDownDuration;
    // ゲーム開始時にフラグをリセット
    _isGameEnded = false;
    // UIの初期表示を更新する
    OnTimeChanged?.Invoke(_currentTime);
  }

  private void Update()
  {
    if (Input.GetKeyDown(KeyCode.P))
    {
      UIManager.Instance.TogglePause();
    }

    if (_isGameEnded) return;
    // 残り時間を数える
    OperateCurrentTime(-Time.deltaTime);
    if (_currentTime <= 0f)
    {
      _currentTime = 0f;
      Debug.Log("タイマーが0秒になりました");
      _isGameEnded = true;
      GameOver();
    }
  }
}
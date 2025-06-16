using System.Collections.Generic;
using System.Linq;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [Tooltip("プレイヤーの参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private Player player;
    [Tooltip("敵の参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private EnemyAI enemyAI;

    [Tooltip ("カウントダウンタイマーの参照。ゲーム開始時に設定する必要があります")]
    [SerializeField] private CountdownTimer countdownTimer;

    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;
        
    private readonly ReactiveProperty<int> _itemCount = new(0);

    public ReadOnlyReactiveProperty<float> ClosestEnemyDistance { get; private set; }
    
    public Player Player => player;

    private readonly float _fallTimePenalty = -20f;

    private Rigidbody _playerqRigidbody;

    [SerializeField] private Vector3 DefaultRespawnPosition; // デフォルトの復活地点（スタート地点）
    private Vector3 _respawnPosition;

    // PlayerPrefsから最後に取得したアイテムを読み込むためのキー
    private const string RESPAWN_POSITION_X = "RespawnPosition_x";
    private const string RESPAWN_POSITION_Y = "RespawnPosition_y";
    private const string RESPAWN_POSITION_Z = "RespawnPosition_z";

    public void AddItemCount()
    {
        _itemCount.Value++;
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
        this.RespownPlayer();
    }

    private void RespownPlayer()
    {
        // playerのRigidbodyを束縛する
        _playerqRigidbody = player.GetComponent<Rigidbody>();
        if (_playerqRigidbody != null)
        {
            Debug.Log("Respowning Player");

            // 座標それぞれのキーが設定されているか確認
            if (PlayerPrefs.HasKey(RESPAWN_POSITION_X) && PlayerPrefs.HasKey(RESPAWN_POSITION_Y) && PlayerPrefs.HasKey(RESPAWN_POSITION_Z))
            {
                // 保存された座標を読み込む
                float x = PlayerPrefs.GetFloat(RESPAWN_POSITION_X);
                float y = PlayerPrefs.GetFloat(RESPAWN_POSITION_Y);
                float z = PlayerPrefs.GetFloat(RESPAWN_POSITION_Z);
                _respawnPosition = new Vector3(x, y, z);
                Debug.Log($"リスポーン地点: 最後に拾ったアイテムの位置 ({_respawnPosition})");

            } 
            else //まだアイテムを取得していない場合
            { 
                _respawnPosition = DefaultRespawnPosition;
                Debug.Log($"リスポーン地点: 初期リスポーン地点（{DefaultRespawnPosition}） を使用します。");
            }
            
            
            // リスポーン地点に移動させる
            _playerqRigidbody.transform.position = _respawnPosition;
            // 速度と慣性をリセット
            _playerqRigidbody.linearVelocity = Vector3.zero;
            _playerqRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void ResetRespawnPosition()
    {
         // 前回のリスポーン地点を初期化（キーがあれば削除する）
        if (PlayerPrefs.HasKey(RESPAWN_POSITION_X)) {PlayerPrefs.DeleteKey(RESPAWN_POSITION_X);}
        if (PlayerPrefs.HasKey(RESPAWN_POSITION_Y)) {PlayerPrefs.DeleteKey(RESPAWN_POSITION_Y);}
        if (PlayerPrefs.HasKey(RESPAWN_POSITION_Z)) {PlayerPrefs.DeleteKey(RESPAWN_POSITION_Z);}
        // 削除後、変更をディスクに保存
        PlayerPrefs.Save();
        Debug.Log("ゲーム開始時に、前回のリスポーン位置をリセットしました。");
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

        // スポーン地点をデフォルトスポーン地点に初期化
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
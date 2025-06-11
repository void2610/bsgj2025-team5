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
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            UIManager.Instance.TogglePause();
        }
    }
}
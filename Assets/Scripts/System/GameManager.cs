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

    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;
        
    private readonly ReactiveProperty<int> _itemCount = new(0);

    public ReadOnlyReactiveProperty<float> ClosestEnemyDistance { get; private set; }
    
        
    public Player Player => player;
        
    public void AddItemCount()
    {
        _itemCount.Value++;
        
        // アイテム取得時にプレイヤーのpsychedelic modeを有効にする
        if (player != null)
        {
            player.ActivatePsychedelicMode();
        }
        
        if (_itemCount.Value >= 5)
        {
            Debug.Log("Clear!!");
            SceneManager.LoadScene("ClearScene");
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
        SceneManager.LoadScene("GameOverScene");
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
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : SingletonMonoBehaviour<GameManager>
{
    [SerializeField] private Player player;

    public ReadOnlyReactiveProperty<int> ItemCount => _itemCount;
        
    private readonly ReactiveProperty<int> _itemCount = new(0);
        
    public Player Player => player;
        
    public void AddItemCount()
    {
        _itemCount.Value++;
        if (_itemCount.Value >= 5)
        {
            Debug.Log("Clear!!");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }

    public void GameOver()
    {
        Debug.Log("Game Over");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
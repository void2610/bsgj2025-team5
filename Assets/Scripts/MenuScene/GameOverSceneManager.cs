using UnityEngine;

public class GameOverSceneManager : MonoBehaviour
{

    [SerializeField] private SeData gameOverSe;
    
    private void Start()
    {
        SeManager.Instance.PlaySe(gameOverSe, pitch: 1.0f);
    }
}

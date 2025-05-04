using UnityEngine;
using UnityEngine.SceneManagement;

namespace Izumi.Scripts.Prototype
{
    public class GameManager : SingletonMonoBehaviour<GameManager>
    {
        public void GameOver()
        {
            Debug.Log("Game Over");
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}

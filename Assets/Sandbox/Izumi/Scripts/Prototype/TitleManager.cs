using UnityEngine;
using UnityEngine.SceneManagement;

namespace Izumi.Prototype
{
    public class TitleManager : MonoBehaviour
    {
        public void StartGame()
        {
            SceneManager.LoadScene("MainScene");
        }
    }
}

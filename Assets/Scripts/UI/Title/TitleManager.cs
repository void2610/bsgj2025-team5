using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("MainScene");
    }
    
    public void GoToTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }
    
    public void GoToTitle()
    {
        SceneManager.LoadScene("TitleScene");
    }

    public void ExitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else 
            Application.Quit();
        #endif
    }

    private void Awake()
    {
        Time.timeScale = 1;
        Cursor.visible = false;
    }
}
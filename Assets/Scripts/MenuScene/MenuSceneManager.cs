using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MenuSceneManager : MonoBehaviour
{
    public void GoToMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }
    
    public void GoToTutorialScene()
    {
        SceneManager.LoadScene("TutorialScene");
    }
    
    public void GoToTitleScene()
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
        Cursor.lockState = CursorLockMode.None;
        
        // 毎フレームをストリーム化
        Observable.EveryUpdate()
            // 左クリック or 任意キー押下を検知
            .Where(_ => Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            .Take(1)                      // 最初の1回だけ
            .Subscribe(_ => Cursor.visible   = false)
            .AddTo(this);                 // GameObject が破棄されたら自動Dispose
    }
}
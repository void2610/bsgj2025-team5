using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;

public class MenuSceneManager : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;

    public void GoToMainScene()
    {
        LoadSceneAsync("MainScene").Forget();
    }
    
    public void GoToTutorialScene()
    {
        LoadSceneAsync("TutorialScene").Forget();
    }

    private async UniTask LoadSceneAsync(string sceneName)
    {
        // Show loading UI
        loadingPanel.SetActive(true);
        progressBar.gameObject.SetActive(true);
        progressText.gameObject.SetActive(true);
        
        // Initialize progress
        progressBar.value = 0f;
        progressText.text = "0%";
        
        // Start async scene loading
        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        
        // Update progress
        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            progressBar.value = progress;
            progressText.text = $"{(int)(progress * 100)}%";
            
            // When loading reaches 90%, allow scene activation
            if (operation.progress >= 0.9f)
            {
                progressBar.value = 1f;
                progressText.text = "100%";
                operation.allowSceneActivation = true;
            }
            
            await UniTask.Yield();
        }
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
        
        // Hide progress UI initially
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        
        // 毎フレームをストリーム化
        Observable.EveryUpdate()
            // 左クリック or 任意キー押下を検知
            .Where(_ => Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            .Take(1)                      // 最初の1回だけ
            .Subscribe(_ => Cursor.visible   = false)
            .AddTo(this);                 // GameObject が破棄されたら自動Dispose
    }
}
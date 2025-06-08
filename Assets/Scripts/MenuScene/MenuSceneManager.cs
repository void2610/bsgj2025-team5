using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using LitMotion;
using LitMotion.Extensions;

public class MenuSceneManager : MonoBehaviour
{
    [Header("Loading UI")]
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private Image fadeImage; // 画面全体を覆うフェード用Image

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
        // ローディングUIを表示
        loadingPanel.SetActive(true);
        progressBar.gameObject.SetActive(true);
        progressText.gameObject.SetActive(true);
        
        // フェード用Imageの初期化
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);
            var color = fadeImage.color;
            color.a = 0f;
            fadeImage.color = color;
        }
        
        // プログレスを初期化
        progressBar.value = 0f;
        progressText.text = "0%";
        
        // 非同期でシーンを読み込み開始
        var operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        
        float displayProgress = 0f;
        bool startedFade = false;
        
        // プログレスをスムーズに更新
        while (!operation.isDone)
        {
            // 実際のローディング進捗を取得 (0-0.9)
            float targetProgress = operation.progress / 0.9f;
            
            // ジャンプを避けるためスムーズに補間
            displayProgress = Mathf.MoveTowards(displayProgress, targetProgress, Time.deltaTime * 1.5f);
            
            // 90%まで表示
            if (displayProgress > 0.9f)
            {
                displayProgress = 0.9f;
            }
            
            progressBar.value = displayProgress;
            progressText.text = $"{(int)(displayProgress * 100)}%";
            
            // ローディングが完了したらフェードアウト開始
            if (operation.progress >= 0.9f && !startedFade)
            {
                startedFade = true;
                
                // プログレスを100%に
                progressBar.value = 1f;
                progressText.text = "100%";
                
                // LitMotionでフェードアウト
                if (fadeImage != null)
                {
                    // 0.5秒かけて黒にフェードアウト
                    await LMotion.Create(0f, 1f, 0.5f)
                        .WithEase(Ease.InOutSine)
                        .BindToColorA(fadeImage)
                        .ToUniTask();
                }
                
                // 画面が真っ黒になってからシーンをアクティベート
                operation.allowSceneActivation = true;
                break;
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
        
        // プログレスUIを初期状態で非表示にする
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (fadeImage != null) fadeImage.gameObject.SetActive(false);
        
        // 毎フレームをストリーム化
        Observable.EveryUpdate()
            // 左クリック or 任意キー押下を検知
            .Where(_ => Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            .Take(1)                      // 最初の1回だけ
            .Subscribe(_ => Cursor.visible   = false)
            .AddTo(this);                 // GameObject が破棄されたら自動Dispose
    }
}
using R3;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using TMPro;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine.Video;

public class MenuSceneManager : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas; // メインのCanvas
    [SerializeField] private GameObject loadingUIPrefab; // ローディングUI全体のPrefab
    [SerializeField] private Image playerImage; // 動画を表示するImageへの参照
    [SerializeField] private Image stillImage; // 静止画を表示するImageへの参照
    private bool _isWebGLBuild = false; //WebGLビルドかどうかのフラグ

    [SerializeField] private StoryPaperTheater storyPaperTheater; // 紙芝居コンポーネント
    [SerializeField] private AudioSource bgmSource; // BGM用AudioSource
    
    public void GoToMainScene()
    {
        GoToSceneWithStoryAsync("MainScene").Forget();
    }
    
    private async UniTask GoToSceneWithStoryAsync(string sceneName)
    {
        // BGMを停止
        LMotion.Create(bgmSource.volume, 0f, 1f).BindToVolume(bgmSource).AddTo(this);
        await storyPaperTheater.StartStoryAsync();
        // 紙芝居終了後にシーンをロード
        await LoadSceneAsync(sceneName);
    }

    private async UniTask LoadSceneAsync(string sceneName)
    {
        // ローディングUIのPrefabをインスタンス化
        var loadingInstance = Instantiate(loadingUIPrefab, mainCanvas.transform);

        // 生成されたオブジェクトから名前で必要なコンポーネントを取得
        var progressBar = loadingInstance.transform.Find("ProgressBar").GetComponent<Slider>();
        var progressText = loadingInstance.transform.Find("ProgressText").GetComponent<TextMeshProUGUI>();
        var fadeImage = loadingInstance.transform.Find("FadeImage").GetComponent<Image>();

        if (!progressBar) throw new System.Exception("ProgressBar is not found in the loading UI prefab.");
        if (!progressText) throw new System.Exception("ProgressText is not found in the loading UI prefab.");
        if (!fadeImage) throw new System.Exception("FadeImage is not found in the loading UI prefab.");

        // フェード用Imageの初期化
        var color = fadeImage.color;
        color.a = 0f;
        fadeImage.color = color;

        // プログレスを初期化
        progressBar.value = 0f;
        progressText.text = "0%";

        // 非同期でシーンを読み込み開始
        var operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null) throw new System.Exception($"Failed to start loading scene: {sceneName}");
        operation.allowSceneActivation = false;

        var displayProgress = 0f;


        // プログレスをスムーズに更新
        while (!operation.isDone)
        {
            // 実際のローディング進捗を取得 (0-0.9)
            var targetProgress = operation.progress / 0.9f;

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
            if (operation.progress >= 0.9f)
            {
                // プログレスを100%に
                progressBar.value = 1f;
                progressText.text = "100%";

                // LitMotionでフェードアウト
                await LMotion.Create(0f, 1f, 1f)
                    .WithEase(Ease.InOutSine)
                    .BindToColorA(fadeImage)
                    .ToUniTask();

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

    /// <summary>
    /// ビルドターゲットに応じて背景の画像・動画を切り替える
    /// </summary>
    private void ManageBackgroundDisplay()
    {
        if (_isWebGLBuild)
        {
            // WebGLビルドの場合
            if (playerImage != null)
            {
                playerImage.gameObject.SetActive(false); // 動画playerImageを含むGameObjectを非アクティブ化
            }
            else
            {
                Debug.Log("PlayerImageがセットされていません！");
            }

            if (stillImage != null)
            {
                stillImage.gameObject.SetActive(true); // 静止画backgroundImageを含むGameObjectをアクティブ化
            }
            else
            {
                Debug.Log("stillImageがセットされていません！");
            }

            Debug.Log("WebGLビルドのため、静止画を表示します。");
        }
        else
        {
            // その他のビルドの場合
            if (playerImage != null)
            {
                playerImage.gameObject.SetActive(true); // VideoPlayerを含むGameObjectをアクティブ化
            }
            else
            {
                Debug.Log("PlayerImageがセットされていません！");
            }

            if (stillImage != null)
            {
                stillImage.gameObject.SetActive(false); // 静止画Imageを含むGameObjectを非アクティブ化
            }
            else
            {
                Debug.Log("stillImageがセットされていません！");
            }

            Debug.Log("WebGL以外のビルドのため、動画を再生します。");
        }
    }


    private void Awake()
    {
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.None;

        // 毎フレームをストリーム化
        Observable.EveryUpdate()
            // 左クリック or 任意キー押下を検知
            .Where(_ => Input.GetMouseButtonDown(0) || Input.anyKeyDown)
            .Take(1) // 最初の1回だけ
            .Subscribe(_ => Cursor.visible = false)
            .AddTo(this); // GameObject が破棄されたら自動Dispose

#if UNITY_WEBGL
        _isWebGLBuild = true;
#else
        _isWebGLBuild = false;
#endif
    }

    private void Start()
    {
        // 背景の画像・動画の切り替え
        ManageBackgroundDisplay();
    }
}
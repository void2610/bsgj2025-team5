using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleSceneManager : MonoBehaviour
{
    [SerializeField] private Canvas mainCanvas; // メインのCanvas
    [SerializeField] private GameObject loadingUIPrefab; // ローディングUI全体のPrefab
    [SerializeField] private Image playerImage; // 動画を表示するImageへの参照
    [SerializeField] private Image stillImage; // 静止画を表示するImageへの参照
    [SerializeField] private StoryPaperTheater storyPaperTheater; // 紙芝居コンポーネント
    [SerializeField] private AudioSource bgmSource; // BGM用AudioSource
    [SerializeField] private Image fadeImage; // フェード用のImage
    [SerializeField] private GameObject popupPrefab; // ポップアップのPrefab
    
    private bool _isWebGLBuild = false; //WebGLビルドかどうかのフラグ
    
    public void GoToMainSceneWithStory()
    {
        GoToSceneWithStoryAsync("MainScene").Forget();
    }
    
    /// <summary>
    /// ビルドターゲットに応じて、動画または静止画を表示するための設定を行います。
    /// </summary>
    private void ManagePlayerImage()
    {
        if (_isWebGLBuild)
        {
            // WebGLビルドの場合
            playerImage?.gameObject.SetActive(false); // 動画playerImageを含むGameObjectを非アクティブ化
            stillImage?.gameObject.SetActive(true); // 静止画backgroundImageを含むGameObjectをアクティブ化
            Debug.Log("WebGLビルドのため、静止画を表示します。");
        }
        else
        { 
            playerImage?.gameObject.SetActive(true); // VideoPlayerを含むGameObjectをアクティブ化
            stillImage?.gameObject.SetActive(false); // 静止画Imageを含むGameObjectを非アクティブ化
            Debug.Log("WebGL以外のビルドのため、動画を再生します。");
        }
    }
    
    private async UniTask GoToSceneWithStoryAsync(string sceneName)
    {
        // BGMを停止
        LMotion.Create(bgmSource.volume, 0f, 1f).BindToVolume(bgmSource).AddTo(this);
        await storyPaperTheater.StartStoryAsync();
        await IrisShot.StartIrisOut();
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
    
    private async UniTask ManageWebBuildPopup()
    {
        // if (!_isWebGLBuild) return;
        
        // WebGLビルドの場合、ポップアップを表示
        var popupInstance = Instantiate(popupPrefab, mainCanvas.transform);
        
        popupInstance.transform.localScale = Vector3.zero;
        
        await LMotion.Create(Vector3.zero, Vector3.one, 0.3f)
            .WithEase(Ease.OutBack)
            .BindToLocalScale(popupInstance.transform)
            .ToUniTask();
        
        await UniTask.Delay(3500);
        
        await LMotion.Create(Vector3.one, Vector3.zero, 0.3f)
            .WithEase(Ease.InBack)
            .BindToLocalScale(popupInstance.transform)
            .ToUniTask();
        
        Destroy(popupInstance);
    }
    
    private void Awake()
    {
#if UNITY_WEBGL
        _isWebGLBuild = true;
#else
        _isWebGLBuild = false;
#endif
    }

    private async UniTaskVoid Start()
    {
        ManagePlayerImage();
        
        // IrisShotの読み込みが一瞬遅れるので、一番最初に読み込まれるシーンは一瞬黒い画像で隠す
        fadeImage.color = new Color(0f, 0f, 0f, 1f);
        await IrisShot.LoadIrisShotObj();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        await UniTask.Delay(1500);
        // WebGLビルドの場合、ポップアップを表示
        await ManageWebBuildPopup();
    }
}

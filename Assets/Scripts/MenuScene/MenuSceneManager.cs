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

    // 静止画表示用のPlayerImageオブジェクト
    [SerializeField] private GameObject playerImageGameObject; // PlayerImageのGameObject

    // 動画再生に関する変数
    [SerializeField] private VideoClip titleVideoClip; // 再生したい動画クリップ (エディタで設定)
    [SerializeField] private VideoPlayer videoPlayer; // シーン内のVideoPlayerコンポーネント (Video Player GameObjectにアタッチされているもの)
    [SerializeField] private GameObject videoPlayerGameObject; // VideoPlayerがアタッチされているGameObject (添付画像にある"Video Player"オブジェクト)


    private bool _isWebGLBuild = false; //WebGLビルドかどうかのフラグ

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
        // オブジェクトが設定されているか確認
        if (playerImageGameObject == null)
        {
            Debug.LogError("PlayerImage GameObjectが設定されていません。");
            return;
        }
        if (videoPlayerGameObject == null || videoPlayer == null)
        {
            Debug.LogError("VideoPlayer GameObjectまたはVideoPlayerコンポーネントが設定されていません。");
            return;
        }

        if (_isWebGLBuild) // WebGLビルドの場合：静止画を表示
        {
            Debug.Log("WebGLビルド: 静止画を表示します。");

            playerImageGameObject.SetActive(true); // PlayerImage (静止画) を有効にする
            videoPlayerGameObject.SetActive(false); // Video Player (動画) を無効にする
            videoPlayer.Stop(); // 動画を停止
        }
        else // WebGL以外のビルドの場合：動画を再生
        {
            Debug.Log("WebGL以外のビルド: 動画を再生します。");

            playerImageGameObject.SetActive(false); // PlayerImage (静止画) を無効にする
            videoPlayerGameObject.SetActive(true); // Video Player (動画) を有効にする

            if (titleVideoClip != null)
            {
                // VideoPlayerの設定と再生
                videoPlayer.clip = titleVideoClip; // VideoClipを設定
                videoPlayer.Prepare(); // 動画の準備を開始
                videoPlayer.prepareCompleted += OnVideoPrepared; // 準備完了イベントを購読
                Debug.Log($"動画クリップ '{titleVideoClip.name}' の準備を開始しました。");
            }
            else
            {
                Debug.LogError("Title Video Clipが設定されていません。動画再生ができません。静止画に戻します。");
                // 動画クリップがない場合は、静止画を表示するフォールバック
                playerImageGameObject.SetActive(true);
                videoPlayerGameObject.SetActive(false);
            }
        }
    }

    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared; // イベントの購読を解除
        source.Play(); // 動画の再生を開始
        Debug.Log("動画の再生を開始しました。");
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
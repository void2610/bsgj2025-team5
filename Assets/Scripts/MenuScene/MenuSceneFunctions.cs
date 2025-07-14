using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSceneFunctions : MonoBehaviour
{
    
    public void MoveScene(string sceneName)
    {
        MoveSceneAsync(sceneName).Forget();
    }
    
    private async UniTaskVoid MoveSceneAsync(string sceneName)
    {
        // シーン遷移の前にIrisShotを実行
        await IrisShot.StartIrisOut();
        SceneManager.LoadScene(sceneName);
    }

    public void ExitGame()
    {
        ExitGameAsync().Forget();
    }
    
    private async UniTaskVoid ExitGameAsync()
    {
#if UNITY_EDITOR
        await UniTask.Yield(); // 警告を回避するため
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Windows固有のフリーズ問題を回避するため、短い遅延を追加
        await UniTask.Delay(100);
        // カーソルを表示状態に戻す
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        // 音声を停止
        AudioListener.pause = true;
        // アプリケーションを終了
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
            .Take(1) // 最初の1回だけ
            .Subscribe(_ => Cursor.visible = false)
            .AddTo(this); // GameObject が破棄されたら自動Dispose
    }

    private void Start()
    {
        IrisShot.StartIrisIn().Forget();
    }
}
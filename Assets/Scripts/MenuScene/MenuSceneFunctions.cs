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
            .Take(1) // 最初の1回だけ
            .Subscribe(_ => Cursor.visible = false)
            .AddTo(this); // GameObject が破棄されたら自動Dispose
    }

    private void Start()
    {
        IrisShot.StartIrisIn().Forget();
    }
}
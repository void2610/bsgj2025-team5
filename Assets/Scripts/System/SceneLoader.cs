using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using R3;
using Cysharp.Threading.Tasks;

public sealed class SceneLoader : MonoBehaviour
{
    [SerializeField] private List<string> scenesToLoad = new();

    public ReactiveProperty<float> Progress { get; } = new(0f);

    private void Awake()
    {
        DontDestroyOnLoad(this);
    }

    private async UniTaskVoid Start()
    {
        // 不正な値を防止
        # if UNITY_EDITOR
        if (scenesToLoad.Count == 0)
            throw new System.Exception("シーン名が設定されていません。");
        var sceneNames = new HashSet<string>();
        foreach (var sceneName in scenesToLoad.Where(sceneName => !sceneNames.Add(sceneName)))
            throw new System.Exception($"シーン名 '{sceneName}' が重複しています。");
        # endif
        
        var activeName = scenesToLoad[0];
        await LoadAdditiveScenesAsync();

        var activeScene = SceneManager.GetSceneByName(activeName);
        if (activeScene.IsValid() && activeScene.isLoaded)
        {
            SceneManager.SetActiveScene(activeScene);
        }
        else
        {
            Debug.LogError($"Scene '{activeName}' がロードされていません。Build Settings と名前を確認してください。");
        }
    }

    private async UniTask LoadAdditiveScenesAsync()
    {
        var total = scenesToLoad.Count;

        for (var i = 0; i < total; i++)
        {
            var sceneName = scenesToLoad[i];
            var op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            var tmp = i;
            var reporter = new System.Progress<float>(p => Progress.Value = (tmp + p) / total);
            await op.ToUniTask(progress: reporter);
            // isLoaded==true になるまで待機
            await UniTask.WaitUntil(() => SceneManager.GetSceneByName(sceneName).isLoaded);
        }

        Progress.Value = 1f;
    }

    public async UniTask UnloadAllScenesAsync()
    {
        for (var i = scenesToLoad.Count - 1; i >= 0; i--)
        {
            var scn = SceneManager.GetSceneByName(scenesToLoad[i]);
            if (!scn.isLoaded) continue;
            
            var op = SceneManager.UnloadSceneAsync(scn);
            await op.ToUniTask();
        }
    }
}

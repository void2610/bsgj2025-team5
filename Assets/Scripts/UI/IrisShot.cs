using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class IrisShot
{
    private static GameObject _irisShotObj;
    
    public static async UniTask StartIrisOut()
    {
        var irisShotObj = await LoadIrisShotObj();
        var unMask = irisShotObj.GetComponentInChildren<Unmask>();
        
        await LMotion.Create(Vector3.one * 20f, Vector3.one * 0.5f, 0.5f)
            .WithEase(Ease.InCubic)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.3f)
            .WithEase(Ease.OutCubic)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 1.5f, Vector3.zero, 0.5f)
            .WithEase(Ease.InCubic)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();

        await UniTask.Delay(500);
    }
    
    public static async UniTask StartIrisIn()
    {
        var irisShotObj = await LoadIrisShotObj();
        var unMask = irisShotObj.GetComponentInChildren<Unmask>();
        
        await LMotion.Create(Vector3.zero, Vector3.one * 0.5f, 0.3f)
            .WithEase(Ease.OutCubic)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.2f)
            .WithEase(Ease.InQuad)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 1.5f, Vector3.one * 20f, 0.5f)
            .WithEase(Ease.OutCubic)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();

        await UniTask.Delay(500);
    }
    
    private static async UniTask<GameObject> LoadIrisShotObj()
    {
        if (_irisShotObj) return _irisShotObj;
        
        var prefab = await Addressables.LoadAssetAsync<GameObject>("IrisShot").ToUniTask();
        
        // キャンバスを探してその子としてインスタンス化
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (!canvas)
        {
            Debug.LogError("Canvas not found in scene");
            return null;
        }
        
        var instance = Object.Instantiate(prefab, canvas.transform);
        _irisShotObj = instance;
        
        return _irisShotObj;
    }
}

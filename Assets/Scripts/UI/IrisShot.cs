using Coffee.UIExtensions;
using Cysharp.Threading.Tasks;
using LitMotion;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class IrisShot
{
    private static GameObject _irisShotObj;
    
    // シーン遷移時にクリアする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Init()
    {
        _irisShotObj = null;
    }
    
    public static async UniTask StartIrisOut(Canvas canvas = null)
    {
        var irisShotObj = await LoadIrisShotObj(canvas);
        var unMask = irisShotObj.GetComponentInChildren<Unmask>();
        
        unMask.transform.localScale = Vector3.one * 20f;
        
        await LMotion.Create(Vector3.one * 20f, Vector3.one * 0.5f, 0.5f)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.3f)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 1.5f, Vector3.zero, 0.5f)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();

        await UniTask.Delay(500, ignoreTimeScale: true);
    }
    
    public static async UniTask StartIrisIn(Canvas canvas = null)
    {
        var irisShotObj = await LoadIrisShotObj(canvas);
        var unMask = irisShotObj.GetComponentInChildren<Unmask>();
        
        unMask.transform.localScale = Vector3.zero;
        
        await LMotion.Create(Vector3.zero, Vector3.one * 0.5f, 0.3f)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 0.5f, Vector3.one * 1.5f, 0.5f)
            .WithEase(Ease.InCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();
            
        await LMotion.Create(Vector3.one * 1.5f, Vector3.one * 20f, 0.3f)
            .WithEase(Ease.OutCubic)
            .WithScheduler(MotionScheduler.UpdateIgnoreTimeScale)
            .Bind(scale => unMask.transform.localScale = scale)
            .ToUniTask();

        await UniTask.Delay(500, ignoreTimeScale: true);
    }
    
    private static async UniTask<GameObject> LoadIrisShotObj(Canvas canvas = null)
    {
        if (_irisShotObj) return _irisShotObj;
        
        // キャンバスを探す
        if (!canvas)
        {
            canvas = Object.FindFirstObjectByType<Canvas>();
            if (!canvas)
            {
                Debug.LogError("Canvas not found in the scene.");
                return null;
            }
        }
        
        var prefab = await Addressables.LoadAssetAsync<GameObject>("IrisShot").ToUniTask();
        var instance = Object.Instantiate(prefab, canvas.transform);
        _irisShotObj = instance;
        return _irisShotObj;
    }
}

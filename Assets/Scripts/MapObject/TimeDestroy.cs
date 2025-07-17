using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class TimeDestroy : MonoBehaviour
{
    [SerializeField]
    private float destroyTime = 10f;

    private async UniTask Start()
    {
        await UniTask.Delay((int)(destroyTime * 1000));
        var scale = transform.localScale;
        await LMotion.Create(scale, Vector3.zero, 0.5f)
            .WithEase(Ease.InBounce)
            .BindToLocalScale(this.transform)
            .ToUniTask();
    }
}

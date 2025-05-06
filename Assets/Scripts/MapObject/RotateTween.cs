using UnityEngine;
using LitMotion;
using LitMotion.Extensions;

namespace Izumi.Prototype
{
    public class RotateTween : MonoBehaviour
    {
        [SerializeField] private float speed = 1f;
        
        private void Start()
        {
            LMotion.Create(new Vector3(45, 0, 45), new Vector3(45, 360, 45), 1f / speed)
                .WithLoops(-1, LoopType.Restart)
                .WithEase(Ease.Linear)
                .BindToEulerAngles(this.transform) // object3の回転に紐づけ
                .AddTo(this.gameObject);
        }
    }
}

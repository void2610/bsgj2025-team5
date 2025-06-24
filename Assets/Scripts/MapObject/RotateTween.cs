using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class RotateTween : MonoBehaviour
{
    [Tooltip("回転速度（1秒あたりの回転数）。大きいほど速く回転します")]
    [SerializeField] private float speed = 1f;
    
    [Tooltip("回転の開始角度。X, Y, Z軸の角度を設定します")]
    [SerializeField] private Vector3 startAngle = new Vector3(45, 0, 45);
    
    [Tooltip("回転の終了角度。X, Y, Z軸の角度を設定します")]
    [SerializeField] private Vector3 endAngle = new Vector3(45, 360, 45);

    private void Start()
    {
        LMotion.Create(startAngle, endAngle, 1f / speed)
            .WithLoops(-1, LoopType.Restart)
            .WithEase(Ease.Linear)
            .BindToEulerAngles(this.transform) // object3の回転に紐づけ
            .AddTo(this.gameObject);
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using LitMotion;
using Cysharp.Threading.Tasks;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target;          // 追従対象（ボール）
    [Header("Offset")]
    public float distance = 6.0f;     // 背後距離
    public float height   = 2.0f;     // 目線の高さオフセット
    [Header("Rotation")]
    public float lookSensitivity = 150f; // マウス感度 (deg/s)
    public float minPitch = -20f;        // 俯角下限
    public float maxPitch =  80f;        // 仰角上限
    [Header("Smooth")]
    public float posLerpSpeed   = 10f;   // 位置追従の滑らかさ
    public float rotLerpSpeed   = 10f;   // 回転追従の滑らかさ
    [Header("Auto Align")]
    public bool autoAlign = true;          // ON/OFF
    public float alignSpeed = 4f;          // 角度収束速度 (1/sec, 高いほど速い)
    public float velThreshold = 0.2f;      // 速度閾値 (m/s) 以下なら無視

    private float _yaw;   // 水平角 (deg)
    private float _pitch; // 垂直角 (deg)
    private Rigidbody _rb;
    private MotionHandle _shakeHandle;
    private Vector3 _shakeOffset;
    
    public void ShakeCamera(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f) => 
        ShakeCameraAsync(magnitude, duration, frequency, dampingRatio).Forget();

    /// <summary>
    /// カメラを一定時間揺らし、完了後に await できる
    /// </summary>
    public async UniTask ShakeCameraAsync(float magnitude, float duration, int frequency = 10, float dampingRatio = 0.5f)
    {
        if(_shakeHandle.IsPlaying()) _shakeHandle.Complete(); // 前の揺れを停止

        // 揺れMotionの開始
        _shakeHandle = LMotion.Shake.Create(startValue: Vector3.zero, strength: Vector3.one * magnitude, duration: duration)
            .WithFrequency(frequency)
            .WithDampingRatio(dampingRatio)
            .Bind(offset => _shakeOffset = offset)
            .AddTo(this);

        await _shakeHandle.ToUniTask();

        _shakeOffset = Vector3.zero; // 終了後リセット
    }

    private void Start()
    {
        // 初期角度をターゲット基準にセット
        _yaw   = target.eulerAngles.y;
        _pitch = 15f;
        _rb   = target.GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        if (autoAlign)
        {
            var v = _rb.linearVelocity;
            v.y = 0f;                                // 水平面へ投影
            var targetYaw = Mathf.Atan2(v.x, v.z) * Mathf.Rad2Deg;
            if (v.sqrMagnitude >= velThreshold * velThreshold)
            {
                _yaw = Mathf.LerpAngle(
                    _yaw,
                    targetYaw,
                    1 - Mathf.Exp(-alignSpeed * Time.deltaTime)
                );
            }
        }

        var targetRot = Quaternion.Euler(_pitch, _yaw, 0f);
        var pivot   = target.position + Vector3.up * height;
        var desired = pivot - targetRot * Vector3.forward * distance;

        // カメラの位置と回転を補間
        transform.position = Vector3.Lerp(
            transform.position, desired, 1 - Mathf.Exp(-posLerpSpeed * Time.deltaTime));

        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, 1 - Mathf.Exp(-rotLerpSpeed * Time.deltaTime));
        
        // カメラの揺れを適用
        transform.position += _shakeOffset;
    }
}

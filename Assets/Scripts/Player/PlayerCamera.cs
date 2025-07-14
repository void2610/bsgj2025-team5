using UnityEngine;
using UnityEngine.InputSystem;
using LitMotion;
using Cysharp.Threading.Tasks;

public class PlayerCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("追従するターゲット（プレイヤーのTransform）")]
    public Transform target;
    
    [Header("Offset")]
    [Tooltip("ターゲットからの距離。大きいほど遠くから見ます")]
    public float distance = 6.0f;
    
    [Tooltip("ターゲットからの高さオフセット。大きいほど上から見下ろします")]
    public float height   = 2.0f;
    [Header("Rotation")]
    [Tooltip("マウス感度（度/秒）。大きいほど素早く回転します")]
    public float lookSensitivity = 150f;
    
    [Tooltip("下向き角度の下限値（マイナス値）")]
    public float minPitch = -20f;
    
    [Tooltip("上向き角度の上限値（プラス値）")]
    public float maxPitch =  80f;
    [Header("Smooth")]
    [Tooltip("位置追従の滑らかさ。大きいほど素早く追従します")]
    public float posLerpSpeed   = 10f;
    
    [Tooltip("回転追従の滑らかさ。大きいほど素早く回転します")]
    public float rotLerpSpeed   = 10f;
    [Header("Auto Align")]
    [Tooltip("ONの場合、移動方向にカメラが自動的に向きます")]
    public bool autoAlign = true;
    
    [Tooltip("カメラが移動方向に向く速度。大きいほど素早く向きます")]
    public float alignSpeed = 4f;
    
    [Tooltip("この速度以下では自動整列を無視します（m/s）")]
    public float velThreshold = 0.2f;

    private float _yaw;   // 水平角 (deg)
    private float _pitch; // 垂直角 (deg)
    private Rigidbody _rb;
    private MotionHandle _shakeHandle;
    private Vector3 _shakeOffset;
    
    // 演出制御用
    public float GetCurrentPitch() => _pitch;
    public float GetCurrentYaw() => _yaw;
    public bool IsIntroMode { get; private set; } = false;
    
    public void SetIntroMode(bool isIntro) => IsIntroMode = isIntro;

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

        Cursor.visible = false;
    }

    private void LateUpdate()
    {
        if (UIManager.Instance.IsPaused || IsIntroMode) return;
        
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

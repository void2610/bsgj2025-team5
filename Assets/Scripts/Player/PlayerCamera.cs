using UnityEngine;
using UnityEngine.InputSystem;

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

    private void Start()
    {
        // 初期角度をターゲット基準にセット
        _yaw   = target.eulerAngles.y;
        _pitch = 15f;
        _rb   = target.GetComponent<Rigidbody>();
    }

    private void LateUpdate()
    {
        if (!target) return;

        /* ── 入力（右クリック押下時のみ） ── */
        var delta = Mouse.current.delta.ReadValue();
        if (Mouse.current.rightButton.isPressed)
        {
            _yaw   += delta.x * lookSensitivity * Time.unscaledDeltaTime;
            _pitch -= delta.y * lookSensitivity * Time.unscaledDeltaTime;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }
        
        var manualLook = Mouse.current.rightButton.isPressed;
        
        if (autoAlign && !manualLook && _rb)
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

        /* ── カメラの目標回転 ── */
        var targetRot = Quaternion.Euler(_pitch, _yaw, 0f);

        /* ── カメラの目標位置 ── */
        var pivot   = target.position + Vector3.up * height;
        var desired = pivot - targetRot * Vector3.forward * distance;

        /* ── スムージング適用 ── */
        transform.position = Vector3.Lerp(
            transform.position, desired, 1 - Mathf.Exp(-posLerpSpeed * Time.deltaTime));

        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRot, 1 - Mathf.Exp(-rotLerpSpeed * Time.deltaTime));
    }
}

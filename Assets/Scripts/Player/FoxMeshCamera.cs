using UnityEngine;
using LitMotion;
using R3;
using Cysharp.Threading.Tasks;

/// <summary>
/// プレイヤーメッシュを正面から映すカメラ
/// UIの立ち絵表示用にレンダーテクスチャに出力する
/// </summary>
[RequireComponent(typeof(Camera))]
public class FoxMeshCamera : MonoBehaviour
{
    [Header("ターゲット設定")]
    [Tooltip("追従するプレイヤーのTransform")]
    [SerializeField] private Transform playerTransform;
    
    [Header("カメラ位置設定")]
    [Tooltip("プレイヤーからのオフセット位置")]
    [SerializeField] private Vector3 positionOffset = new Vector3(0f, 0.5f, 2f);
    
    [Tooltip("カメラの回転角度（X:上下, Y:左右, Z:ロール）")]
    [SerializeField] private Vector3 rotationAngle = new Vector3(0f, 180f, 0f);
    
    [Header("振動エフェクト")]
    [Tooltip("振動の強さ")]
    [SerializeField] private float shakeStrength = 0.2f;
    
    [Tooltip("振動の持続時間")]
    [SerializeField] private float shakeDuration = 0.3f;
    
    [Tooltip("振動の周波数")]
    [SerializeField] private int shakeFrequency = 30;
    
    [Tooltip("振動の減衰率")]
    [SerializeField] private float shakeDampingRatio = 0.5f;
    
    [Tooltip("振動のクールダウン時間")]
    [SerializeField] private float shakeCooldown = 0.5f;
    
    [Tooltip("衝突強度に対する振動強度の倍率")]
    [SerializeField] private float shakeIntensityMultiplier = 0.05f;
    
    // 内部変数
    private float _lastShakeTime = -999f;
    private MotionHandle _shakeHandle;
    private Vector3 _shakeOffset;
    
    private void Start()
    {
        var player = FindAnyObjectByType<Player>();
        // 衝突イベントを購読
        player.OnCollisionEvent
            .Subscribe(collisionData => OnPlayerCollision(collisionData.position, collisionData.intensity))
            .AddTo(this);
    }
    
    private void LateUpdate()
    {
        // カメラ位置と回転を完全に追従
        UpdateCameraTransform();
        
        // 振動オフセットを適用
        transform.position += _shakeOffset;
    }
    
    /// <summary>
    /// カメラの位置と回転を更新（完全追従）
    /// </summary>
    private void UpdateCameraTransform()
    {
        // プレイヤーの向きを考慮したオフセット位置
        Vector3 targetPosition = playerTransform.position + playerTransform.TransformDirection(positionOffset);
        
        // プレイヤーの位置を見るように回転を設定
        Vector3 lookDirection = playerTransform.position - targetPosition;
        if (lookDirection.magnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            targetRotation *= Quaternion.Euler(rotationAngle);
            
            // 即座に位置と回転を更新（完全追従）
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }
    }
    
    /// <summary>
    /// プレイヤーの衝突イベントを処理
    /// </summary>
    private void OnPlayerCollision(Vector3 collisionPosition, float intensity)
    {
        // クールダウン中は処理しない
        if (Time.time - _lastShakeTime < shakeCooldown) return;
        
        // 衝突強度に基づいて振動を開始
        StartShake(intensity);
        _lastShakeTime = Time.time;
    }
    
    /// <summary>
    /// 振動エフェクトを開始
    /// </summary>
    private void StartShake(float intensity = 1f)
    {
        // 既存の振動を停止
        if (_shakeHandle.IsActive())
        {
            _shakeHandle.Complete();
        }
        
        // 衝突強度に基づいて振動の強さを計算
        float dynamicShakeStrength = shakeStrength + (intensity * shakeIntensityMultiplier);
        
        // 新しい振動を開始
        _shakeHandle = LMotion.Shake.Create(
            startValue: Vector3.zero,
            strength: Vector3.one * dynamicShakeStrength,
            duration: shakeDuration
        )
        .WithFrequency(shakeFrequency)
        .WithDampingRatio(shakeDampingRatio)
        .Bind(offset => _shakeOffset = offset)
        .AddTo(this);
        
        // 振動完了時にオフセットをリセット
        WaitForShakeComplete().Forget();
    }
    
    /// <summary>
    /// 振動完了を待機してオフセットをリセット
    /// </summary>
    private async UniTaskVoid WaitForShakeComplete()
    {
        await _shakeHandle.ToUniTask();
        _shakeOffset = Vector3.zero;
    }
    
    /// <summary>
    /// デバッグ用：ギズモでカメラ位置を表示
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (playerTransform)
        {
            // ターゲット位置を表示
            Gizmos.color = Color.yellow;
            Vector3 targetPos = playerTransform.position + playerTransform.TransformDirection(positionOffset);
            Gizmos.DrawWireSphere(targetPos, 0.1f);
            
            // カメラの向きを表示
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(targetPos, playerTransform.position);
        }
    }
}

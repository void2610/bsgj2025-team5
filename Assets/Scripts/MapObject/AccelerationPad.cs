using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AccelerationPad : MonoBehaviour
{
    [Header("Acceleration Settings")]
    [Tooltip("加速の強さ。大きいほど強く加速します")]
    [SerializeField] private float accelerationForce = 20f;
    
    [Tooltip("加速する方向。(1,0,0)で右方向、(0,1,0)で上方向、(0,0,1)で前方向")]
    [SerializeField] private Vector3 accelerationDirection = Vector3.forward;
    
    [Tooltip("ONの場合、オブジェクトの向きに対する相対方向。OFFの場合、ワールド座標での絶対方向")]
    [SerializeField] private bool useLocalDirection = true;
    
    [Header("Feedback")]
    [Tooltip("加速時に再生する効果音")]
    [SerializeField] private SeData accelerationSeData;
    
    [Tooltip("加速時に表示するパーティクルエフェクト")]
    [SerializeField] private ParticleData accelerationParticleData;
    
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            var playerRb = other.GetComponent<Rigidbody>();
            ApplyAcceleration(playerRb);
            PlayFeedback();
        }
    }
    
    private void ApplyAcceleration(Rigidbody playerRb)
    {
        // 加速方向を取得（ローカル or ワールド）
        var direction = useLocalDirection 
            ? transform.TransformDirection(accelerationDirection.normalized) 
            : accelerationDirection.normalized;
        
        // 力を加える
        playerRb.AddForce(direction * accelerationForce, ForceMode.Impulse);
    }
    
    private void PlayFeedback()
    {
        if (accelerationSeData) SeManager.Instance.PlaySe(accelerationSeData);
        
        if (accelerationParticleData)
        {
            // 加速方向を向くようにパーティクルを生成
            var direction = useLocalDirection 
                ? transform.TransformDirection(accelerationDirection.normalized) 
                : accelerationDirection.normalized;
            var rotation = Quaternion.LookRotation(direction);
            
            ParticleManager.Instance.CreateParticle(
                accelerationParticleData, 
                transform.position,
                rotation
            );
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // エディタで方向を可視化
        Gizmos.color = Color.cyan;
        var direction = useLocalDirection 
            ? transform.TransformDirection(accelerationDirection.normalized) 
            : accelerationDirection.normalized;
        Gizmos.DrawRay(transform.position, direction * 2f);
    }
}
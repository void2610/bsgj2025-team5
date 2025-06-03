using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AccelerationPad : MonoBehaviour
{
    [Header("Acceleration Settings")]
    [SerializeField] private float accelerationForce = 20f;
    [SerializeField] private Vector3 accelerationDirection = Vector3.forward;
    [SerializeField] private bool useLocalDirection = true;
    
    [Header("Feedback")]
    [SerializeField] private SeData accelerationSeData;
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
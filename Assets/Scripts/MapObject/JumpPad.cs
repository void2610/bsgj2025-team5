using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 15f;
    
    [Header("Feedback")]
    [SerializeField] private SeData jumpSeData;
    [SerializeField] private ParticleData jumpParticleData;
    
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            var playerRb = other.GetComponent<Rigidbody>();
            ApplyJumpForce(playerRb);
            PlayFeedback();
        }
    }
    
    private void ApplyJumpForce(Rigidbody playerRb)
    {
        // 現在の上向き速度をリセットして新しい速度を設定
        var currentVelocity = playerRb.linearVelocity;
        currentVelocity.y = jumpForce;
        playerRb.linearVelocity = currentVelocity;
    }
    
    private void PlayFeedback()
    {
        if (jumpSeData) SeManager.Instance.PlaySe(jumpSeData);
        
        if (jumpParticleData)
        {
            ParticleManager.Instance.CreateParticle(
                jumpParticleData, 
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );
        }
    }
}
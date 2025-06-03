using UnityEngine;

[RequireComponent(typeof(Collider))]
public class JumpPad : MonoBehaviour
{
    [Header("Jump Settings")]
    [Tooltip("ジャンプの強さ。大きいほど高く飛びます")]
    [SerializeField] private float jumpForce = 15f;
    
    [Header("Feedback")]
    [Tooltip("ジャンプ時に再生する効果音")]
    [SerializeField] private SeData jumpSeData;
    
    [Tooltip("ジャンプ時に表示するパーティクルエフェクト")]
    [SerializeField] private ParticleData jumpParticleData;
    
    private void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            if (other.TryGetComponent<Rigidbody>(out var playerRb))
            {
                ApplyJumpForce(playerRb);
            }
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
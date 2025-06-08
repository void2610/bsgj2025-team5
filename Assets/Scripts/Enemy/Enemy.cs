using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Tooltip("敵の移動速度（メートル/秒）")]
    [SerializeField] private float speed = 5f;
    
    [Tooltip("敵がターゲットを向く回転速度（度/秒）")]
    [SerializeField] private float rotationSpeed = 360f;
    
    [Tooltip("追いかけるターゲット（通常はPlayer）")]
    [SerializeField] private Transform target;
    
    [Tooltip("ゲームオーバーになる距離。この距離内に入るとプレイヤーが捕まります")]
    [SerializeField] private float attackRange = 1f;
        
    private void FixedUpdate()
    {
        // ターゲットの方向を向く
        var direction = (target.position - transform.position).normalized;
        var targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

        // ターゲットに向かって移動
        transform.Translate(direction * (speed * Time.deltaTime), Space.World);
            
        // ターゲットとの距離を計算
        var distance = Vector3.Distance(transform.position, target.position);
        if (distance < attackRange)
        {
            GameManager.Instance.GameOver();
        }
    }
}
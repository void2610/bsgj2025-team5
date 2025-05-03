using UnityEngine;

namespace Izumi.Scripts.Prototype
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float speed = 5f; // 移動速度
        [SerializeField] private float rotationSpeed = 360f; // 回転速度 (deg/s)
        [SerializeField] private Transform target; // 追従対象
        [SerializeField] private float attackRange = 1f; // 攻撃範囲
        [SerializeField] private float attackForce = 1f;
        
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
                // 力を加える
                var rb = target.GetComponent<Rigidbody>();
                var forceDirection = (target.position - transform.position).normalized;
                forceDirection.y = 0.5f; // 上方向の力を加える
                rb.AddForce(forceDirection * attackForce, ForceMode.Impulse);
                target.GetComponent<Player>().TakeDamage(1); // プレイヤーにダメージを与える
            }
        }
    }
}

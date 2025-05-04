using UnityEngine;

namespace Izumi.Prototype
{
    public class Enemy : MonoBehaviour
    {
        [SerializeField] private float speed = 5f; // 移動速度
        [SerializeField] private float rotationSpeed = 360f; // 回転速度 (deg/s)
        [SerializeField] private Transform target; // 追従対象
        [SerializeField] private float attackRange = 1f; // 攻撃範囲
        
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
}

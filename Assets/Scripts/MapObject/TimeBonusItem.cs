using UnityEngine;

public class TimeBonusItem : MonoBehaviour
{
    [Tooltip("取得時に増加する時間（秒）")]
    [SerializeField] private float timeBonus = 10f;
    
    [Tooltip("取得時のパーティクルエフェクト（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Tooltip("取得時のサウンドエフェクト（オプション）")]
    [SerializeField] private AudioClip pickupSound;
    
    [Tooltip("アイテムの回転速度")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("アイテムの上下運動の高さ")]
    [SerializeField] private float floatHeight = 0.3f;
    
    [Tooltip("アイテムの上下運動の速度")]
    [SerializeField] private float floatSpeed = 2f;
    
    private Vector3 _startPosition;
    private float _floatTimer;
    
    private void Start()
    {
        // 初期位置を記録
        _startPosition = transform.position;
    }
    
    private void Update()
    {
        // アイテムの回転アニメーション
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        
        // アイテムの上下運動アニメーション
        _floatTimer += Time.deltaTime;
        var newY = _startPosition.y + Mathf.Sin(_floatTimer * floatSpeed) * floatHeight;
        transform.position = new Vector3(_startPosition.x, newY, _startPosition.z);
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            // 時間を増やす
            GameManager.Instance.IncreaseTime(timeBonus);
            
            if (particlePrefab)
            {
                Instantiate(particlePrefab, transform.position, Quaternion.identity);
            }
            
            // アイテムを削除
            Destroy(gameObject);
        }
    }
}
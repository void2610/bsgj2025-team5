using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BreakableObject : MonoBehaviour
{
    [Tooltip("壊すために必要な最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;
    
    [Tooltip("オブジェクトが消滅するまでの遅延時間")]
    [SerializeField] private float destroyDelay = 0.5f;
    
    [Tooltip("吹っ飛び時に加える力の強さ")]
    [SerializeField] private float blowForce = 80f;
    
    [Tooltip("吹っ飛び時の上向きの力")]
    [SerializeField] private float upwardForce = 30f;
    
    [Tooltip("プレイヤー検知距離")]
    [SerializeField] private float detectionDistance = 2f;
    
    private Rigidbody _rb;
    private Collider _collider;
    private Player _player;
    private bool _isBlownAway;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        
        // 初期状態では物理演算を無効にして静的にする
        _rb.isKinematic = true;
    }
    
    private void Start()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        // すでに吹っ飛んでいるか、プレイヤーが見つからない場合は処理しない
        if (_isBlownAway || !_player) return;
        
        var squaredDistance = (transform.position - _player.transform.position).sqrMagnitude;
        
        // 検知距離内にプレイヤーがいて、条件を満たしている場合
        if (squaredDistance <= detectionDistance * detectionDistance && _player.PlayerSpeedInt.CurrentValue >= requiredSpeed)
        {
            // コライダーを無効化してプレイヤーの減速を防ぐ
            _collider.enabled = false;
            BlowAway();
        }
    }

    private void BlowAway()
    {
        _isBlownAway = true;
        
        // 物理演算を有効にする
        _rb.isKinematic = false;
        
        // プレイヤーの位置から吹っ飛ぶ方向を計算
        var direction = (transform.position - _player.transform.position).normalized;
        // 水平方向の力と上向きの力を組み合わせ
        var force = direction * blowForce + Vector3.up * upwardForce;
        
        // 力を加える
        _rb.AddForce(force, ForceMode.Impulse);
        
        // 回転も加える（よりリアルな物理挙動のため）
        var randomTorque = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        _rb.AddTorque(randomTorque, ForceMode.Impulse);
        
        // 指定時間後に消滅
        Destroy(gameObject, destroyDelay);
    }
}
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BreakableObject : MonoBehaviour
{
    [Tooltip("壊すために必要な最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;
    
    [Tooltip("オブジェクトが消滅するまでの遅延時間")]
    [SerializeField] private float destroyDelay = 0.5f;
    
    [Tooltip("カメラを揺らす強さ")]
    [SerializeField] private float cameraShakeMagnitude = 0.4f;
    
    [Tooltip("吹っ飛び時に加える力の強さ")]
    [SerializeField] private float blowForce = 80f;
    
    [Tooltip("吹っ飛び時の上向きの力")]
    [SerializeField] private float upwardForce = 30f;
    
    [Tooltip("プレイヤー検知距離")]
    [SerializeField] private float detectionDistance = 2f;
    
    [Tooltip("プレイヤーを減速させる力の強さ")]
    [SerializeField] private float slowdownForce = 10f;
    
    [System.Serializable]
    public class SpeedBasedSettings
    {
        [Tooltip("この速度レベルでの吹っ飛び力の倍率")]
        public float blowForceMultiplier = 1f;
        [Tooltip("この速度レベルでの減速力の倍率")]
        public float slowdownForceMultiplier = 1f;
    }
    
    [Tooltip("速度レベル別の設定（0:停止〜4:最高速）")]
    [SerializeField] private List<SpeedBasedSettings> speedSettings = new List<SpeedBasedSettings>();
    
    [Header("アイテムドロップ設定")]
    [Tooltip("ドロップするアイテムのプレハブ")]
    [SerializeField] private GameObject dropItemPrefab;
    
    [Tooltip("アイテムをドロップする確率（0〜1）")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;
    
    [Tooltip("ドロップするアイテムの高さオフセット")]
    [SerializeField] private float dropHeightOffset = 0.5f;
    
    [Tooltip("ドロップするアイテムのランダムな位置オフセット範囲")]
    [SerializeField] private Vector2 dropPositionOffsetRange = new Vector2(-0.5f, 0.5f);
    
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
        
        if (speedSettings is not { Count: 5 })
        {
            throw new System.Exception("設定されている速度レベルの数は5でなければなりません。");
        }
    }
    
    private void Start()
    {
        _player = FindFirstObjectByType<Player>();
    }

    private void Update()
    {
        if (_isBlownAway || !_player) return;
        
        var squaredDistance = (transform.position - _player.transform.position).sqrMagnitude;
        // 検知距離内にプレイヤーがいて、条件を満たしている場合
        if (squaredDistance <= detectionDistance * detectionDistance && _player.PlayerItemCountInt.CurrentValue >= requiredSpeed)
        {
            // コライダーを無効化してプレイヤーの減速を防ぐ
            Physics.IgnoreCollision(_collider, _player.GetComponent<Collider>());
            BlowAway(_player.PlayerItemCountInt.CurrentValue);
        }
    }

    private void BlowAway(int playerSpeed)
    {
        _isBlownAway = true;
        _rb.isKinematic = false;
        
        // プレイヤーの速度レベルに応じた設定を取得
        var settings = speedSettings[Mathf.Clamp(playerSpeed, 0, 4)];
        
        // プレイヤーの位置から吹っ飛ぶ方向を計算
        var direction = (transform.position - _player.transform.position).normalized;
        // 水平方向の力と上向きの力を組み合わせ（速度レベル倍率を適用）
        var force = direction * (blowForce * settings.blowForceMultiplier) + Vector3.up * upwardForce;
        // 力を加える
        _rb.AddForce(force, ForceMode.Impulse);
        
        // 回転も加える（よりリアルな物理挙動のため）
        var randomTorque = new Vector3(
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f),
            Random.Range(-10f, 10f)
        );
        _rb.AddTorque(randomTorque, ForceMode.Impulse);
        
        // プレイヤーを減速させる
        SlowDownPlayer(playerSpeed);
        
        // カメラを揺らす
        FindFirstObjectByType<PlayerCamera>().ShakeCamera(cameraShakeMagnitude, 0.3f);
        
        // 確率でアイテムをドロップ
        if (dropItemPrefab && Random.value < dropChance)
        {
            var spawnPosition = transform.position + new Vector3(
                Random.Range(dropPositionOffsetRange.x, dropPositionOffsetRange.y),
                dropHeightOffset,
                Random.Range(dropPositionOffsetRange.x, dropPositionOffsetRange.y)
            );
            Instantiate(dropItemPrefab, spawnPosition, Quaternion.identity);
        }
        
        // 指定時間後に消滅
        Destroy(gameObject, destroyDelay);
    }
    
    private void SlowDownPlayer(int playerSpeed)
    {
        // プレイヤーの速度レベルに応じた設定を取得
        var settings = speedSettings[Mathf.Clamp(playerSpeed, 0, 4)];
        
        // プレイヤーのRigidbodyを取得してブレーキ力を適用
        var playerRb = _player.GetComponent<Rigidbody>();
        // 現在の速度を取得
        var currentVelocity = playerRb.linearVelocity;
        var currentSpeed = currentVelocity.magnitude;
        // 減速力を計算
        var slowdownAmount = slowdownForce * settings.slowdownForceMultiplier * Time.fixedDeltaTime;
        // 減速後の速度を計算（0を下回らないようにする）
        var newSpeed = Mathf.Max(0f, currentSpeed - slowdownAmount);
        // 速度の方向を保持したまま速度を調整
        if (currentSpeed > 0f) playerRb.linearVelocity = currentVelocity.normalized * newSpeed;
    }
}
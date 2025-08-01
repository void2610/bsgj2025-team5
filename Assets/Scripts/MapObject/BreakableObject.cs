using System.Collections.Generic;
using UnityEngine;
using LitMotion;
using Cysharp.Threading.Tasks;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class BreakableObject : MonoBehaviour
{
    [System.Serializable]
    public class SpeedBasedSettings
    {
        [Tooltip("この速度レベルでの吹っ飛び力の倍率")]
        public float blowForceMultiplier = 1f;
        [Tooltip("この速度レベルでの減速力の倍率")]
        public float slowdownForceMultiplier = 1f;
    }
    
    [Tooltip("壊すために必要な最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed;
    
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
    
    [Tooltip("破壊時のSE")]
    [SerializeField] private SeData breakSe;
    
    [Tooltip("速度レベル別の設定（0:停止〜4:最高速）")]
    [SerializeField] private List<SpeedBasedSettings> speedSettings = new List<SpeedBasedSettings>();
    
    [Header("アイテムドロップ設定")]
    [Tooltip("ドロップするアイテムのプレハブ")]
    [SerializeField] private GameObject dropItemPrefab;
    
    [Tooltip("アイテムをドロップする確率（0〜1）")]
    [SerializeField, Range(0f, 1f)] private float dropChance = 0.3f;
    
    [Tooltip("吹っ飛び時のドロップ力")]
    [SerializeField] private float dropForce = 15f;
    
    [Tooltip("ドロップするアイテムの高さオフセット")]
    [SerializeField] private float dropHeightOffset = 0.5f;
    
    [Tooltip("アイテムドロップ時のアーチアニメーション時間")]
    [SerializeField] private float dropAnimationDuration = 0.8f;
    
    [Tooltip("アイテムドロップ時のアーチの高さ")]
    [SerializeField] private float dropArchHeight = 2.0f;
    
    [Tooltip("アイテムドロップ時のアーチアニメーションイージング")]
    [SerializeField] private Ease dropArchEase = Ease.OutQuart;
    
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
        // スケールを考慮した検知距離を計算（最大スケール値を使用）
        var maxScale = Mathf.Max(transform.localScale.x, transform.localScale.y, transform.localScale.z);
        var scaledDetectionDistance = detectionDistance * maxScale;
        
        // 検知距離内にプレイヤーがいて、条件を満たしている場合
        if (squaredDistance <= scaledDetectionDistance * scaledDetectionDistance && _player.PlayerItemCountInt.CurrentValue >= requiredSpeed)
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
        // 破壊音を再生
        SeManager.Instance.PlaySe(breakSe);
        
        // 確率でアイテムをドロップ
        if (dropItemPrefab && Random.value < dropChance)
        {
            DropItemWithAnimationAsync(direction).Forget();
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

    private async UniTaskVoid DropItemWithAnimationAsync(Vector3 collisionDirection)
    {
        // プレイヤーからの衝突方向にアイテムを配置（反対方向にスポーン）
        var dir = collisionDirection.normalized;
        // 放射状に散らばるためのランダムな角度を追加
        var dropDirection = Quaternion.AngleAxis(Random.Range(-15f, 15f), Vector3.up) * dir;
        // 最終的なドロップ位置を計算
        var finalDropPosition = this.transform.position + dropDirection * dropForce;
        // オブジェクトの底面位置を基準にしてY座標を設定
        var bounds = _collider.bounds;
        finalDropPosition.y = bounds.min.y + dropHeightOffset;
        
        // アイテムをBreakableObjectの位置にスポーン
        var droppedItem = Instantiate(dropItemPrefab, this.transform.position, Quaternion.identity);
        
        // アーチアニメーションの設定
        var startPosition = transform.position;
        var midPoint = (startPosition + finalDropPosition) / 2f;
        midPoint.y += dropArchHeight; // アーチの高さを追加
        
        // アーチを描く移動アニメーション（二次ベジェ曲線）
        await LMotion.Create(0f, 1f, dropAnimationDuration)
            .WithEase(dropArchEase)
            .Bind(t => {
                // 二次ベジェ曲線の計算
                var position = CalculateQuadraticBezier(startPosition, midPoint, finalDropPosition, t);
                droppedItem.transform.position = position;
            })
            .AddTo(droppedItem);
        droppedItem.transform.position = finalDropPosition;
        
        droppedItem?.GetComponent<TimeBonusItem>()?.Initialize();
    }
    
    // 二次ベジェ曲線の計算
    private Vector3 CalculateQuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
    {
        var u = 1f - t;
        var tt = t * t;
        var uu = u * u;
        var uut = 2f * u * t;
        
        return uu * p0 + uut * p1 + tt * p2;
    }
}
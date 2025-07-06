using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Player : MonoBehaviour
{
    [Header("カメラと操作")]
    [Tooltip("プレイヤーを追従するカメラ")]
    [SerializeField] private Camera playerCamera;
    
    [Tooltip("ONの場合、マウスの左右操作が反転します")]
    [SerializeField] private bool   isHorizontalInverted = false;
    
    [Tooltip("ONの場合、マウスの上下操作が反転します")]
    [SerializeField] private bool   isVerticalInverted = false;

    [Header("物理演算")]
    [Tooltip("マウス移動量を回転力に変換する倍率。大きいほど敏感に回転します")]
    [SerializeField] private float  torqueMultiplier   = 0.25f;
    
    [Tooltip("最大移動速度。この速度を基準に速度レベルが計算されます")]
    [SerializeField] private float  maxLinearVelocity  = 10f;
    
    [Tooltip("最大回転速度。ボールの回転の上限値")]
    [SerializeField] private float  maxAngularVelocity = 50f;
    
    [Tooltip("回転の減衰率。大きいほど回転が止まりやすくなります（0=減衰なし）")]
    [SerializeField] private float angularDamping = 3f;
    
    [Header("Physics Material")]
    [Tooltip("動摩擦係数。移動中の摩擦の強さ（0=摩擦なし）")]
    [SerializeField] private float dynamicFriction = 1f;
    
    [Tooltip("静止摩擦係数。停止時の摩擦の強さ（0=摩擦なし）")]
    [SerializeField] private float staticFriction = 1f;
    
    [Tooltip("弾性。衝突時の跳ね返りの強さ（0=跳ね返りなし、1=完全弾性）")]
    [SerializeField] private float bounciness = 0f;
    
    [Header("演出")]
    [Tooltip("衝突時に生成する砂のパーティクル")]
    [SerializeField] private ParticleData sandParticleData;
    
    [Tooltip("衝突演出が発生する速度の閾値")]
    [SerializeField] private float collisionSpeedThreshold = 0.2f;

    [Tooltip("移動時に生成する煙のパーティクル")]
    [SerializeField] private ParticleSystem smokeParticleData;
    
    [Tooltip("煙パーティクルの発生量の範囲")]
    [SerializeField] private Vector2 smokeEmissionRange = new Vector2(0f, 2f);
    
    [Tooltip("衝突時のSE")]
    [SerializeField] private SeData collisionSe;
    
    [Tooltip("マウス加速時のSE")]
    [SerializeField] private SeData mouseAccelerationSe;
    
    [Tooltip("マウス加速度の閾値。この値を超えると加速SEが再生されます")]
    [SerializeField] private float mouseAccelerationThreshold = 50f;
    
    [Tooltip("SE再生のクールダウン時間（秒）")]
    [SerializeField] private float mouseAccelerationSeCooldown = 0.5f;

    /// <summary>
    /// プレイヤーの速度を0-1のfloatで表す（現在はアイテム数ベース）
    /// </summary>
    public ReadOnlyReactiveProperty<float> PlayerItemCountNorm => _itemCountNorm;
    /// <summary>
    /// プレイヤーの速度を0-4のintで表す（現在はアイテム数ベース）
    /// </summary>
    public ReadOnlyReactiveProperty<int>  PlayerItemCountInt  { get; private set; }
    /// <summary>
    /// マウスの移動速度を表す（正規化されていない生の値）
    /// </summary>
    public ReadOnlyReactiveProperty<float> MouseSpeed => _mouseSpeed;

    private readonly ReactiveProperty<float> _itemCountNorm = new(0f);
    private readonly ReactiveProperty<float> _mouseSpeed = new(0f);
    private readonly ReactiveProperty<float> _playerSpeed = new(0f);

    private Rigidbody _rb;
    private Collider _collider;
    private PhysicsMaterial _physicsMaterial;
    private Vector2 _accumulatedInputDelta;
    
    // マウス加速度検出用
    private Vector2 _previousMouseDelta;
    private bool _canPlayAccelerationSe = true;
    private CancellationTokenSource _accelerationSeCts;

    private void UpdateSmokeParticle(float speed)
    {
        var emission = smokeParticleData.emission;
        emission.rateOverDistance = Mathf.Lerp(smokeEmissionRange.x, smokeEmissionRange.y, speed);
    }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        
        // Rigidbodyの設定
        _rb.maxAngularVelocity = maxAngularVelocity;
        _rb.angularDamping = angularDamping;
        
        // Physics Materialの設定（既存のマテリアルがない場合のみ作成）
        if (!_collider.material)
        {
            _physicsMaterial = new PhysicsMaterial("PlayerPhysicsMaterial")
            {
                dynamicFriction = dynamicFriction,
                staticFriction = staticFriction,
                bounciness = bounciness,
                frictionCombine = PhysicsMaterialCombine.Average,
                bounceCombine = PhysicsMaterialCombine.Average
            };
            _collider.material = _physicsMaterial;
        }
        else
        {
            _physicsMaterial = _collider.material;
        }

        // アイテム数ベースの正規化された値を5段階（0-4）に変換して公開する
        PlayerItemCountInt = _itemCountNorm
            .Select(n => Mathf.Clamp(Mathf.FloorToInt(n * 5f), 0, 4))
            .ToReadOnlyReactiveProperty()
            .AddTo(this);
    }

    private void Start()
    {
        // GameManagerのアイテム数変化を購読して、_speedNormを更新
        GameManager.Instance.ItemCount.Subscribe(OnItemCountChanged).AddTo(this);
        
        // _playerSpeedの変更を監視して、煙のパーティクルの発生量を更新
        _playerSpeed.Subscribe(UpdateSmokeParticle).AddTo(this);
        
        // 初期値設定
        OnItemCountChanged(GameManager.Instance.ItemCount.CurrentValue);
    }

    private void Update()
    {
        // マウスのdelta値を取得（ピクセル/フレーム）
        var currentDelta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
        
        // マウス加速度の計算
        var deltaChange = currentDelta - _previousMouseDelta;
        var acceleration = deltaChange.magnitude / Time.deltaTime;
        
        // 加速度が閾値を超えた場合、かつ再生可能であればSEを再生
        if (acceleration > mouseAccelerationThreshold && _canPlayAccelerationSe)
        {
            SeManager.Instance.PlaySe(mouseAccelerationSe);
            PlayAccelerationSeWithCooldown().Forget();
        }
        
        // 次フレームのために現在のdeltaを保存
        _previousMouseDelta = currentDelta;
        
        // FixedUpdate用に入力を蓄積
        _accumulatedInputDelta += currentDelta;
        
        // マウス速度の更新（UI表示用）
        _mouseSpeed.Value = currentDelta.magnitude;
    }

    private void OnItemCountChanged(int itemCount)
    {
        // アイテム数（0-5）を0-1の範囲にマッピング
        _itemCountNorm.Value = Mathf.Clamp01(itemCount / 5f);
    }

    private void FixedUpdate()
    {
        // 蓄積された入力を使用
        if (_accumulatedInputDelta.sqrMagnitude < 1e-4f) return;

        var camF = playerCamera.transform.forward;
        var camR = playerCamera.transform.right;
        camF.y = camR.y = 0f; camF.Normalize(); camR.Normalize();

        // カメラ相対の方向を計算（反転設定を個別に適用）
        var horizontalInput = isHorizontalInverted ? -_accumulatedInputDelta.x : _accumulatedInputDelta.x;
        var verticalInput = isVerticalInverted ? _accumulatedInputDelta.y : -_accumulatedInputDelta.y;
        var torqueDir = camF * horizontalInput + camR * verticalInput;

        // フレームレート非依存の強度計算
        // _accumulatedInputDeltaは既にフレーム間の総移動量なので、Time.fixedDeltaTimeで正規化
        var strength = _accumulatedInputDelta.magnitude * torqueMultiplier;
        _rb.angularVelocity += torqueDir * strength;
        
        // 使用済みの入力をリセット
        _accumulatedInputDelta = Vector2.zero;
        
        // プレイヤーの速度を更新
        _playerSpeed.Value = _rb.linearVelocity.magnitude / maxLinearVelocity;
    }

    /// <summary>
    /// プレイヤーの動きを完全に停止する（ゲームクリア時の演出用）
    /// </summary>
    public void StopMovement()
    {
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        _rb.isKinematic = true;
    }
    
    /// <summary>
    /// 加速SE再生のクールダウン処理
    /// </summary>
    private async UniTaskVoid PlayAccelerationSeWithCooldown()
    {
        _canPlayAccelerationSe = false;
        
        // 既存のトークンをキャンセル
        _accelerationSeCts?.Cancel();
        _accelerationSeCts?.Dispose();
        _accelerationSeCts = new CancellationTokenSource();
        
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(mouseAccelerationSeCooldown), 
                cancellationToken: _accelerationSeCts.Token);
            _canPlayAccelerationSe = true;
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        // 一定以上の速度で衝突した場合、砂のパーティクルを生成する
        // このようにコードから生成する場合はParticleManagerを通して生成すると、万が一、大量にこのコードが呼ばれても過剰な生成を防げる

        if (_rb.linearVelocity.magnitude > collisionSpeedThreshold && other.contacts.Length > 0)
        {
            var quaternion = Quaternion.FromToRotation(Vector3.up, other.contacts[0].normal);
            ParticleManager.Instance.CreateParticle(sandParticleData, this.transform.position + Vector3.down * 0.5f, quaternion);
            
            // カメラを揺らす
            playerCamera.GetComponent<PlayerCamera>().ShakeCamera(0.2f, 0.3f);
            // 衝突音を再生
            SeManager.Instance.PlaySe(collisionSe);
        }
    }
    
    private void OnDestroy()
    {
        // クリーンアップ
        _accelerationSeCts?.Cancel();
        _accelerationSeCts?.Dispose();
    }
}
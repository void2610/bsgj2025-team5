using System;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Player : MonoBehaviour
{
    [Header("Camera & Control")]
    [Tooltip("プレイヤーを追従するカメラ")]
    [SerializeField] private Camera playerCamera;
    
    [Tooltip("ONの場合、マウス操作が反転します")]
    [SerializeField] private bool   isInverted = false;

    [Header("Physics")]
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
    
    [Header("Visual")]
    [Tooltip("衝突時に生成する砂のパーティクルエフェクト")]
    [SerializeField] private ParticleData sandParticleData;

    /// <summary>
    /// プレイヤーの速度を0-1のfloatで表す
    /// </summary>
    public ReadOnlyReactiveProperty<float> PlayerSpeedNorm => _speedNorm;
    /// <summary>
    /// プレイヤーの速度を0-4のintで表す
    /// </summary>
    public ReadOnlyReactiveProperty<int>  PlayerSpeedInt  { get; private set; }
    /// <summary>
    /// マウスの移動速度を表す（正規化されていない生の値）
    /// </summary>
    public ReadOnlyReactiveProperty<float> MouseSpeed => _mouseSpeed;

    private readonly ReactiveProperty<float> _speedNorm = new(0f);
    private readonly ReactiveProperty<float> _mouseSpeed = new(0f);

    private Rigidbody _rb;
    private Collider _collider;
    private PhysicsMaterial _physicsMaterial;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
        
        // Rigidbodyの設定
        _rb.maxAngularVelocity = maxAngularVelocity;
        _rb.angularDamping = angularDamping;
        
        // Physics Materialの作成と設定
        _physicsMaterial = new PhysicsMaterial("PlayerPhysicsMaterial")
        {
            dynamicFriction = dynamicFriction,
            staticFriction = staticFriction,
            bounciness = bounciness,
            frictionCombine = PhysicsMaterialCombine.Average,
            bounceCombine = PhysicsMaterialCombine.Average
        };
        _collider.material = _physicsMaterial;

        // 正規化された速度を別のReactivePropertyに変換して公開する
        PlayerSpeedInt = _speedNorm
            .Select(n => Mathf.Clamp(Mathf.FloorToInt((n + 0.25f) * 4f), 0, 4))
            .ToReadOnlyReactiveProperty()
            .AddTo(this);
    }

    private void FixedUpdate()
    {
        var vNorm = Mathf.Clamp01(_rb.linearVelocity.magnitude / maxLinearVelocity);
        _speedNorm.Value = vNorm;

        var delta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
        _mouseSpeed.Value = delta.magnitude;
        if (delta.sqrMagnitude < 1e-4f) return;

        var camF = playerCamera.transform.forward;
        var camR = playerCamera.transform.right;
        camF.y = camR.y = 0f; camF.Normalize(); camR.Normalize();

        var torqueDir = camF * delta.x + camR * -delta.y;
        if (isInverted) torqueDir = -torqueDir;

        var strength = delta.magnitude * torqueMultiplier;
        _rb.angularVelocity += torqueDir * strength;
    }

    private void OnCollisionEnter(Collision other)
    {
        // 一定以上の速度で衝突した場合、砂のパーティクルを生成する
        // このようにコードから生成する場合はParticleManagerを通して生成すると、万が一、大量にこのコードが呼ばれても過剰な生成を防げる
        if (_rb.linearVelocity.magnitude > 0.2f)
        {
            var quaternion = Quaternion.FromToRotation(Vector3.up, other.contacts[0].normal);
            ParticleManager.Instance.CreateParticle(sandParticleData, this.transform.position + Vector3.down * 0.5f, quaternion);
            
            // カメラを揺らす
            playerCamera.GetComponent<PlayerCamera>().ShakeCamera(0.2f, 0.3f);
        }
    }
}
using System;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("Camera & Control")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private bool   isInverted = false;

    [Header("Physics")]
    [SerializeField] private float  torqueMultiplier   = 0.25f;
    [SerializeField] private float  maxLinearVelocity  = 10f;
    [SerializeField] private float  maxAngularVelocity = 50f;
    
    [Header("Visual")]
    [SerializeField] private ParticleData sandParticleData;

    /// <summary>
    /// プレイヤーの速度を0-1のfloatで表す
    /// </summary>
    public ReadOnlyReactiveProperty<float> PlayerSpeedNorm => _speedNorm;
    /// <summary>
    /// プレイヤーの速度を0-4のintで表す
    /// </summary>
    public ReadOnlyReactiveProperty<int>  PlayerSpeedInt  { get; private set; }

    private readonly ReactiveProperty<float> _speedNorm = new(0f);

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _rb.maxAngularVelocity = maxAngularVelocity;

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
        if (delta.sqrMagnitude < 1e-4f) return;

        var camF = playerCamera.transform.forward;
        var camR = playerCamera.transform.right;
        camF.y = camR.y = 0f; camF.Normalize(); camR.Normalize();

        var torqueDir = camF * delta.x + camR * -delta.y;
        if (isInverted) torqueDir = -torqueDir;

        var strength = delta.magnitude * torqueMultiplier;
        _rb.AddTorque(torqueDir * strength, ForceMode.Impulse);
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
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Izumi.Prototype
{
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
                .Select(n => Mathf.Clamp(Mathf.FloorToInt((n + 0.3f) * 4f), 0, 4))
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
    }
}

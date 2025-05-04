using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Izumi.Scripts.Prototype
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private bool isInverted = false;
        [SerializeField] private float torqueMultiplier = 0.25f;
        [SerializeField] private float maxLinearVelocity = 10f;
        [SerializeField] private float maxAngularVelocity = 50f;
        
        // プレイヤーの現在の速度を0~4で表す
        public ReadOnlyReactiveProperty<int> PlayerSpeed => _playerSpeed;
        
        private ReactiveProperty<int> _playerSpeed = new (0);
        private Rigidbody _rb;
        
        private void Awake()
        {
            _rb  = GetComponent<Rigidbody>();
            // ちゃんと転がるように上限を引き上げ
            _rb.maxAngularVelocity = maxAngularVelocity;
        }

        private void FixedUpdate()
        {
            // 演出を更新
            var v = _rb.linearVelocity.magnitude / maxLinearVelocity;
            VolumeManager.Instance.SetValue(v);
            
            // トラックボールの瞬間移動量 (ピクセル単位)
            var delta = Mouse.current?.delta.ReadValue() ?? Vector2.zero;
            if (delta.sqrMagnitude < 0.0001f) return; // タッチ無し

            // カメラ基準の水平ベクトルを取得
            var camForward = playerCamera.transform.forward;
            var camRight   = playerCamera.transform.right;
            camForward.y = camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            // ――― マッピング方針 ―――
            //   右へボールを強く回す (delta.x>0) → カメラ前方へ転がしたい
            //   上へボールを強く回す (delta.y>0) → カメラ右方へ転がしたい
            //   （好みで符号反転してください）
            var torqueDir = camForward *  delta.x     // 横回転 → 前後トルク
                            + camRight   * -delta.y;    // 縦回転 → 左右トルク
            if (isInverted) torqueDir *= -1f; // 符号反転

            // delta の大きさ ≒ 回す勢い (ピクセル/フレーム)
            var strength = delta.magnitude * torqueMultiplier;

            // Impulse にすると「瞬間トルクを毎フレーム加える」イメージ
            _rb.AddTorque(torqueDir * strength, ForceMode.Impulse);
            
            // プレイヤー速度を更新
            _playerSpeed.Value = Mathf.FloorToInt(Mathf.Clamp(v * 4f, 0f, 4f));
        }
    }
}
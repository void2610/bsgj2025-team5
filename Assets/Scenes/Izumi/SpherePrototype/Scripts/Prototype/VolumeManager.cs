using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using LitMotion;

namespace Izumi.Scripts.Prototype
{
    public class VolumeManager : SingletonMonoBehaviour<VolumeManager>
    {
        [SerializeField] private Volume volume;

        [Header("Color Adjustments")]
        [SerializeField] private Vector2 saturationRange = new (0f, 60f);
        [SerializeField] private Vector2 exposureRange   = new (0f, 1.5f);
        [SerializeField] private Vector2 hueShiftSpeedRange = new (0f, 0.5f); // deg/s
        [SerializeField] private float hueShiftThreshold = 0.25f; // ← ★ 新規：この v を超えたら発動

        [Header("Chromatic Aberration")]
        [SerializeField] private Vector2 caIntensityRange = new (0f, 1f);

        [Header("Lens Distortion")]
        [SerializeField] private Vector2 ldIntensityRange = new (0f, -0.4f);

        private ColorAdjustments _cAdj;
        private ChromaticAberration _ca;
        private LensDistortion _ld;

        private MotionHandle _hueHandle;    // LitMotion ハンドル
        private float _currentSpeed = -1f;  // 直近の deg/s

        protected override void Awake()
        {
            base.Awake();

            volume.profile.TryGet(out _cAdj);
            volume.profile.TryGet(out _ca);
            volume.profile.TryGet(out _ld);
        }

        /// <summary>
        /// 正規化速度 v (0‥1) を受け取り、ポストプロセスを更新
        /// </summary>
        public void SetValue(float v)
        {
            v = Mathf.Clamp01(v);

            /* ── 彩度・露光・CA・歪みは従来どおり補間 ── */
            _cAdj.saturation.value   = Mathf.Lerp(saturationRange.x, saturationRange.y, v);
            _cAdj.postExposure.value = Mathf.Lerp(exposureRange.x,   exposureRange.y,   v);
            _ca.intensity.value      = Mathf.Lerp(caIntensityRange.x, caIntensityRange.y, v);
            _ld.intensity.value      = Mathf.Lerp(ldIntensityRange.x, ldIntensityRange.y, v);

            /* ── Hue Shift のオン／オフ判定 ── */
            if (v < hueShiftThreshold)
            {
                // 閾値未満：Tween 停止 & 色相リセット
                CancelHueTween();
                return;
            }

            /* 閾値以上：速度に応じた HueShift Tween を有効化 */
            float speed = Mathf.Lerp(hueShiftSpeedRange.x, hueShiftSpeedRange.y, v); // deg/s

            if (!Mathf.Approximately(speed, _currentSpeed))
            {
                _currentSpeed = speed;
                StartHueTween(speed);
            }
        }

        /* 0→360°をループさせる Tween を生成 */
        private void StartHueTween(float degPerSec)
        {
            CancelHueTween();

            float duration = 360f / degPerSec; // 1 周に掛かる秒数

            _hueHandle = LMotion.Create(0f, 360f, duration)
                .WithLoops(-1, LoopType.Restart)
                .Bind(angle =>
                {
                    // 0..360 → -180..180 にラップ
                    float shifted = (angle <= 180f) ? angle : angle - 360f;
                    _cAdj.hueShift.value = shifted;
                })
                .AddTo(this);
        }

        private void CancelHueTween()
        {
            if (_hueHandle.IsActive()) _hueHandle.Cancel(); // ※ IsActive はプロパティ
            _currentSpeed = -1f;
            _cAdj.hueShift.value = 0f; // 基準色に戻す
        }
    }
}

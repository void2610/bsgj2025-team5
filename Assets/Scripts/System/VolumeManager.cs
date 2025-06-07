using R3;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class VolumeManager : MonoBehaviour
{
    [Tooltip("ポストプロセスボリューム。ここに設定されたプロファイルのエフェクトを制御します")]
    [SerializeField] private Volume volume;

    [Header("Color Adjustments")]
    [Tooltip("彩度の範囲。X:最低速度時、Y:最高速度時の値")]
    [SerializeField] private Vector2 saturationRange = new (0f, 60f);
    
    [Tooltip("露出の範囲。X:最低速度時、Y:最高速度時の値")]
    [SerializeField] private Vector2 exposureRange   = new (0f, 1.5f);

    [Header("Hue Shift")]
    [Tooltip("色相回転速度の範囲（度/秒）。X:闾値速度時、Y:最高速度時")]
    [SerializeField] private Vector2 hueShiftSpeedRange = new (0f, 15f);
    
    [Tooltip("色相回転が始まる速度の闾値（0-1）")]
    [SerializeField] private float   hueShiftThreshold  = 0.5f;

    [Header("Chromatic Aberration")]
    [Tooltip("色収差の強度範囲。X:最低速度時、Y:最高速度時の値")]
    [SerializeField] private Vector2 caIntensityRange = new (0f, 1f);

    [Header("Lens Distortion")]
    [Tooltip("レンズ歪みの強度範囲。X:最低速度時、Y:最高速度時の値（マイナス値で歪み）")]
    [SerializeField] private Vector2 ldIntensityRange = new (0f, -0.4f);

    private ColorAdjustments    _cAdj;
    private ChromaticAberration _ca;
    private LensDistortion      _ld;

    private void Awake()
    {
        volume.profile.TryGet(out _cAdj);
        volume.profile.TryGet(out _ca);
        volume.profile.TryGet(out _ld);
    }

    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedNorm.Subscribe(SetValue);
    }

    public void SetValue(float v)
    {
        v = Mathf.Clamp01(v);

        /* ── 基本エフェクト補間 ─────────────────── */
        _cAdj.saturation.value   = Mathf.Lerp(saturationRange.x, saturationRange.y, v);
        _cAdj.postExposure.value = Mathf.Lerp(exposureRange.x,   exposureRange.y,   v);
        _ca.intensity.value      = Mathf.Lerp(caIntensityRange.x, caIntensityRange.y, v);
        _ld.intensity.value      = Mathf.Lerp(ldIntensityRange.x, ldIntensityRange.y, v);

        /* ── Hue Shift ──────────────────────────── */
        if (v < hueShiftThreshold)
        {
            // 閾値未満：色相固定 0°
            _cAdj.hueShift.value = 0f;
            return;
        }

        /* 閾値以上：速度に比例した緩やかな色相シフト */
        var t01        = Mathf.InverseLerp(hueShiftThreshold, 1f, v);          // 0→1
        var degPerSec  = Mathf.Lerp(hueShiftSpeedRange.x, hueShiftSpeedRange.y, t01);
        var smoothTime = Time.time * 0.3f;                                     // 時間スケールを減少
        var rawAngle   = Mathf.Sin(smoothTime) * degPerSec;                    // sin波でスムーズな変化

        _cAdj.hueShift.value = rawAngle;
    }
}
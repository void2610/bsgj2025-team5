using R3;
using UnityEngine;

public class ScalableObject : MonoBehaviour
{
    [Header("Scale Settings")]
    [Tooltip("プレイヤーが停止している時のオブジェクトの大きさ（元のサイズに対する倍率）")]
    [SerializeField] private float minScale = 0.5f;
    
    [Tooltip("プレイヤーが最高速度の時のオブジェクトの大きさ（元のサイズに対する倍率）")]
    [SerializeField] private float maxScale = 2.0f;
    
    [Tooltip("速度と大きさの変化カーブ。横軸:速度(0-1)、縦軸:補間値(0-1)")]
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);
    
    private Vector3 _originalScale;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void OnChangePlayerSpeed(float speedNorm)
    {
        var curveValue = scaleCurve.Evaluate(speedNorm);
        var scaleMultiplier = Mathf.Lerp(minScale, maxScale, curveValue);
        transform.localScale = _originalScale * scaleMultiplier;
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedNorm.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }
}
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
    
    [Header("Position Adjustment")]
    [Tooltip("スケール変更時の位置補正値。オブジェクトの中心からの距離として適用されます")]
    [SerializeField] private float positionOffsetMultiplier = 0.5f;
    
    [Tooltip("位置補正を適用する方向（通常はY軸のみ）")]
    [SerializeField] private Vector3 positionOffsetDirection = Vector3.up;
    
    private Vector3 _originalScale;
    private Vector3 _originalPosition;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _originalPosition = transform.position;
    }

    private void OnChangePlayerSpeed(float speedNorm)
    {
        var curveValue = scaleCurve.Evaluate(speedNorm);
        var scaleMultiplier = Mathf.Lerp(minScale, maxScale, curveValue);
        
        // スケール変更前の補正値を計算
        var scaleDifference = scaleMultiplier - 1.0f;
        var positionOffset = positionOffsetDirection * (scaleDifference * positionOffsetMultiplier);
        
        // スケールと位置を同時に更新
        transform.localScale = _originalScale * scaleMultiplier;
        transform.position = _originalPosition + positionOffset;
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedNorm.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }
}
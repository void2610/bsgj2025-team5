using R3;
using UnityEngine;

public class ScalableObject : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 2.0f;
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
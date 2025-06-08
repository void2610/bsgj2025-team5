using System;
using R3;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ConcentrationLineController : MonoBehaviour
{
    [Tooltip("最大マウス速度（この値で集中線が最大強度になる）")]
    [SerializeField] private float maxMouseSpeed = 200f;
    
    [Tooltip("集中線の強度変化の滑らかさ（0-1、大きいほど滑らか）")]
    [SerializeField] private float smoothing = 0.1f;

    [Header("Material Properties")]
    [Tooltip("集中線のEdgeを制御するプロパティ名")]
    [SerializeField] private string edgePropertyName = "_Edge";
    
    [Tooltip("Edgeプロパティの最小値")]
    [SerializeField] private float edgeMinValue = 0f;
    
    [Tooltip("Edgeプロパティの最大値")]
    [SerializeField] private float edgeMaxValue = 1f;
    
    private Material _concentrationLineMaterial;
    private float _currentIntensity;
    private IDisposable _subscription;

    private void Awake()
    {
        // マテリアルのインスタンスを作成
        var image = this.GetComponent<Image>();
        _concentrationLineMaterial = new Material(image.material); 
        image.material = _concentrationLineMaterial;
    }

    private void Start()
    {
        var player = GameManager.Instance.Player;
        // マウス速度を購読してマテリアルプロパティを更新
        _subscription = player.MouseSpeed
            .Subscribe(mouseSpeed =>
            {
                UpdateConcentrationLineEffect(mouseSpeed);
            })
            .AddTo(this);
    }

    private void UpdateConcentrationLineEffect(float mouseSpeed)
    {
        Debug.Log($"Mouse Speed: {mouseSpeed}");
        // マウス速度を0-1の範囲に正規化
        var normalizedSpeed = Mathf.Clamp01(mouseSpeed / maxMouseSpeed);
        
        // スムージング処理
        _currentIntensity = Mathf.Lerp(_currentIntensity, normalizedSpeed, smoothing);
        
        // Edgeプロパティを更新（最小値から最大値の範囲にマッピング）
        if (_concentrationLineMaterial.HasProperty(edgePropertyName))
        {
            // マウスが速いほどEdgeが小さくなるように反転
            var edgeValue = Mathf.Lerp(edgeMaxValue, edgeMinValue, _currentIntensity);
            _concentrationLineMaterial.SetFloat(edgePropertyName, edgeValue);
        }
    }

    private void OnDestroy()
    {
        _subscription?.Dispose();
        
        // マテリアルインスタンスの破棄
        if (_concentrationLineMaterial)
        {
            Destroy(_concentrationLineMaterial);
        }
    }
}
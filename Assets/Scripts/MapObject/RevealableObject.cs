using R3;
using UnityEngine;

public class RevealableObject : MonoBehaviour
{
    [Tooltip("オブジェクトが表示される最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;
    
    [Tooltip("ONの場合、指定速度以下で表示、OFFの場合、指定速度以上で表示")]
    [SerializeField] private bool invertBehavior = false;
    
    [Tooltip("パーティクルのプレハブ（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Tooltip("表示状態の透明度")]
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
    
    [Tooltip("非表示状態の透明度")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0.5f;
    
    private Material _material;
    private Collider _collider;
    private static readonly int _baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int _surface = Shader.PropertyToID("_Surface");
    private static readonly int _blend = Shader.PropertyToID("_Blend");
    private static readonly int _srcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int _dstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int _zWrite = Shader.PropertyToID("_ZWrite");
    private Color _originalColor;

    private void OnChangePlayerSpeed(int s)
    {
        var shouldBeActive = invertBehavior ? s <= requiredSpeed : s >= requiredSpeed;
        if (shouldBeActive)
        {
            var color = _originalColor;
            color.a = visibleAlpha;
            _material.SetColor(_baseColor, color);
            _collider.isTrigger = false;
            
            // 透明度が1の場合は不透明モードに切り替え
            if (visibleAlpha >= 0.99f)
            {
                SetOpaqueMode();
            }
            else
            {
                SetTransparentMode();
            }
        }
        else
        {
            var color = _originalColor;
            color.a = hiddenAlpha;
            _material.SetColor(_baseColor, color);
            _collider.isTrigger = true;
            
            // 半透明の場合は透明モードに切り替え
            SetTransparentMode();
        }
    }
    
    private void SetOpaqueMode()
    {
        _material.SetFloat(_surface, 0); // Opaque
        _material.SetFloat(_zWrite, 1);
        _material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.DisableKeyword("_ALPHABLEND_ON");
        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }
    
    private void SetTransparentMode()
    {
        _material.SetFloat(_surface, 1); // Transparent
        _material.SetFloat(_blend, 0);
        _material.SetFloat(_srcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetFloat(_dstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetFloat(_zWrite, 0);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.EnableKeyword("_ALPHABLEND_ON");
        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        var rend = this.GetComponent<Renderer>();
        _material = new Material(rend.material);
        rend.material = _material;
        
        _collider = this.GetComponent<Collider>();
        
        _originalColor = _material.GetColor(_baseColor);
        
        // 初期状態は不透明モードで開始
        SetOpaqueMode();
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }
}
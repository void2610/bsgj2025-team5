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
            color.a = 1f;
            _material.SetColor(_baseColor, color);
            _collider.isTrigger = false;
        }
        else
        {
            var color = _originalColor;
            color.a = 0.6f;
            _material.SetColor(_baseColor, color);
            _collider.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        var rend = this.GetComponent<Renderer>();
        _material = new Material(rend.material);
        rend.material = _material;
        
        _collider = this.GetComponent<Collider>();
        
        _originalColor = _material.GetColor(_baseColor);
        
        // マテリアルを透明にするための設定
        _material.SetFloat(_surface, 1); // 0 = Opaque, 1 = Transparent
        _material.SetFloat(_blend, 0);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply
        _material.SetFloat(_srcBlend, (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _material.SetFloat(_dstBlend, (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _material.SetFloat(_zWrite, 0);
        _material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        _material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }
}
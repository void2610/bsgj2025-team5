using R3;
using UnityEngine;

public class Item : MonoBehaviour
{
    [Tooltip("オブジェクトが表示される最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed;
    
    [Tooltip("ONの場合、指定速度以下で表示、OFFの場合、指定速度以上で表示")]
    [SerializeField] private bool invertBehavior;
    
    [Tooltip("パーティクルのプレハブ（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Tooltip("表示状態の透明度")]
    [SerializeField, Range(0f, 1f)] private float visibleAlpha = 1f;
    
    [Tooltip("非表示状態の透明度")]
    [SerializeField, Range(0f, 1f)] private float hiddenAlpha = 0.5f;
    
    private Material _material;
    private static readonly int _baseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int _surface = Shader.PropertyToID("_Surface");
    private static readonly int _blend = Shader.PropertyToID("_Blend");
    private static readonly int _srcBlend = Shader.PropertyToID("_SrcBlend");
    private static readonly int _dstBlend = Shader.PropertyToID("_DstBlend");
    private static readonly int _zWrite = Shader.PropertyToID("_ZWrite");
    private Color _originalColor;
    private bool _isVisible = true;

    private void OnChangePlayerSpeed(int s)
    {
        var shouldBeActive = invertBehavior ? s <= requiredSpeed : s >= requiredSpeed;
        if (shouldBeActive)
        {
            var color = _originalColor;
            color.a = visibleAlpha;
            _material.SetColor(_baseColor, color);
            _isVisible = true;
        }
        else
        {
            var color = _originalColor;
            color.a = hiddenAlpha;
            _material.SetColor(_baseColor, color);
            _isVisible = false;
        }
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        var rend = this.GetComponent<Renderer>();
        
        // 新しいマテリアルインスタンスを作成
        _material = new Material(rend.material);
        rend.material = _material;
        
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
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<Player>(out _))
        {
            // 表示状態の時のみ取得可能
            if (!_isVisible) return;

            // アイテムとった座標をGameManagerに渡して、プレイヤーのリスポーン地点にする。
            GameManager.Instance.AddItemCount(new Vector3(transform.position.x,transform.position.y,transform.position.z));
            
            Destroy(gameObject);
        }
    }
}
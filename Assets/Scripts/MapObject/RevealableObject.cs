using R3;
using UnityEngine;
using LitMotion;

public class RevealableObject : MonoBehaviour
{
    [Tooltip("オブジェクトが表示される最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed = 0;
    
    [Tooltip("ONの場合、指定速度以下で表示、OFFの場合、指定速度以上で表示")]
    [SerializeField] private bool invertBehavior = false;
    
    [Tooltip("パーティクルのプレハブ（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Tooltip("ディゾルブマテリアル")]
    [SerializeField] private Material dissolveMaterial;
    
    [Tooltip("ディゾルブアニメーションの継続時間")]
    [SerializeField] private float dissolveDuration = 2.0f;
    
    private Material _originalMaterial;
    private Material _materialInstance;
    private Collider _collider;
    private Renderer _renderer;
    private MotionHandle _currentMotion;
    private bool _isRevealed = false;
    
    private static readonly int _dissolveAmount = Shader.PropertyToID("_Dissolve");
    private static readonly int _mainTex = Shader.PropertyToID("_MainTex");

    private void OnChangePlayerSpeed(int s)
    {
        var shouldBeActive = invertBehavior ? s <= requiredSpeed : s >= requiredSpeed;
        if (shouldBeActive && !_isRevealed)
        {
            RevealObject();
        }
        else if (!shouldBeActive && _isRevealed)
        {
            HideObject();
        }
    }
    
    /// <summary>
    /// オブジェクトをディゾルブ効果で出現させる
    /// </summary>
    private void RevealObject()
    {
        _isRevealed = true;
        _collider.isTrigger = false;
        
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブマテリアルに切り替え
        _materialInstance = new Material(dissolveMaterial);
        _renderer.material = _materialInstance;
        
        // ディゾルブアニメーション開始（0→1で出現）
        _currentMotion = LMotion.Create(0f, 1f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .Bind(value => _materialInstance?.SetFloat(_dissolveAmount, value))
            .AddTo(this);
    }
    
    /// <summary>
    /// オブジェクトをディゾルブ効果で消失させる
    /// </summary>
    private void HideObject()
    {
        _isRevealed = false;
        _collider.isTrigger = true;
        
        // 既存のアニメーションを停止
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブアニメーション開始
        _currentMotion = LMotion.Create(1, 0f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() => { _renderer.material = _originalMaterial; })
            .Bind(value => _materialInstance?.SetFloat(_dissolveAmount, value))
            .AddTo(this);
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        _renderer = this.GetComponent<Renderer>();
        _originalMaterial = _renderer.material;
        Debug.Log(_originalMaterial.mainTexture.name);
        _collider = this.GetComponent<Collider>();
        
        // 初期状態では非表示（ディゾルブ値0）
        _materialInstance = new Material(dissolveMaterial);
        _materialInstance.SetFloat(_dissolveAmount, 0f);
        _materialInstance.SetTexture(_mainTex, _originalMaterial.mainTexture);
        _renderer.material = _materialInstance;
        
        _isRevealed = false;
        _collider.isTrigger = true;
    }
        
    private void Start()
    {
        GameManager.Instance.Player.PlayerSpeedInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }

    private void OnDestroy()
    {
        // アニメーションを停止
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        // マテリアルインスタンスを破棄
        if (_materialInstance) DestroyImmediate(_materialInstance);
    }
}
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
    private Material _originalOutlineMaterial;
    private Material _materialInstance;
    private Material _outlineMaterialInstance;
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
        
        _collider.enabled = true;
        
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブマテリアルに切り替え
        _materialInstance.SetFloat(_dissolveAmount, 0f);
        if (_outlineMaterialInstance)
            _outlineMaterialInstance.SetFloat(_dissolveAmount, 0f);
        
        var materials = _renderer.materials;
        materials[0] = _materialInstance;
        if (materials.Length > 1 && _outlineMaterialInstance != null)
            materials[1] = _outlineMaterialInstance;
        _renderer.materials = materials;
        
        // ディゾルブアニメーション開始（0→1で出現）
        _currentMotion = LMotion.Create(0f, 1f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() => { 
                var ms = _renderer.materials;
                ms[0] = _originalMaterial;
                if (ms.Length > 1 && _originalOutlineMaterial != null)
                    ms[1] = _originalOutlineMaterial;
                _renderer.materials = ms;
            })
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
                _outlineMaterialInstance?.SetFloat(_dissolveAmount, value);
            })
            .AddTo(this);
    }
    
    /// <summary>
    /// オブジェクトをディゾルブ効果で消失させる
    /// </summary>
    private void HideObject()
    {
        _isRevealed = false;
        
        _collider.enabled = false;
        
        // 既存のアニメーションを停止
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブマテリアルに切り替え
        _materialInstance.SetFloat(_dissolveAmount, 1f);
        if (_outlineMaterialInstance != null)
            _outlineMaterialInstance.SetFloat(_dissolveAmount, 1f);
        
        var materials = _renderer.materials;
        materials[0] = _materialInstance;
        if (materials.Length > 1 && _outlineMaterialInstance != null)
            materials[1] = _outlineMaterialInstance;
        _renderer.materials = materials;
        
        // ディゾルブアニメーション開始（1→0で消失）
        _currentMotion = LMotion.Create(1f, 0f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
                _outlineMaterialInstance?.SetFloat(_dissolveAmount, value);
            })
            .AddTo(this);
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        _renderer = this.GetComponent<Renderer>();
        var materials = _renderer.materials;
        _originalMaterial = materials[0];
        if (materials.Length > 1)
            _originalOutlineMaterial = materials[1];
        _collider = this.GetComponent<Collider>();
        
        // マテリアルインスタンスの準備（初期状態の設定はStart()で行う）
        _materialInstance = new Material(dissolveMaterial);
        _materialInstance.SetTexture(_mainTex, _originalMaterial.mainTexture);
        
        // アウトライン用マテリアルインスタンスの準備
        if (_originalOutlineMaterial)
        {
            _outlineMaterialInstance = new Material(dissolveMaterial);
            _outlineMaterialInstance.SetTexture(_mainTex, _originalOutlineMaterial.mainTexture);
        }
    }
        
    private void Start()
    {
        // 初期速度を取得（通常は0）
        var initialSpeed = GameManager.Instance.Player.PlayerItemCountInt.CurrentValue;
        
        // 初期状態を設定
        var shouldBeActiveInitially = invertBehavior ? initialSpeed <= requiredSpeed : initialSpeed >= requiredSpeed;
        
        if (shouldBeActiveInitially)
        {
            // 初期状態で表示する場合
            _isRevealed = true;
            _collider.enabled = true;
            _materialInstance.SetFloat(_dissolveAmount, 1f);
            var materials = _renderer.materials;
            materials[0] = _originalMaterial;
            if (materials.Length > 1 && _originalOutlineMaterial != null)
                materials[1] = _originalOutlineMaterial;
            _renderer.materials = materials;
        }
        else
        {
            // 初期状態で非表示の場合
            _isRevealed = false;
            _collider.enabled = false;
            _materialInstance.SetFloat(_dissolveAmount, 0f);
            if (_outlineMaterialInstance != null)
                _outlineMaterialInstance.SetFloat(_dissolveAmount, 0f);
            var materials = _renderer.materials;
            materials[0] = _materialInstance;
            if (materials.Length > 1 && _outlineMaterialInstance != null)
                materials[1] = _outlineMaterialInstance;
            _renderer.materials = materials;
        }
        
        // 速度変化の購読を開始
        GameManager.Instance.Player.PlayerItemCountInt.Subscribe(OnChangePlayerSpeed).AddTo(this);
    }

    private void OnDestroy()
    {
        // アニメーションを停止
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        // マテリアルインスタンスを破棄
        if (_materialInstance) DestroyImmediate(_materialInstance);
        if (_outlineMaterialInstance) DestroyImmediate(_outlineMaterialInstance);
    }
}
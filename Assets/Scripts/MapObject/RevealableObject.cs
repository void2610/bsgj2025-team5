using R3;
using UnityEngine;
using LitMotion;

/// <summary>
/// プレイヤーの速度に応じてオブジェクトをディゾルブ効果で表示/非表示にする
/// 3D（Renderer）と2D（SpriteRenderer）の両方に対応
/// </summary>
public class RevealableObject : MonoBehaviour
{
    [Tooltip("オブジェクトが表示される最低速度レベル（0:停止〜4:最高速）")]
    [SerializeField, Range(0, 4)] private int requiredSpeed;
    
    [Tooltip("ONの場合、指定速度以下で表示、OFFの場合、指定速度以上で表示")]
    [SerializeField] private bool invertBehavior;
    
    [Tooltip("パーティクルのプレハブ（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Tooltip("ディゾルブマテリアル")]
    [SerializeField] private Material dissolveMaterial;
    
    [Tooltip("ディゾルブアニメーションの継続時間")]
    [SerializeField] private float dissolveDuration = 2.0f;
    
    // 共通フィールド
    private Material _originalMaterial;
    private Material _materialInstance;
    private Collider _collider;
    private MotionHandle _currentMotion;
    private bool _isRevealed;
    private bool _is2D;
    
    // 3D専用フィールド
    private Material _originalOutlineMaterial;
    private Material _outlineMaterialInstance;
    private Renderer _renderer;
    
    // 2D専用フィールド
    private SpriteRenderer _spriteRenderer;
    
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
        
        // コライダーを有効化
        _collider.enabled = true;
        
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブマテリアルに切り替え
        _materialInstance.SetFloat(_dissolveAmount, 0f);
        
        if (_is2D)
        {
            _spriteRenderer.material = _materialInstance;
        }
        else
        {
            if (_outlineMaterialInstance)
                _outlineMaterialInstance.SetFloat(_dissolveAmount, 0f);
            
            var materials = _renderer.materials;
            materials[0] = _materialInstance;
            if (materials.Length > 1 && _outlineMaterialInstance)
                materials[1] = _outlineMaterialInstance;
            _renderer.materials = materials;
        }
        
        // ディゾルブアニメーション開始（0→1で出現）
        _currentMotion = LMotion.Create(0f, 1f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() => { 
                if (_is2D)
                {
                    _spriteRenderer.material = _originalMaterial;
                }
                else
                {
                    var ms = _renderer.materials;
                    ms[0] = _originalMaterial;
                    if (ms.Length > 1 && _originalOutlineMaterial)
                        ms[1] = _originalOutlineMaterial;
                    _renderer.materials = ms;
                }
            })
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
                if (!_is2D && _outlineMaterialInstance)
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
        
        // コライダーを無効化
        _collider.enabled = false;
        
        // 既存のアニメーションを停止
        if (_currentMotion.IsActive()) _currentMotion.Cancel();
        
        // ディゾルブマテリアルに切り替え
        _materialInstance.SetFloat(_dissolveAmount, 1f);
        
        if (_is2D)
        {
            _spriteRenderer.material = _materialInstance;
        }
        else
        {
            if (_outlineMaterialInstance)
                _outlineMaterialInstance.SetFloat(_dissolveAmount, 1f);
            
            var materials = _renderer.materials;
            materials[0] = _materialInstance;
            if (materials.Length > 1 && _outlineMaterialInstance)
                materials[1] = _outlineMaterialInstance;
            _renderer.materials = materials;
        }
        
        // ディゾルブアニメーション開始（1→0で消失）
        _currentMotion = LMotion.Create(1f, 0f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
                if (!_is2D && _outlineMaterialInstance)
                    _outlineMaterialInstance?.SetFloat(_dissolveAmount, value);
            })
            .AddTo(this);
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        // レンダラータイプを判定
        _spriteRenderer = this.GetComponent<SpriteRenderer>();
        if (_spriteRenderer)
        {
            _is2D = true;
            _originalMaterial = _spriteRenderer.material;
            _collider = this.GetComponent<Collider>();
            
            // マテリアルインスタンスの準備
            _materialInstance = new Material(dissolveMaterial);
            // スプライトのテクスチャを設定
            if (_spriteRenderer.sprite)
            {
                _materialInstance.SetTexture(_mainTex, _spriteRenderer.sprite.texture);
            }
        }
        else
        {
            _is2D = false;
            _renderer = this.GetComponent<Renderer>();
            if (!_renderer)
            {
                Debug.LogError("RevealableObject requires either a Renderer or SpriteRenderer component!");
                enabled = false;
                return;
            }
            
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
            _materialInstance.SetFloat(_dissolveAmount, 1f);
            
            _collider.enabled = true;
            
            if (_is2D)
            {
                _spriteRenderer.material = _originalMaterial;
            }
            else
            {
                var materials = _renderer.materials;
                materials[0] = _originalMaterial;
                if (materials.Length > 1 && _originalOutlineMaterial)
                    materials[1] = _originalOutlineMaterial;
                _renderer.materials = materials;
            }
        }
        else
        {
            // 初期状態で非表示の場合
            _isRevealed = false;
            _materialInstance.SetFloat(_dissolveAmount, 0f);
            
            _collider.enabled = false;
            
            if (_is2D)
            {
                _spriteRenderer.material = _materialInstance;
            }
            else
            {
                if (_outlineMaterialInstance)
                    _outlineMaterialInstance.SetFloat(_dissolveAmount, 0f);
                var materials = _renderer.materials;
                materials[0] = _materialInstance;
                if (materials.Length > 1 && _outlineMaterialInstance)
                    materials[1] = _outlineMaterialInstance;
                _renderer.materials = materials;
            }
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
        if (!_is2D && _outlineMaterialInstance) DestroyImmediate(_outlineMaterialInstance);
    }
}
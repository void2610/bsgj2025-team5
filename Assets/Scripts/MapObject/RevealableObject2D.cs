using R3;
using UnityEngine;
using LitMotion;

/// <summary>
/// SpriteRenderer用の2D版RevealableObject
/// プレイヤーの速度に応じてスプライトをディゾルブ効果で表示/非表示にする
/// </summary>
public class RevealableObject2D : MonoBehaviour
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
    
    private Material _originalMaterial;
    private Material _materialInstance;
    private Collider _collider;
    private SpriteRenderer _spriteRenderer;
    private MotionHandle _currentMotion;
    private bool _isRevealed;
    
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
        _spriteRenderer.material = _materialInstance;
        
        // ディゾルブアニメーション開始（0→1で出現）
        _currentMotion = LMotion.Create(0f, 1f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .WithOnComplete(() => { 
                _spriteRenderer.material = _originalMaterial;
            })
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
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
        _spriteRenderer.material = _materialInstance;
        
        // ディゾルブアニメーション開始（1→0で消失）
        _currentMotion = LMotion.Create(1f, 0f, dissolveDuration)
            .WithEase(Ease.OutQuad)
            .Bind(value => {
                _materialInstance?.SetFloat(_dissolveAmount, value);
            })
            .AddTo(this);
    }

    private void Awake()
    {
        if (particlePrefab) Instantiate(particlePrefab, this.transform.position, Quaternion.identity);
        
        _spriteRenderer = this.GetComponent<SpriteRenderer>();
        _originalMaterial = _spriteRenderer.material;
        _collider = this.GetComponent<Collider>();
        
        // マテリアルインスタンスの準備（初期状態の設定はStart()で行う）
        _materialInstance = new Material(dissolveMaterial);
        // スプライトのテクスチャを設定
        if (_spriteRenderer.sprite)
        {
            _materialInstance.SetTexture(_mainTex, _spriteRenderer.sprite.texture);
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
            _spriteRenderer.material = _originalMaterial;
        }
        else
        {
            // 初期状態で非表示の場合
            _isRevealed = false;
            _collider.enabled = false;
            _materialInstance.SetFloat(_dissolveAmount, 0f);
            _spriteRenderer.material = _materialInstance;
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
    }
}
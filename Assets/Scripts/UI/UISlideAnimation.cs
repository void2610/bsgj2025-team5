using UnityEngine;
using LitMotion;
using Cysharp.Threading.Tasks;

/// <summary>
/// UIが画面外からスライドしてくるアニメーション
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UISlideAnimation : MonoBehaviour
{
    [System.Serializable]
    public enum SlideDirection
    {
        Left,   // 左から右へ
        Right,  // 右から左へ
        Up,     // 上から下へ
        Down    // 下から上へ
    }
    
    [Header("スライド設定")]
    [Tooltip("スライドの方向")]
    [SerializeField] private SlideDirection slideDirection = SlideDirection.Left;
    
    [Tooltip("スライドする距離（ピクセル）")]
    [SerializeField] private float slideDistance = 1000f;
    
    [Tooltip("アニメーションの継続時間（秒）")]
    [SerializeField] private float duration = 0.8f;
    
    [Tooltip("アニメーションの遅延時間（秒）")]
    [SerializeField] private float delay = 0f;
    
    [Tooltip("アニメーションのイージング")]
    [SerializeField] private Ease ease = Ease.OutCubic;
    
    [Header("デバッグ")]
    [Tooltip("開始時に自動でスライドアニメーションを実行")]
    [SerializeField] private bool autoStart = false;
    
    private RectTransform _rectTransform;
    private Vector2 _originalPosition;
    private Vector2 _startPosition;
    private bool _isInitialized = false;
    
    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        Initialize();
    }
    
    private void Start()
    {
        if (autoStart)
        {
            StartSlideAnimationAsync().Forget();
        }
    }
    
    /// <summary>
    /// アニメーションの初期化
    /// </summary>
    private void Initialize()
    {
        if (_isInitialized) return;
        
        // 元の位置を記録
        _originalPosition = _rectTransform.anchoredPosition;
        
        // スライド開始位置を計算
        _startPosition = _originalPosition + GetSlideOffset();
        
        // UI要素を開始位置に移動（画面外）
        _rectTransform.anchoredPosition = _startPosition;
        
        _isInitialized = true;
    }
    
    /// <summary>
    /// スライド方向に基づいてオフセットを計算
    /// </summary>
    private Vector2 GetSlideOffset()
    {
        return slideDirection switch
        {
            SlideDirection.Left => new Vector2(-slideDistance, 0),
            SlideDirection.Right => new Vector2(slideDistance, 0),
            SlideDirection.Up => new Vector2(0, slideDistance),
            SlideDirection.Down => new Vector2(0, -slideDistance),
            _ => Vector2.zero
        };
    }
    
    /// <summary>
    /// スライドアニメーションを開始（非同期）
    /// </summary>
    public async UniTask StartSlideAnimationAsync()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        // 遅延がある場合は待機
        if (delay > 0f)
        {
            await UniTask.Delay((int)(delay * 1000));
        }
        
        // スライドアニメーションを実行
        await LMotion.Create(_startPosition, _originalPosition, duration)
            .WithEase(ease)
            .Bind(pos => _rectTransform.anchoredPosition = pos)
            .AddTo(this);
    }
    
    /// <summary>
    /// スライドアウトアニメーションを開始（非同期）
    /// 現在位置から画面外へ移動
    /// </summary>
    public async UniTask StartSlideOutAnimationAsync()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        // 現在の位置を取得
        var currentPosition = _rectTransform.anchoredPosition;
        
        // スライドアウト終了位置を計算（画面外）
        var outPosition = currentPosition + GetSlideOffset();
        
        // スライドアウトアニメーションを実行
        await LMotion.Create(currentPosition, outPosition, duration)
            .WithEase(ease)
            .Bind(pos => _rectTransform.anchoredPosition = pos)
            .AddTo(this);
    }
    
    /// <summary>
    /// UI要素を元の位置に瞬間移動
    /// </summary>
    public void ResetToOriginalPosition()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        _rectTransform.anchoredPosition = _originalPosition;
    }
    
    /// <summary>
    /// UI要素を開始位置に瞬間移動
    /// </summary>
    public void ResetToStartPosition()
    {
        if (!_isInitialized)
        {
            Initialize();
        }
        
        _rectTransform.anchoredPosition = _startPosition;
    }
    
    /// <summary>
    /// エディタ用：設定を変更した時に初期化をリセット
    /// </summary>
    private void OnValidate()
    {
        if (Application.isPlaying && _isInitialized)
        {
            _isInitialized = false;
            Initialize();
        }
    }
}
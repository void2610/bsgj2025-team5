using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;

/// <summary>
/// レベルごとのスケール設定
/// </summary>
[System.Serializable]
public class ScaleLevelSettings
{
    [Tooltip("1秒あたりの往復回数")]
    public float speed = 1f;
    
    [Tooltip("最小スケール（元サイズに対する倍率）")]
    public float minScale = 0.8f;
    
    [Tooltip("最大スケール（元サイズに対する倍率）")]
    public float maxScale = 1.2f;
    
    [Tooltip("スケール変化のイージング")]
    public Ease easing = Ease.InOutSine;
}

/// <summary>
/// プレイヤーの速度に応じてyoyoのような往復スケール変化をするオブジェクト
/// </summary>
public class YoyoScaleObject : MonoBehaviour
{
    [Header("レベル別スケール設定")]
    [Tooltip("各PlayerItemCountレベルでのスケール設定")]
    [SerializeField] private ScaleLevelSettings[] levelSettings = new ScaleLevelSettings[5];
    
    // 内部変数
    private ScaleLevelSettings _currentSettings;
    private MotionHandle _scaleMotion;
    private Vector3 _originalScale;
    
    private void Awake()
    {
        // 元のスケールを保存
        _originalScale = transform.localScale;
    }
    
    private void Start()
    {
        // プレイヤーのアイテム数変化を購読
        GameManager.Instance.Player.PlayerItemCountInt
            .Subscribe(OnChangePlayerItemCount)
            .AddTo(this);
        
        // 初期速度を設定
        OnChangePlayerItemCount(GameManager.Instance.Player.PlayerItemCountInt.CurrentValue);
    }
    
    /// <summary>
    /// プレイヤーアイテム数が変化した時の処理
    /// </summary>
    private void OnChangePlayerItemCount(int itemCount)
    {
        // アイテム数を配列のインデックスに変換（0-4の範囲）
        int levelIndex = Mathf.Clamp(itemCount, 0, levelSettings.Length - 1);
        _currentSettings = levelSettings[levelIndex];
        
        // スケールモーションを即座に更新
        UpdateScale();
    }
    
    /// <summary>
    /// スケールモーションを更新
    /// </summary>
    private void UpdateScale()
    {
        // 既存のスケールモーションを停止
        if (_scaleMotion != null && _scaleMotion.IsActive()) _scaleMotion.Cancel();
        
        // 設定がない場合は停止
        if (_currentSettings == null) return;
        
        // スケール速度が0に近い場合は停止
        if (Mathf.Abs(_currentSettings.speed) < 0.01f) return;
        
        // スケール時間を計算（1往復あたりの時間）
        float duration = 1f / Mathf.Abs(_currentSettings.speed);
        
        // 最小・最大スケールを計算
        Vector3 minScaleVector = _originalScale * _currentSettings.minScale;
        Vector3 maxScaleVector = _originalScale * _currentSettings.maxScale;
        
        // 新しいスケールモーションを開始（yoyoで往復）
        _scaleMotion = LMotion.Create(minScaleVector, maxScaleVector, duration)
            .WithLoops(-1, LoopType.Yoyo)
            .WithEase(_currentSettings.easing)
            .BindToLocalScale(transform)
            .AddTo(this);
    }
    
    /// <summary>
    /// クリーンアップ
    /// </summary>
    private void OnDestroy()
    {
        if (_scaleMotion.IsActive()) _scaleMotion.Cancel();
    }
}
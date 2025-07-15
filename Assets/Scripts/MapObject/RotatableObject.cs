using LitMotion;
using LitMotion.Extensions;
using R3;
using UnityEngine;

/// <summary>
/// プレイヤーの速度に応じて回転運動をするオブジェクト
/// </summary>
public class RotatableObject : MonoBehaviour
{
    [Header("回転設定")]
    [Tooltip("各PlayerItemCountレベルでの1秒あたりの回転数")]
    [SerializeField] private float[] rotationSpeedsPerLevel = { 0f, 0.25f, 0.5f, 0.75f, 1f };
    
    [Header("回転範囲")]
    [Tooltip("回転の開始角度")]
    [SerializeField] private Vector3 startAngle = Vector3.zero;
    
    [Tooltip("回転の終了角度")]
    [SerializeField] private Vector3 endAngle = new(0, 360, 0);
    
    [Header("オプション")]
    [Tooltip("ランダムな初期回転角度を設定")]
    [SerializeField] private bool randomStartRotation;
    
    // 内部変数
    private float _currentRotationSpeed;
    private MotionHandle _rotationMotion;
    
    private void Awake()
    {
        // ランダムな初期回転を設定
        if (randomStartRotation)
        {
            float randomAngle = Random.Range(0f, 360f);
            Vector3 randomRotation = Vector3.Lerp(startAngle, endAngle, randomAngle / 360f);
            transform.rotation = Quaternion.Euler(randomRotation);
        }
        else
        {
            transform.rotation = Quaternion.Euler(startAngle);
        }
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
        int levelIndex = Mathf.Clamp(itemCount, 0, rotationSpeedsPerLevel.Length - 1);
        _currentRotationSpeed = rotationSpeedsPerLevel[levelIndex];
        
        // 回転モーションを即座に更新
        UpdateRotation();
    }
    
    /// <summary>
    /// 回転モーションを更新
    /// </summary>
    private void UpdateRotation()
    {
        // 既存の回転モーションを停止
        if (_rotationMotion.IsActive()) _rotationMotion.Cancel();
        
        // 回転速度が0に近い場合は停止
        if (Mathf.Abs(_currentRotationSpeed) < 0.01f) return;
        
        // 回転時間を計算（1回転あたりの時間）
        float duration = 1f / Mathf.Abs(_currentRotationSpeed);
        
        // 新しい回転モーションを開始
        _rotationMotion = LMotion.Create(startAngle, endAngle, duration)
            .WithLoops(-1)
            .WithEase(Ease.Linear)
            .BindToEulerAngles(transform)
            .AddTo(this);
    }
    
    /// <summary>
    /// クリーンアップ
    /// </summary>
    private void OnDestroy()
    {
        if (_rotationMotion.IsActive()) _rotationMotion.Cancel();
    }
    
}
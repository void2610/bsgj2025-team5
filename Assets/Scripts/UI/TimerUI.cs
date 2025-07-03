using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions;
using System;

public class TimerUI : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI mainTimerText;
    [SerializeField] private TextMeshProUGUI timePenaltyTextPrefab;
    [SerializeField] private Transform timePenaltyTextSpawnPoint;
    
    [Header("点滅設定")]
    [SerializeField] private float flashThresholdTime = 60.0f;
    
    private readonly Color _timerFlashColor = Color.red;
    private readonly float _timerFlashDuration = 0.5f;
    private readonly float _timePenaltyAnimationDuration = 1.0f;
    private readonly float _timePenaltyMoveAmount = 50f;
    
    private MotionHandle _currentFlashMotionHandle;
    private Color _originalTimerTextColor;

    private void Awake()
    {
        _originalTimerTextColor = mainTimerText.color;
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerのインスタンスが見つかりません。シングルトンが正しく初期化されているか確認してください。", this);
            return;
        }

        // タイマーにかかるアニメーションの登録
        GameManager.Instance.OnTimeChanged
            .Subscribe(v => UpdateTimer(v))
            .AddTo(this);

        // 時間ペナルティ発生イベントの登録
        GameManager.Instance.OnHappenTimePenalty
            .Subscribe(amount => ShowTimePenaltyAnimation(amount))
            .AddTo(this);
    }

    private void UpdateTimer(float v)
    {
        UpdateTimerText(v);
        
        if (v <= flashThresholdTime && v > 0f)
        {
            StartTimerFlashAnimation();
        }
        else
        {
            StopTimerFlashAnimation();
        }
    }

    private void UpdateTimerText(float currentTime)
    {
        int minutes = Mathf.Max(0, Mathf.FloorToInt(currentTime / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(currentTime % 60));
        mainTimerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 残り時間が閾値以下になった場合にタイマーテキストの点滅アニメーションを開始する
    /// </summary>
    private void StartTimerFlashAnimation()
    {
        if (_currentFlashMotionHandle.IsActive()) return;
        
        _currentFlashMotionHandle = LMotion.Create(_originalTimerTextColor, _timerFlashColor, _timerFlashDuration)
            .WithEase(Ease.OutSine)
            .WithLoops(-1, LoopType.Yoyo)
            .BindToColor(mainTimerText)
            .AddTo(this);
    }

    /// <summary>
    /// タイマー点滅アニメーションを停止し、テキストの色を元に戻す
    /// </summary>
    private void StopTimerFlashAnimation()
    {
        // 点滅している場合モーションをキャンセルして、テキストを元の色に戻す
        if (_currentFlashMotionHandle.IsActive())
        {
            _currentFlashMotionHandle.Cancel();
            mainTimerText.color = _originalTimerTextColor;
        }
    }

    /// <summary>
    /// 時間ペナルティー量を示すテキストを生成し、アニメーション表示します。
    /// </summary>
    private void ShowTimePenaltyAnimation(float amount)
    {
        if (timePenaltyTextPrefab == null) return;
        
        TextMeshProUGUI penaltyTextInstance = Instantiate(timePenaltyTextPrefab,
            timePenaltyTextSpawnPoint.position,
            Quaternion.identity, timePenaltyTextSpawnPoint);
        penaltyTextInstance.gameObject.SetActive(true);
        penaltyTextInstance.text = $"-{Mathf.CeilToInt(amount)}";

        // 初期色を点滅色（赤）に設定
        Color initialColor = _timerFlashColor;
        penaltyTextInstance.color = initialColor;

        // 垂直方向への移動アニメーション
        LMotion.Create(penaltyTextInstance.rectTransform.anchoredPosition,
                penaltyTextInstance.rectTransform.anchoredPosition + new Vector2(0, _timePenaltyMoveAmount),
                _timePenaltyAnimationDuration)
            .BindToAnchoredPosition(penaltyTextInstance.rectTransform)
            .AddTo(this);

        // アルファ値のフェードアウトアニメーション
        LMotion.Create(initialColor, new Color(initialColor.r, initialColor.g, initialColor.b, 0f),
                _timePenaltyAnimationDuration)
            .BindToColor(penaltyTextInstance)
            .AddTo(this);

        // アニメーション後にオブジェクトを破棄
        Destroy(penaltyTextInstance.gameObject, _timePenaltyAnimationDuration);
    }
}
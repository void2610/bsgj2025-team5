using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions;
using System;
using Cysharp.Threading.Tasks;

public class TimerUI : MonoBehaviour
{
    [Header("UI要素")]
    [SerializeField] private TextMeshProUGUI mainTimerText;
    [SerializeField] private TextMeshProUGUI timePenaltyTextPrefab;
    [SerializeField] private Transform timePenaltyTextSpawnPoint;
    [SerializeField] private TextMeshProUGUI timeBonusTextPrefab;
    
    [Header("点滅設定")]
    [SerializeField] private float flashThresholdTime = 60.0f;
    
    private readonly Color _timerFlashColor = Color.red;
    private readonly Color _timeBonusColor = Color.green;
    private readonly float _timerFlashDuration = 0.5f;
    private readonly float _timeAnimationDuration = 1.0f;
    private readonly float _timeMoveAmount = 50f;
    
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
        
        GameManager.Instance.OnTimeChanged
            .Subscribe(v => UpdateTimer(v))
            .AddTo(this);
        
        GameManager.Instance.OnHappenTimePenalty
            .Subscribe(amount => ShowTimeChangeAnimation(amount, _timerFlashColor, "-"))
            .AddTo(this);
        
        GameManager.Instance.OnHappenTimeBonus
            .Subscribe(amount => ShowTimeChangeAnimation(amount, _timeBonusColor, "+"))
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

    private void StartTimerFlashAnimation()
    {
        if (_currentFlashMotionHandle.IsActive()) return;
        
        _currentFlashMotionHandle = LMotion.Create(_originalTimerTextColor, _timerFlashColor, _timerFlashDuration)
            .WithEase(Ease.OutSine)
            .WithLoops(-1, LoopType.Yoyo)
            .BindToColor(mainTimerText)
            .AddTo(this);
    }

    private void StopTimerFlashAnimation()
    {
        if (_currentFlashMotionHandle.IsActive())
        {
            _currentFlashMotionHandle.Cancel();
            mainTimerText.color = _originalTimerTextColor;
        }
    }

    private async void ShowTimeChangeAnimation(float amount, Color color, string prefix)
    {
        TextMeshProUGUI textPrefab = color == _timeBonusColor && timeBonusTextPrefab != null 
            ? timeBonusTextPrefab 
            : timePenaltyTextPrefab;
            
        if (textPrefab == null) return;
        
        TextMeshProUGUI textInstance = Instantiate(textPrefab,
            timePenaltyTextSpawnPoint.position,
            Quaternion.identity, timePenaltyTextSpawnPoint);
        textInstance.gameObject.SetActive(true);
        textInstance.text = $"{prefix}{Mathf.CeilToInt(amount)}";
        textInstance.color = color;
        
        var moveHandle = LMotion.Create(textInstance.rectTransform.anchoredPosition,
                textInstance.rectTransform.anchoredPosition + new Vector2(0, _timeMoveAmount),
                _timeAnimationDuration)
            .BindToAnchoredPosition(textInstance.rectTransform)
            .AddTo(textInstance.gameObject);
        
        var fadeHandle = LMotion.Create(color, new Color(color.r, color.g, color.b, 0f),
                _timeAnimationDuration)
            .BindToColor(textInstance)
            .AddTo(textInstance.gameObject);
        
        // 両方のアニメーションが完了するまで待機
        await UniTask.WhenAll(
            moveHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy()),
            fadeHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
        );
        
        // オブジェクトがまだ存在していれば破棄
        if (textInstance != null && textInstance.gameObject != null)
        {
            Destroy(textInstance.gameObject);
        }
    }
}
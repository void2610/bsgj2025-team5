using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions;
using System;

public class TimerUI : MonoBehaviour
{
    [Tooltip("必要なUIの要素")]
    // UI要素
    [SerializeField]
    private TextMeshProUGUI mainTimerText; // タイマー表示テキスト

    [SerializeField] private TextMeshProUGUI timePenaltyTextPrefab; // 時間ペナルティー表示用のプレハブ
    [SerializeField] private Transform timePenaltyTextSpawnPoint; // 時間ペナルティー表示を生成する位置

    [Tooltip("点滅アニメーションの設定")]
    // 点滅アニメーション設定
    private readonly Color _timerFlashColor = Color.red; // タイマーが点滅する色

    private readonly float _timerFlashDuration = 0.5f; // タイマー点滅の1サイクルにかかる時間
    [SerializeField] private readonly float flashThresholdTime = 60.0f; // タイマー点滅を開始する残り時間

    [Tooltip("時間ペナルティアニメーションの設定")]
    // 時間ペナルティーアニメーション設定
    private readonly float _timePenaltyAnimationDuration = 1.0f; // 時間アニメーションの期間

    private readonly float _timePenaltyMoveAmount = 50f; // 時間ペナルティアニメーションでテキストが移動する量 (上方向)

    // 内部状態管理用
    private MotionHandle _currentFlashMotionHandle; // 現在実行中のタイマー点滅モーションのハンドル
    private Color _originalTimerTextColor; // タイマーテキストの元の色

    private void Awake()
    {
        // タイマーの元の色を保持
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

    /// <summary>
    /// タイマーへの処理を登録する
    /// </summary>
    private void UpdateTimer(float v)
    {
        // タイマーの表示を更新
        UpdateTimerText(v);

        // 残り時間が閾値以下で0より大きい場合タイマー点滅を開始
        if (v <= flashThresholdTime && v > 0f)
        {
            StartTimerFlashAnimation();
        }
        else // 残り時間が0以下になった場合タイマー点滅を停止
        {
            StopTimerFlashAnimation();
        }
    }

    /// <summary>
    /// タイマーの表示を更新する
    /// </summary>
    /// <param name="currentTime">現在の残り時間 (秒)</param>
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
        // 既に点滅モーションがアクティブな場合は何もしない
        if (_currentFlashMotionHandle.IsActive()) return;

        // LitMotionを使ってテキストのcolorプロパティを赤色にして点滅させる
        _currentFlashMotionHandle = LMotion.Create(_originalTimerTextColor, _timerFlashColor, _timerFlashDuration)
            .WithEase(Ease.OutSine) // イージングを設定
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
        Debug.Log($"ShowTimePenaltyAnimation called with amount: {amount} at time: {Time.time}");
        if (timePenaltyTextPrefab == null)
        {
            Debug.LogWarning("Time Penalty Text Prefabが設定されていません。Inspectorで設定してください。", this);
            return;
        }

        // 減少表示テキストのインスタンスを生成し、ペナルティー量を整数に丸めて表示
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
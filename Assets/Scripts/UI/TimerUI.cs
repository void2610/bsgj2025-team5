using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions; // これが必要です
using System;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText; // 対象のTextMeshProを束縛

    [SerializeField] private Color flashColor = Color.red; // 点滅する色

    [SerializeField] private float flashDuration = 0.5f; // 点滅の期間

    [SerializeField] private float flashStartTime = 10.0f; // 点滅を開始する残り時間

    private MotionHandle _flashMotionHandle; // LitMotion のハンドル

    private Color _originalTextColor; // 元のテキスト色を保持するフィールド変数

    private void Awake()
    {
        // 元のテキスト色を保持
        _originalTextColor = timerText.color;
    }

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged
                .Subscribe(SetTimeDisplay)
                .AddTo(this);

            // 残り10sで赤文字点滅
            GameManager.Instance.OnTimeChanged
                .Where(time => time <= flashStartTime && time > 0f)
                .Subscribe(_ => StartFlash())
                .AddTo(this);
            // 点滅終了
            GameManager.Instance.OnTimeChanged
                .Where(time => time <= 0f)
                .Subscribe(_ => StopFlash())
                .AddTo(this);
        }
        else
        {
            Debug.LogError("GameManagerのインスタンスが見つかりません。シングルトンが正しく初期化されているか確認してください。", this);
        }
    }

    private void SetTimeDisplay(float currentTime)
    {
        int minutes = Mathf.Max(0, Mathf.FloorToInt(currentTime / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(currentTime % 60));
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    private void StartFlash()
    {
        // 既に点滅モーションが開始している場合は何もしない
        if (_flashMotionHandle.IsActive()) return;

        // LitMotionでTextMeshProUGUIのVertex Colorプロパティをアニメーションさせる
        _flashMotionHandle = LMotion.Create(_originalTextColor, flashColor, flashDuration)
            .WithEase(Ease.OutSine)
            .WithLoops(-1, LoopType.Yoyo) // アニメーションを無限ループさせる
            .BindToColor(timerText) // colorプロパティに束縛
            .AddTo(this);
    }

    private void StopFlash()
    {
        if (_flashMotionHandle.IsActive())
        {
            _flashMotionHandle.Cancel(); // 無限ループから解放する
            timerText.color = _originalTextColor; // テキストを元の色に戻す
        }
    }
}
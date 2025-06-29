using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions; // これが必要です
using System;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText; // 対象のTextMeshProを束縛

    [SerializeField] private TextMeshProUGUI timeDecreaseText; // 時間減少を知らせるテキストを束縛

    [SerializeField] private Transform timeDecreaseSpawnPoint; // 時間減少を知らせるテキストの生成位置

    [SerializeField] private Color flashColor = Color.red; // 点滅する色

    [SerializeField] private float flashDuration = 0.5f; // 点滅の期間

    [SerializeField] private float flashStartTime = 10.0f; // 点滅を開始する残り時間

    [SerializeField] private float timeDecreaseAnimationDuration = 1.0f; // 減少アニメーショの期間

    [SerializeField] private float timeDecreaseMoveAmount = 20f; // 減少アニメーションの移動量

    private MotionHandle _flashMotionHandle; // LitMotion のハンドル

    private Color _originalTextColor; // 元のテキスト色を保持するフィールド変数

    private float _previousTime; // 時間減少量のためのフィールド変数

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

            GameManager.Instance.OnTimeChanged
                .Subscribe(CheckTimeDecrease)
                .AddTo(this);

            // 残り時間によって赤文字点滅
            GameManager.Instance.OnTimeChanged
                .Where(time => time <= flashStartTime && time > 0f)
                .Subscribe(_ => StartFlash())
                .AddTo(this);
            // 点滅終了
            GameManager.Instance.OnTimeChanged
                .Where(time => time <= 0f)
                .Subscribe(_ => StopFlash())
                .AddTo(this);

            // 時間保持用変数の初期化
            _previousTime = GameManager.Instance.CurrentTimeValue;
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

    private void CheckTimeDecrease(float currentTime)
    {
        // 時間が減少したときのみに処理
        if (currentTime < _previousTime)
        {
            float decreaseAmount = _previousTime - currentTime;
            if (decreaseAmount > 1.0f) // 微小変化は無視
            {
                ShowTimeDecreaseAnimation(decreaseAmount);
            }
        }

        // 時間の更新
        _previousTime = currentTime;
    }

    private void ShowTimeDecreaseAnimation(float amount)
    {
        if (timeDecreaseText == null)
        {
            Debug.LogWarning("Time Decrease Prefabが設定されていません。", this);
            return;
        }

        // 減少表示テキストのインスタンスを生成
        TextMeshProUGUI decreaseTextInstance = Instantiate(timeDecreaseText, timeDecreaseSpawnPoint.position, Quaternion.identity, timeDecreaseSpawnPoint);
        decreaseTextInstance.gameObject.SetActive(true); // アクティブにする
        decreaseTextInstance.text = $"-{Mathf.CeilToInt(amount)}"; // 整数に丸めて表示

        // 初期色を赤に設定
        Color startColor = flashColor; // 点滅色を初期色として使用
        decreaseTextInstance.color = startColor;

        // 垂直方向への移動アニメーション
        LMotion.Create(decreaseTextInstance.rectTransform.anchoredPosition, decreaseTextInstance.rectTransform.anchoredPosition + new Vector2(0, timeDecreaseMoveAmount), timeDecreaseAnimationDuration)
            .BindToAnchoredPosition(decreaseTextInstance.rectTransform)
            .AddTo(this); // TimerUIオブジェクトが破棄されたときにモーションもキャンセルされるようにする

        // アルファ値のフェードアウトアニメーション
        LMotion.Create(startColor, new Color(startColor.r, startColor.g, startColor.b, 0f),
            timeDecreaseAnimationDuration)
          .BindToColor(decreaseTextInstance)
          .AddTo(this); // TimerUIオブジェクトが破棄されたときにモーションもキャンセルされるようにする

        // アニメーション後にオブジェクトを破棄
        Destroy(decreaseTextInstance.gameObject, timeDecreaseAnimationDuration);
    }
}
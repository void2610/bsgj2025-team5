using UnityEngine;
using TMPro;
using R3;
using LitMotion;
using LitMotion.Extensions;

public class TimerUI : MonoBehaviour
{
    [Tooltip("必要なUIの要素")]
    [SerializeField] private TextMeshProUGUI _mainTimerText; // タイマー表示テキスト
    [SerializeField] private TextMeshProUGUI _timePenaltyTextPrefab; // 時間ペナルティー表示用のプレハブ
    [SerializeField] private Transform _timePenaltyTextSpawnPoint; // 時間ペナルティー表示を生成する位置

    [Tooltip("点滅アニメーションの設定")]
    // 点滅アニメーション設定
    private readonly Color _timerFlashColor = Color.red; // タイマーが点滅する色
    private readonly float _timerFlashDuration = 0.5f; // タイマー点滅の1サイクルにかかる時間
    private readonly float _flashThresholdTime = 10.0f; // タイマー点滅を開始する残り時間

    [Tooltip("時間ペナルティアニメーションの設定")]
    // 時間ペナルティーアニメーション設定
    private readonly float _timePenaltyAnimationDuration = 1.0f; // 時間アニメーションの期間
    private readonly float _timePenaltyMoveAmount = 50f; // 時間ペナルティアニメーションでテキストが移動する量 (上方向)
    private readonly float _minPenaltyAmountToShow = 1.0f; // 表示する時間ペナルティー量の最小値

    // 内部状態管理用
    private MotionHandle _currentFlashMotionHandle; // 現在実行中のタイマー点滅モーションのハンドル
    private Color _originalTimerTextColor; // タイマーテキストの元の色
    private float _previousGameTime; // 前回のゲーム時間を保持し、時間ペナルティーを検出するために使用

    private void Awake()
    {
        // タイマーの元の色を保持
        _originalTimerTextColor = _mainTimerText.color;
    }

    private void Start()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManagerのインスタンスが見つかりません。シングルトンが正しく初期化されているか確認してください。", this);
            return;
        }

        // 初期ゲーム時間を設定（時間ペナルティー検出の基準値）
        _previousGameTime = GameManager.Instance.CurrentTimeValue;
        // タイマーにかかるアニメーションの登録
        TimerSubscribe();
    }

    /// <summary>
    /// タイマーへの処理を登録する
    /// </summary>
    private void TimerSubscribe()
    {
        // GameManagerのOnTimeChangedイベントを購読し、各種処理を登録
        Observable<float> timeChanged = GameManager.Instance.OnTimeChanged;

        // タイマーに残り時間を表示
        timeChanged
            .Subscribe(UpdateTimerText)
            .AddTo(this);

        // 時間ペナルティーの検出とアニメーション表示
        timeChanged
            .Subscribe(CheckForTimePenalty)
            .AddTo(this);

        // 残り時間が閾値以下で0より大きい場合タイマー点滅を開始
        timeChanged
            .Where(remainingTime => remainingTime <= _flashThresholdTime && remainingTime > 0f)
            .Subscribe(_ => StartTimerFlashAnimation()) 
            .AddTo(this);

        // 残り時間が0以下になった場合タイマー点滅を停止
        timeChanged
            .Where(remainingTime => remainingTime <= 0f) 
            .Subscribe(_ => StopTimerFlashAnimation())
            .AddTo(this);
    }

    /// <summary>
    /// タイマーの表示を更新する
    /// </summary>
    /// <param name="currentTime">現在の残り時間 (秒)</param>
    private void UpdateTimerText(float currentTime)
    {
        int minutes = Mathf.Max(0, Mathf.FloorToInt(currentTime / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(currentTime % 60));
        _mainTimerText.text = $"{minutes:00}:{seconds:00}";
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
            .BindToColor(_mainTimerText)
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
            _mainTimerText.color = _originalTimerTextColor;
        }
    }

    /// <summary>
    /// 時間ペナルティーをアニメーションで表示する
    /// </summary>
    private void CheckForTimePenalty(float currentTime)
    {
        // 時間が減少した場合にのみ処理を実行
        if (currentTime < _previousGameTime)
        {
            float timeDecreasedAmount = _previousGameTime - currentTime;

            // 定義された最小減少量より大きい場合にアニメーションを表示
            if (timeDecreasedAmount > _minPenaltyAmountToShow)
            {
                ShowTimePenaltyAnimation(timeDecreasedAmount);
            }
        }

        // 現在の時間を保持
        _previousGameTime = currentTime;
    }

    /// <summary>
    /// 時間ペナルティー量を示すテキストを生成し、アニメーション表示します。
    /// </summary>
    private void ShowTimePenaltyAnimation(float amount)
    {
        if (_timePenaltyTextPrefab == null)
        {
            Debug.LogWarning("Time Penalty Text Prefabが設定されていません。Inspectorで設定してください。", this);
            return;
        }

        // 減少表示テキストのインスタンスを生成し、ペナルティー量を整数に丸めて表示
        TextMeshProUGUI penaltyTextInstance = Instantiate(_timePenaltyTextPrefab, _timePenaltyTextSpawnPoint.position,
            Quaternion.identity, _timePenaltyTextSpawnPoint);
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
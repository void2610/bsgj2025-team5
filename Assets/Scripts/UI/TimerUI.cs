using UnityEngine;
using TMPro;

public class TimerUI : MonoBehaviour
{
    // UIのTextMeshProの束縛
    [SerializeField] private TextMeshProUGUI timerText;

    /// <summary>
    /// 
    /// </summary>
    private void Start()
    {
        // GameManagerのインスタンスが存在し、初期化されていることを確認
        if (GameManager.Instance != null)
        {
            // GameManagerのOnTimeChangedイベントにSetTimeDisplayメソッドを登録
            GameManager.Instance.OnTimeChanged += SetTimeDisplay;

            // 初期時間を表示させる
            SetTimeDisplay(GameManager.Instance.CurrentTimeValue);
        }
    }

    /// <summary>
    /// UIに残り時間をセットするメソッド
    /// </summary>
    private void SetTimeDisplay(float currentTime)
    {
        // 残り時間を分秒で計算する(かならず0以上にする)
        int minutes = Mathf.Max(0, Mathf.FloorToInt(currentTime / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(currentTime % 60));
        // 表示形式をM分S秒に揃える
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}
using UnityEngine;
using TMPro;
using R3;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;

    private void Start()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnTimeChanged
                .Subscribe(SetTimeDisplay)
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
}
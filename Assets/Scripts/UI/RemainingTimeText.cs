/// <summary>
/// クリアシーンで残り時間をTextMeshProに表示させるスクリプト
/// </summary>
/// 開発進捗
/// 06/12: 作成

using UnityEngine;
using TMPro; // TextMeshProUGUI を使う場合

public class RemainingTimeText : MonoBehaviour
{
    // UIのTextMeshProの束縛
    [SerializeField] private TextMeshProUGUI remainingTimeText;
    // PlayerPrefsに登録された残り時間のキー
    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";
    // 残り時間（デフォルトは0）
    private float _remainingTime = 0;

    private void Start()
    {
        // 残り時間の表示
        DisplayRemainingTime();
    }

    private void DisplayRemainingTime()
    {
        // PlayerPrefsから残り時間を読み込み（デフォルトは0f）
        if (PlayerPrefs.HasKey(REMAINING_TIME_AT_CLEAR))
        {
            _remainingTime = Mathf.Max(0f, PlayerPrefs.GetFloat(REMAINING_TIME_AT_CLEAR));
            // 残り時間を表示した後は、PlayerPrefsのデータをクリアする
            PlayerPrefs.DeleteKey(REMAINING_TIME_AT_CLEAR);
            PlayerPrefs.Save();
        }
        
        // 読み込んだ時間が0以上の場合のみ表示
        if (_remainingTime >= 0)
        {
            // "残りタイム: MM:SS" の形式で表示
            int minutes = Mathf.FloorToInt(_remainingTime / 60);
            int seconds = Mathf.FloorToInt(_remainingTime % 60);            
            remainingTimeText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}
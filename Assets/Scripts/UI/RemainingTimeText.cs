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
    public TextMeshProUGUI remainingTimeText;

    void Start()
    {
        // 残り時間の表示
        DisplayRemainingTime();
    }

    void DisplayRemainingTime()
    {
        // PlayerPrefsから残り時間を読み込み
        // キーが存在しない場合はデフォルト値として0fを返す
        float time = PlayerPrefs.GetFloat(PlayerPrefsKeys.RemainingTimeAtClear, 0f);
        
        // 読み込んだ時間が0より大きい場合のみ表示（デフォルト値でないことを確認）
        if (time >= 0)
        {
            // "残りタイム: MM:SS" の形式で表示
            int minutes = Mathf.FloorToInt(time / 60);
            int seconds = Mathf.FloorToInt(time % 60);            
            remainingTimeText.text = $"Time: {minutes:00}:{seconds:00}";
            
            // デバッグ用
            Debug.Log($"クリアシーンでPlayerPrefsから読み込んだ残りタイム: {time:F2}秒");
        }
        else
        {
            Debug.LogWarning("PlayerPrefsに残り時間のデータが見つからないか、値が0以下です。");
        }
        
        // 残り時間を表示した後は、PlayerPrefsのデータをクリアする
        PlayerPrefs.DeleteKey(PlayerPrefsKeys.RemainingTimeAtClear);
        PlayerPrefs.Save();
    }
}
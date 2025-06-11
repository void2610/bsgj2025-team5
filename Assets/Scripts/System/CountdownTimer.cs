/// <summary>
/// ゲームシーンのカウントダウンタイマーの処理をするスクリプト
/// </summary>
/// 開発進捗
/// 06/11:作成

using UnityEngine;
using TMPro;

public class CountdownTimer : MonoBehaviour
{
    // 初期カウント時間（180秒）
    [SerializeField] public float CountdownDuration = 180f;
    // UIのTextMeshProの内容物の束縛
    [SerializeField] public TextMeshProUGUI timerText;
    // 現在の残り時間
    private float currentTime;


    /// <summary>
    /// ゲーム開始時に、初期時間をタイマーに反映させる
    /// </summary>
    void Start()
    {
        currentTime = CountdownDuration;
        SetTimeDisplay();
    }

    /// <summary>
    /// 毎フレームタイマーを減らし、0になるとゲームオーバーとなる
    /// </summary>
    void Update()
    {
        // タイマーを数える
        currentTime -= Time.deltaTime;
        // 時間が0以下でゲームオーバー
        if (currentTime <= 0f)
        {
            // 残り時間を0で固定
            currentTime = 0f;
            // デバッグ用
            Debug.Log("タイマーが0になりました!");
            // ゲームオーバーにしてシーンを読み込む
            GameManager.Instance.GameOver();
        }
        else
        {
            SetTimeDisplay();
        }
    }

    /// <summary>
    /// UIに残り時間をセットするメソッド
    /// </summary>
    public void SetTimeDisplay()
    {
        // 残り時間を分秒で計算する
        int minutes = Mathf.FloorToInt(currentTime / 60);
        int seconds = Mathf.FloorToInt(currentTime % 60);
        // 表示形式をM分S秒に揃える
        timerText.text = $"{minutes:00}:{seconds:00}";
    }
}

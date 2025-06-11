/// <summary>
/// ゲームシーンのカウントダウンタイマーの処理をするスクリプト
/// </summary>
/// 開発進捗
/// 06/11:作成

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // シーン管理のために必要

public class CountdownTimer : MonoBehaviour
{
    // 初期カウント時間（180秒）
    [SerializeField] public float CountdownDuration = 180f;
    // UIのTextMeshProの束縛
    [SerializeField] public TextMeshProUGUI timerText;
    // 現在の残り時間
    private float currentTime;
    // ゲーム終了（ゲームオーバーまたはクリア）を判定するフラグ
    private bool gameEnded = false; 

    /// <summary>
    /// ゲーム開始時に、初期時間をタイマーに反映させる
    /// </summary>
    void Start()
    {
        currentTime = CountdownDuration;
        SetTimeDisplay();
        gameEnded = false;
    }

    /// <summary>
    /// 毎フレームタイマーを減らし、0になるとゲームオーバーとなる
    /// </summary>
    void Update()
    {
        // ゲームが終了していたら更新しない
        if (gameEnded) return; 

        // タイマーを数える
        currentTime -= Time.deltaTime;

        // 時間が0秒以下でゲームオーバー
        if (currentTime <= 0f)
        {
            // 0秒で固定
            currentTime = 0f;
            SetTimeDisplay();

            Debug.Log("タイマーが0秒になりました");

            gameEnded = true; // ゲームを終了状態にする
            GameManager.Instance.GameOver(); // GameManagerを介してゲームオーバーシーンへ遷移

            this.enabled = false; // このスクリプトの更新を停止
        }
        else
        {
            // タイマーを更新
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

    /// <summary>
    /// クリア時の残り時間を保存するメソッド
    /// </summary>
    public void SaveCurrentTime()
    {
        // currentTime = 0f;
        currentTime = 0f;
        SetTimeDisplay();

        // PlayerPrefsに残り時間を保存
        PlayerPrefs.SetFloat(PlayerPrefsKeys.RemainingTimeAtClear, currentTime);
        PlayerPrefs.Save();

        Debug.Log($"タイマーが0秒以下になりました。クリア時の残り時間としてPlayerPrefsに保存しました: {currentTime:F2}秒");

        // ゲームを終了状態にする
        gameEnded = true; 
    }
}
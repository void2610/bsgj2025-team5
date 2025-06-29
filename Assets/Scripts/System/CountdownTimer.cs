/// <summary>
/// ゲームシーンのカウントダウンタイマーの処理をするスクリプト
/// </summary>
/// 開発進捗
/// 06/11:作成
/// 06/16:

using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // シーン管理のために必要

public class CountdownTimer : MonoBehaviour
{
    // 初期カウント時間（180秒）
    [SerializeField] private float countDownDuration = 180f;
    // UIのTextMeshProの束縛
    [SerializeField] private TextMeshProUGUI timerText;
    // 現在の残り時間
    private float _currentTime;
    // ゲーム終了（ゲームオーバーまたはクリア）を判定するフラグ
    private bool _wasGameEnded = false;
    // PlayerPrefsに登録する残り時間のキー
    private const string REMAINING_TIME_AT_CLEAR = "RemainingTimeAtClear";

    /// <summary>
    /// ゲーム開始時に、初期時間をタイマーに反映させる
    /// </summary>
    private void Start()
    {
        _currentTime = countDownDuration;
        SetTimeDisplay();
        _wasGameEnded = false;
    }

    /// <summary>
    /// 毎フレームタイマーを減らし、0になるとゲームオーバーとなる
    /// </summary>
    private void Update()
    {
        // ゲームが終了していたら更新しない
        if (_wasGameEnded) return; 

        // 残り時間を数える
        OperateCurrentTime(-Time.deltaTime);
        // タイマーを更新
        SetTimeDisplay();
        // 時間が0秒以下でゲームオーバー
        if (_currentTime <= 0f)
        {
            _currentTime = 0f; //0秒で固定

            Debug.Log("タイマーが0秒になりました");

            _wasGameEnded = true;
            GameManager.Instance.GameOver(); // GameManagerを介してゲームオーバーシーンへ遷移

            this.enabled = false; // このスクリプトの更新を停止
        }

    }

    /// <summary>
    /// UIに残り時間をセットするメソッド
    /// </summary>
    private void SetTimeDisplay()
    {
        // 残り時間を分秒で計算する(かならず0以上にする)
        int minutes = Mathf.Max(0, Mathf.FloorToInt(_currentTime / 60));
        int seconds = Mathf.Max(0, Mathf.FloorToInt(_currentTime % 60));
        // 表示形式をM分S秒に揃える
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>
    /// 残り時間を変更するメソッド
    /// </summary>
    public void OperateCurrentTime(float v)
    {
        _currentTime += v;
    }

    /// <summary>
    /// クリア時の残り時間を保存するメソッド
    /// </summary>
    public void SaveCurrentTime()
    {
        SetTimeDisplay();

        // PlayerPrefsに残り時間を保存
        PlayerPrefs.SetFloat(REMAINING_TIME_AT_CLEAR, _currentTime);
        PlayerPrefs.Save();

        Debug.Log($"クリア時の残り時間としてPlayerPrefsに保存しました: {_currentTime:F2}秒");

        _wasGameEnded = true; 
    }
}
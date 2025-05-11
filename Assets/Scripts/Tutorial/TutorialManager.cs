/// <summary>
/// ゲーム上のチュートリアルを管理するマネージャクラス
/// 
/// 実装進捗
/// 五月十一日：作成、LitMotion未実装
/// 
/// 
/// 
/// </summary> 

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

using LitMotion;
using LitMotion.Extensions;



public class TutorialManager : MonoBehaviour
{
    // チュートリアル用UI
    protected RectTransform tutorialTextArea;
    protected Text TutorialTitle;
    protected Text TutorialText;
 
    // チュートリアルタスク
    protected ITutorialTask currentTask;
    protected List<ITutorialTask> tutorialTask;
 
    // チュートリアル表示フラグ
    private bool isEnabled;
 
    // チュートリアルタスクの条件を満たした際の遷移用フラグ
    private bool task_executed = false;
 
    // チュートリアル表示時のUI移動距離
    private float fade_pos_x = 350;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // チュートリアル表示用UIのインスタンス取得
        tutorialTextArea = GameObject.Find("TutorialTextArea").GetComponent<RectTransform>();
        TutorialTitle = tutorialTextArea.Find("Title").GetComponent<Text>();
        TutorialText = tutorialTextArea.Find("Text").GetComponentInChildren<Text>();
 
        // チュートリアルの一覧
        tutorialTask = new List<ITutorialTask>()
        {
            // チュートリアルが増えたらここに記載していく
            new MovementTask(),
        };
 
        // 最初のチュートリアルを設定
        StartCoroutine(SetCurrentTask(tutorialTask.First()));
 
        isEnabled = true;
    }

    // Update is called once per frame
    void Update()
    {
        // チュートリアルが存在し実行されていない場合に処理
        if (currentTask != null && !task_executed) {
            // 現在のチュートリアルが実行されたか判定
            if (currentTask.CheckTask()) {
                task_executed = true;

                /// <sammary>
                /// 最優先の実装
                /// </sammary>
                // これをDOTweenを使わずに実装する

                // DOVirtual.DelayedCall(currentTask.GetTransitionTime(), () => {
                //     iTween.MoveTo(tutorialTextArea.gameObject, iTween.Hash(
                //         "position", tutorialTextArea.transform.position + new Vector3(fade_pos_x, 0, 0),
                //         "time", 1f
                //     ));
 
                //     tutorialTask.RemoveAt(0);
 
                //     var nextTask = tutorialTask.FirstOrDefault();
                //     if (nextTask != null) {
                //         StartCoroutine(SetCurrentTask(nextTask, 1f));
                //     }
                // });
            }
        }
 
        // ヘルプボタンが押されたかどうかを判定、ここかGomeManegerで感知

        // if (Input.GetButtonDown("Help")) {
        //     SwitchEnabled();
        // }
    }

    /// <summary>
    /// 新しいチュートリアルタスクを設定する
    /// 優先すべき実装
    /// </summary>
    /// <param name="task"></param>
    /// <param name="time"></param>
    /// <returns></returns>
    protected IEnumerator SetCurrentTask(ITutorialTask task, float time = 0)
    {
        // timeが指定されている場合は待機
        yield return new WaitForSeconds(time);
 
        currentTask = task;
        task_executed = false;
 
        // UIにタイトルと説明文を設定
        TutorialTitle.text = task.GetTitle();
        TutorialText.text = task.GetText();
 
        // チュートリアルタスク設定時用の関数を実行
        task.OnTaskSetting();

        // LitMotionを使ってチュートリアルUIを画面外から遷移させる
        //
        //
    }

    /// <summary>
    /// チュートリアルの有効・無効の切り替え
    /// 実装検討中
    /// </summary>
    // protected void SwitchEnabled()
    // {
    //     isEnabled = !isEnabled;
 
    //     // UIの表示切り替え
    //     float alpha = isEnabled ? 1f : 0;
    //     tutorialTextArea.GetComponent<CanvasGroup>().alpha = alpha;
    // }
}

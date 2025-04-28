using System.Threading;
using UnityEngine;
using R3;
using R3.Triggers;
using TMPro;

// R3を使うには以下の名前空間をusingする
using Cysharp.Threading.Tasks;


public class UniTaskTutorialUI : MonoBehaviour
{
    [SerializeField] private TMP_Text text;
    
    private string _message = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    
    // 1文字ずつ表示する
    // キャンセルされたらスキップして全部表示する
    private async UniTask PrintAsync(string message, TMP_Text text, CancellationToken cancellationToken = default)
    {
        text.text = "";
        for (var i = 0; i < message.Length; i++)
        {
            // キャンセルされたらスキップして全部表示する
            if (cancellationToken.IsCancellationRequested)
            {
                text.text = message;
                return;
            }
            
            // 1文字ずつ表示する
            text.text += message[i];
            await UniTask.Delay(100);
            // 下のように書くと、キャンセルされた時点で関数の処理が終了する
            // await UniTask.Delay(100, cancellationToken: cancellationToken);
        }
    }
    
    private async UniTask Start()
    {
        // cancellationTokenを作成する
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;
        
        // スペースキーを押すとキャンセルする処理
        // R3のトリガー機能を使って、Update関数と同じようなことができる(多分あんまり使わない)
        this.UpdateAsObservable()
            .Where(_ => Input.GetKeyDown(KeyCode.Space))
            .Subscribe(_ =>
            {
                cancellationTokenSource.Cancel();
            })
            .AddTo(this);
        
        // 非同期処理を開始する
        await PrintAsync(_message, text, cancellationToken);
        // テキスト表示処理が終わったらログ
        Debug.Log("終了");
    }
}

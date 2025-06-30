using LitMotion;
using LitMotion.Extensions;
using UnityEngine;

public class YoyoTween : MonoBehaviour
{
    [Tooltip("往復運動の周期（秒）。往復1回にかかる時間")]
    [SerializeField] private float period = 2f;
    
    [Tooltip("移動方向と距離。この分だけ初期位置から移動します")]
    [SerializeField] private Vector3 moveVector = new (1, 0, 0);
    
    [Tooltip("ループタイプ。Yoyoで往復、Restartで一方向の繰り返し")]
    [SerializeField] private LoopType loopType = LoopType.Yoyo;
    
    [Tooltip("イージング関数。動きの加減速を制御します")]
    [SerializeField] private Ease ease = Ease.InOutSine;
    
    [Tooltip("開始時にランダムな遅延を入れるか")]
    [SerializeField] private bool useRandomDelay = false;
    
    [Tooltip("ランダム遅延の最大値（秒）")]
    [SerializeField] private float randomDelayMax = 1f;
    
    private Vector3 _initialLocalPosition;
    private Vector3 _offset;

    private void Start()
    {
        // 初期ローカル位置を保存
        _initialLocalPosition = transform.localPosition;
        
        // 遅延時間を計算
        var delay = useRandomDelay ? Random.Range(0f, randomDelayMax) : 0f;
        
        // オフセット値をアニメーション（0から1の値で制御）
        LMotion.Create(0f, 1f, period / 2f)
            .WithDelay(delay)
            .WithLoops(-1, loopType)
            .WithEase(ease)
            .Bind(value => 
            {
                // 現在のオフセットを計算
                _offset = moveVector * value;
                // 他のYoyoTweenのオフセットと合成して適用
                UpdatePosition();
            })
            .AddTo(gameObject);
    }

    private void UpdatePosition()
    {
        // 全てのYoyoTweenコンポーネントを取得
        var yoyoTweens = GetComponents<YoyoTween>();
        var totalOffset = Vector3.zero;
        
        // 各YoyoTweenのオフセットを合計
        foreach (var tween in yoyoTweens)
        {
            totalOffset += tween._offset;
        }
        
        // 初期位置に合計オフセットを適用
        transform.localPosition = _initialLocalPosition + totalOffset;
    }
}
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

    private Vector3 _initialPosition;

    private void Start()
    {
        // 初期位置を保存
        _initialPosition = transform.position;
        
        // 終了位置を計算
        var endPosition = _initialPosition + moveVector;
        
        // 遅延時間を計算
        var delay = useRandomDelay ? Random.Range(0f, randomDelayMax) : 0f;
        
        // 往復運動を開始
        LMotion.Create(_initialPosition, endPosition, period / 2f)
            .WithDelay(delay) // 遅延を適用
            .WithLoops(-1, loopType) // 指定されたループタイプを使用
            .WithEase(ease) // 指定されたイージングを使用
            .BindToPosition(transform) // 位置に紐づけ
            .AddTo(gameObject);
    }
}
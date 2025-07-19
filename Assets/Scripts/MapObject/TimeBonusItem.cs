using UnityEngine;
using LitMotion;
using Cysharp.Threading.Tasks;

public class TimeBonusItem : MonoBehaviour
{
    [Header("アイテム設定")]
    [Tooltip("ステージに直接置かれるかどうか")]
    [SerializeField] private bool isPlacedOnStage = true;
    
    [Tooltip("取得時に増加する時間（秒）")]
    [SerializeField] private float timeBonus = 10f;
    
    [Tooltip("取得時のパーティクルエフェクト（オプション）")]
    [SerializeField] private GameObject particlePrefab;
    
    [Header("アニメーション設定")]
    [Tooltip("アイテムの回転速度")]
    [SerializeField] private float rotationSpeed = 90f;
    
    [Tooltip("アイテムの上下運動の高さ")]
    [SerializeField] private float floatHeight = 0.3f;
    
    [Tooltip("アイテムの上下運動の速度")]
    [SerializeField] private float floatSpeed = 2f;
    
    private Vector3 _startPosition;
    private MotionHandle _rotationHandle;
    private MotionHandle _floatHandle;
    private bool _isInitialized;
    
    // BreakableObjectから呼び出される初期化メソッド
    public void Initialize()
    {
        _startPosition = this.transform.position;
        _isInitialized = true;
        StartAnimations();
        
        // プレイヤーが既に範囲内にいるかチェック
        var myCollider = GetComponent<Collider>();
        if (myCollider && myCollider.isTrigger)
        {
            var bounds = myCollider.bounds;
            var results = new Collider[10];
            var count = Physics.OverlapBoxNonAlloc(bounds.center, bounds.extents, results, transform.rotation);
            for (int i = 0; i < count; i++)
            {
                if (results[i].TryGetComponent<Player>(out _))
                {
                    HandleTrigger(results[i]);
                    break;
                }
            }
        }
    }
    
    private void StartAnimations()
    {
        // 回転アニメーション（無限ループ）
        _rotationHandle = LMotion.Create(0f, 360f, 360f / rotationSpeed)
            .WithEase(Ease.Linear)
            .WithLoops(-1)
            .Bind(rotationY => {
                if (this && transform)
                {
                    transform.rotation = Quaternion.Euler(0, rotationY, 0);
                }
            })
            .AddTo(this);
        
        // 上下浮遊アニメーション（無限ループ）
        _floatHandle = LMotion.Create(_startPosition.y, _startPosition.y + floatHeight, 1f / floatSpeed)
            .WithEase(Ease.InOutSine)
            .WithLoops(-1, LoopType.Yoyo)
            .Bind(positionY => {
                if (this && transform)
                {
                    var pos = transform.position;
                    pos.y = positionY;
                    transform.position = pos;
                }
            })
            .AddTo(this);
    }
    
    private async void HandleTrigger(Collider other)
    {
        if (other.TryGetComponent<Player>(out var player))
        {
            // アニメーションを停止
            StopAnimations();
            
            // コライダーを無効化して重複取得を防ぐ
            GetComponent<Collider>().enabled = false;
            
            // プレイヤーに向かって移動するアニメーション
            var moveHandle = LMotion.Create(transform.position, player.transform.position, 0.3f)
                .WithEase(Ease.InQuad)
                .Bind(position => 
                {
                    if (this && transform)
                    {
                        transform.position = position;
                    }
                })
                .AddTo(this);
            
            // 同時にスケールを小さくするアニメーション
            var scaleHandle = LMotion.Create(transform.localScale, Vector3.zero, 0.3f)
                .WithEase(Ease.InQuad)
                .Bind(scale =>
                {
                    if (this && transform)
                    {
                        transform.localScale = scale;
                    }
                })
                .AddTo(this);
            
            // 両方のアニメーションが完了するまで待機
            await UniTask.WhenAll(
                moveHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy()),
                scaleHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy())
            );
            
            // アニメーション完了後の処理
            if (this != null && gameObject != null)
            {
                // 時間ボーナスを追加
                GameManager.Instance.IncreaseTime(timeBonus);
                
                // パーティクルエフェクトを生成
                if (particlePrefab)
                {
                    Instantiate(particlePrefab, transform.position, Quaternion.identity);
                }
                
                // オブジェクトを破棄
                Destroy(gameObject);
            }
        }
    }

    private void Awake()
    {
        if (isPlacedOnStage) Initialize();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!_isInitialized) return;
        HandleTrigger(other);
    }
    
    /// <summary>
    /// アニメーションを停止する
    /// </summary>
    private void StopAnimations()
    {
        if (_rotationHandle.IsPlaying()) _rotationHandle.Cancel();
        if (_floatHandle.IsPlaying()) _floatHandle.Cancel();
    }
    
    /// <summary>
    /// オブジェクト破棄時にアニメーションを確実に停止
    /// </summary>
    private void OnDestroy()
    {
        StopAnimations();
    }
}
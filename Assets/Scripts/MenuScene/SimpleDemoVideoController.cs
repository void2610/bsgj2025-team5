using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using System.Threading;

[RequireComponent(typeof(RawImage))]
public class SimpleDemoVideoController : MonoBehaviour
{
    [Tooltip("デモ動画が開始するまでの待機時間（秒）")]
    [SerializeField] private float idleTimeToStartDemo = 30f;
    [Tooltip("フェードイン/アウトの時間（秒）")]
    [SerializeField] private float fadeDuration = 1f;
    
    private RawImage _demoVideoRawImage;
    private float _idleTimer;
    private bool _isDemoPlaying;
    private CancellationTokenSource _demoCancellationTokenSource;
    private MotionHandle _fadeHandle;
    private Vector3 _lastMousePosition;

    private bool DetectAnyInput()
    {
        // マウス移動の検知
        var currentMousePosition = Input.mousePosition;
        if (Vector3.Distance(currentMousePosition, _lastMousePosition) > 0.1f)
        {
            _lastMousePosition = currentMousePosition;
            return true;
        }
        
        // マウスクリックの検知
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2)) return true;
        
        if (Input.anyKeyDown) return true;
        if (Input.touchCount > 0) return true;
        
        return false;
    }
    
    private void ResetIdleTimer()
    {
        _idleTimer = 0f;
    }
    
    private async UniTaskVoid StartDemoAsync()
    {
        if (_isDemoPlaying || !_demoVideoRawImage) return;
        
        _isDemoPlaying = true;
        _demoCancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // RawImageを表示
            _demoVideoRawImage.enabled = true;
            
            // フェードイン
            _fadeHandle = LMotion.Create(0f, 1f, fadeDuration)
                .WithEase(Ease.InOutSine)
                .BindToColorA(_demoVideoRawImage)
                .AddTo(this);
            
            await _fadeHandle.ToUniTask(cancellationToken: _demoCancellationTokenSource.Token);
        }
        catch (System.OperationCanceledException) { }
    }
    
    
    private void StopDemo()
    {
        if (!_isDemoPlaying) return;
        
        _isDemoPlaying = false;
        
        // キャンセル
        _demoCancellationTokenSource?.Cancel();
        _demoCancellationTokenSource?.Dispose();
        
        // フェードハンドルをキャンセル
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        
        // フェードアウトして非表示
        FadeOutAndHideAsync().Forget();
        
        // タイマーリセット
        ResetIdleTimer();
    }
    
    private async UniTaskVoid FadeOutAndHideAsync()
    {
        if (!_demoVideoRawImage) return;
        
        // フェードアウト
        var fadeOutHandle = LMotion.Create(_demoVideoRawImage.color.a, 0f, fadeDuration * 0.5f)
            .WithEase(Ease.InOutSine)
            .BindToColorA(_demoVideoRawImage)
            .AddTo(this);
        
        await fadeOutHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        
        _demoVideoRawImage.enabled = false;
    }
    
    private void Awake()
    {
        _demoVideoRawImage = this.GetComponent<RawImage>();
    }
    
    private void Start()
    {
        // 初期状態では透明にするが、GameObjectは有効のまま
        if (_demoVideoRawImage)
        {
            var color = _demoVideoRawImage.color;
            color.a = 0f;
            _demoVideoRawImage.color = color;
            _demoVideoRawImage.enabled = false;  // RawImageコンポーネントのみ無効化
        }
        
        _idleTimer = 0f;
        _lastMousePosition = Input.mousePosition;
    }
    
    private void Update()
    {
        // 入力検知
        if (DetectAnyInput())
        {
            if (_isDemoPlaying) StopDemo();
            else ResetIdleTimer();
        }
        else if (!_isDemoPlaying)
        {
            // アイドル時間をカウント
            _idleTimer += Time.deltaTime;
            // アイドル時間が設定値を超えたらデモ開始
            if (_idleTimer >= idleTimeToStartDemo)
            {
                StartDemoAsync().Forget();
            }
        }
    }
    
    private void OnDestroy()
    {
        // クリーンアップ
        _demoCancellationTokenSource?.Cancel();
        _demoCancellationTokenSource?.Dispose();
        
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
    }
}
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using LitMotion;
using LitMotion.Extensions;
using System.Threading;

public class DemoVideoController : MonoBehaviour
{
    [Header("デモ動画設定")]
    [Tooltip("デモ動画を表示するRawImage")]
    [SerializeField] private RawImage demoVideoRawImage;
    
    [Tooltip("デモ動画を再生するVideoPlayer")]
    [SerializeField] private VideoPlayer videoPlayer;
    
    [Tooltip("デモ動画が開始するまでの待機時間（秒）")]
    [SerializeField] private float idleTimeToStartDemo = 30f;
    
    [Tooltip("フェードイン/アウトの時間（秒）")]
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("入力設定")]
    [Tooltip("入力を検知するためのInputActionAsset")]
    [SerializeField] private InputActionAsset inputActions;
    
    private float _idleTimer;
    private bool _isDemoPlaying;
    private CancellationTokenSource _demoCancellationTokenSource;
    private MotionHandle _fadeHandle;
    
    private void Start()
    {
        // 初期状態では非表示
        if (demoVideoRawImage != null)
        {
            var color = demoVideoRawImage.color;
            color.a = 0f;
            demoVideoRawImage.color = color;
            demoVideoRawImage.gameObject.SetActive(false);
        }
        
        // 入力アクションを有効化
        if (inputActions != null)
        {
            inputActions.Enable();
            
            // すべての入力アクションに対してコールバックを設定
            foreach (var actionMap in inputActions.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    action.performed += OnAnyInput;
                    action.started += OnAnyInput;
                }
            }
        }
        
        _idleTimer = 0f;
    }
    
    private void Update()
    {
        if (!_isDemoPlaying)
        {
            // マウス移動の検知
            if (Mouse.current != null && Mouse.current.delta.ReadValue().magnitude > 0.1f)
            {
                ResetIdleTimer();
            }
            
            // アイドル時間をカウント
            _idleTimer += Time.deltaTime;
            
            // アイドル時間が設定値を超えたらデモ開始
            if (_idleTimer >= idleTimeToStartDemo)
            {
                StartDemoAsync().Forget();
            }
        }
    }
    
    private void OnAnyInput(InputAction.CallbackContext context)
    {
        // デモ再生中なら停止
        if (_isDemoPlaying)
        {
            StopDemo();
        }
        else
        {
            ResetIdleTimer();
        }
    }
    
    private void ResetIdleTimer()
    {
        _idleTimer = 0f;
    }
    
    private async UniTaskVoid StartDemoAsync()
    {
        if (_isDemoPlaying || demoVideoRawImage == null || videoPlayer == null) return;
        
        _isDemoPlaying = true;
        _demoCancellationTokenSource = new CancellationTokenSource();
        
        try
        {
            // RawImageを表示
            demoVideoRawImage.gameObject.SetActive(true);
            
            // ビデオを準備して再生開始
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.Prepare();
            
            // フェードイン
            _fadeHandle = LMotion.Create(0f, 1f, fadeDuration)
                .WithEase(Ease.InOutSine)
                .BindToColorA(demoVideoRawImage)
                .AddTo(this);
            
            await _fadeHandle.ToUniTask(cancellationToken: _demoCancellationTokenSource.Token);
            
            // ビデオループ再生
            videoPlayer.isLooping = true;
        }
        catch (System.OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }
    
    private void OnVideoPrepared(VideoPlayer source)
    {
        source.prepareCompleted -= OnVideoPrepared;
        source.Play();
    }
    
    private void StopDemo()
    {
        if (!_isDemoPlaying) return;
        
        _isDemoPlaying = false;
        
        // キャンセル
        _demoCancellationTokenSource?.Cancel();
        _demoCancellationTokenSource?.Dispose();
        
        // フェードハンドルをキャンセル
        if (_fadeHandle.IsActive())
        {
            _fadeHandle.Cancel();
        }
        
        // フェードアウトして非表示
        FadeOutAndHideAsync().Forget();
        
        // タイマーリセット
        ResetIdleTimer();
    }
    
    private async UniTaskVoid FadeOutAndHideAsync()
    {
        if (demoVideoRawImage == null) return;
        
        // フェードアウト
        var fadeOutHandle = LMotion.Create(demoVideoRawImage.color.a, 0f, fadeDuration * 0.5f)
            .WithEase(Ease.InOutSine)
            .BindToColorA(demoVideoRawImage)
            .AddTo(this);
        
        await fadeOutHandle.ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
        
        // ビデオ停止と非表示
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }
        
        demoVideoRawImage.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        // クリーンアップ
        _demoCancellationTokenSource?.Cancel();
        _demoCancellationTokenSource?.Dispose();
        
        if (_fadeHandle.IsActive())
        {
            _fadeHandle.Cancel();
        }
        
        // 入力アクションのコールバックを解除
        if (inputActions != null)
        {
            foreach (var actionMap in inputActions.actionMaps)
            {
                foreach (var action in actionMap.actions)
                {
                    action.performed -= OnAnyInput;
                    action.started -= OnAnyInput;
                }
            }
        }
    }
}
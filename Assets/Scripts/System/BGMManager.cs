using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LitMotion;
using R3;

/// <summary>
/// プレイヤーの速度に応じてBGMのテンポを変更するクラス
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : SingletonMonoBehaviour<BGMManager>
{
    [Header("BGM設定")]
    [Tooltip("曲1: サイケデリックレベル0-2で使用")]
    [SerializeField] private AudioClip bgmClip1;
    
    [Tooltip("曲2: サイケデリックレベル3-4で使用")]
    [SerializeField] private AudioClip bgmClip2;
    
    [Tooltip("ピッチ変更時のフェード時間（秒）。大きいほど滑らかに変化します")]
    [SerializeField] private float fadeTime = 0.5f;
    
    [Tooltip("BGMのテンポ更新をチェックする間隔（秒）")]
    [SerializeField] private float bgmUpdateInterval = 1f;
    
    [Header("音量設定")]
    [Tooltip("曲1の音量倍率")]
    [SerializeField] private float bgm1VolumeMultiplier = 1.0f;
    
    [Tooltip("曲2の音量倍率")]
    [SerializeField] private float bgm2VolumeMultiplier = 1.0f;
    
    [Header("ピッチ設定")]
    [Tooltip("各スピードレベル（0-4）でのピッチ倍率")]
    [SerializeField] private List<float> pitchMultipliers = new() { 1.0f, 1.5f, 2.0f, 1.0f, 1.5f };
    
    [Header("フェード設定")]
    [Tooltip("曲切り替え時のフェード時間（秒）")]
    [SerializeField] private float crossFadeTime = 1.0f;
    
    private int _currentSpeedLevel = 0;
    private AudioSource _audioSource;
    private AudioSource _audioSource2;
    private MotionHandle _fadeHandle;
    private bool _isUsingSource1 = true;
    
    public void FadeOutBGM(float volume, float duration = 1.0f)
    {
        var currentSource = _isUsingSource1 ? _audioSource : _audioSource2;
        
        if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
        
        _fadeHandle = LMotion.Create(currentSource.volume, volume, duration)
            .WithEase(Ease.InOutQuad)
            .Bind(v => currentSource.volume = v)
            .AddTo(this);
    }

    private async UniTask ChangeBGMAsync(int newSpeedLevel)
    {
        if (newSpeedLevel is < 0 or > 4) return;
        if (_currentSpeedLevel == newSpeedLevel) return;

        _currentSpeedLevel = newSpeedLevel;

        // 使用する曲とピッチを決定
        AudioClip targetClip;
        float targetPitch;
        float targetVolume;
        
        if (newSpeedLevel <= 2)
        {
            targetClip = bgmClip1;
            targetVolume = bgm1VolumeMultiplier;
        }
        else
        {
            targetClip = bgmClip2;
            targetVolume = bgm2VolumeMultiplier;
        }
        
        targetPitch = pitchMultipliers[newSpeedLevel];

        var currentSource = _isUsingSource1 ? _audioSource : _audioSource2;
        var nextSource = _isUsingSource1 ? _audioSource2 : _audioSource;
        
        // 曲が変わる場合のみクロスフェード
        if (currentSource.clip != targetClip)
        {
            nextSource.clip = targetClip;
            nextSource.pitch = targetPitch;
            nextSource.volume = 0;
            nextSource.Play();
            
            // クロスフェード
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            
            _fadeHandle = LMotion.Create(0f, 1f, crossFadeTime)
                .WithEase(Ease.InOutQuad)
                .Bind(progress =>
                {
                    var currentVolume = currentSource.clip == bgmClip1 ? bgm1VolumeMultiplier : bgm2VolumeMultiplier;
                    currentSource.volume = currentVolume * (1f - progress);
                    nextSource.volume = targetVolume * progress;
                })
                .AddTo(this);
                
            await _fadeHandle.ToUniTask();
            
            currentSource.Stop();
            _isUsingSource1 = !_isUsingSource1;
        }
        else
        {
            // 同じ曲でピッチのみ変更
            if (_fadeHandle.IsActive()) _fadeHandle.Cancel();
            
            _fadeHandle = LMotion.Create(currentSource.pitch, targetPitch, fadeTime)
                .WithEase(Ease.InOutQuad)
                .Bind(v => currentSource.pitch = v)
                .AddTo(this);
                
            await _fadeHandle.ToUniTask();
        }
    }

    protected override void Awake()
    {
        base.Awake();
        // 2つのAudioSourceをセットアップ
        _audioSource = GetComponent<AudioSource>();
        _audioSource2 = gameObject.AddComponent<AudioSource>();
        
        _audioSource.loop = true;
        _audioSource2.loop = true;
        
        // 同じミキサーグループを設定
        _audioSource2.outputAudioMixerGroup = _audioSource.outputAudioMixerGroup;

        if (!bgmClip1) throw new ArgumentNullException(nameof(bgmClip1));
        if (!bgmClip2) throw new ArgumentNullException(nameof(bgmClip2));

        // 初期状態で曲1を再生
        _audioSource.clip = bgmClip1;
        _audioSource.pitch = pitchMultipliers[0];
        _audioSource.volume = bgm1VolumeMultiplier;
        _audioSource.Play();
        
        _audioSource2.volume = 0f;
    }

    private void Start()
    {
        // タイマーで指定した間隔ごとにBGMのテンポを更新
        UniTaskAsyncEnumerable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(bgmUpdateInterval))
            .ForEachAsync(_ =>
            {
                var nextSpeedLevel = GameManager.Instance.Player.PlayerItemCountInt.CurrentValue;
                ChangeBGMAsync(nextSpeedLevel).Forget();
            }, this.GetCancellationTokenOnDestroy()).Forget();
    }
}
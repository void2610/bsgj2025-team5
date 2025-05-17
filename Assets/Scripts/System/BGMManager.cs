using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
using Cysharp.Threading.Tasks.Linq;
using LitMotion;
using R3;
using UnityEngine.Serialization;

/// <summary>
/// プレイヤーの速度に応じてBGMを変更するクラス
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> bgmClips;
    [SerializeField] private float fadeTime = 0.5f; // フェード時間
    [SerializeField] private float bgmUpdateInterval = 1f; // スロットル時間
    
    private int _currentBGMIndex;
    private AudioSource _audioSource;
    private MotionHandle _fadeHandle;
    
    private async UniTask CrossFadeToAsync(int newIndex)
    {
        if (newIndex < 0 || newIndex >= bgmClips.Count) throw new ArgumentOutOfRangeException(nameof(newIndex));
        if (_currentBGMIndex == newIndex) return;
        
        _currentBGMIndex = newIndex;

        try
        {
            await LMotion.Create(1f, 0f, fadeTime)
                .WithEase(Ease.OutSine)
                .Bind(v => _audioSource.volume = v)
                .AddTo(this)
                .ToUniTask();
        }
        catch (OperationCanceledException) {}

        _audioSource.Stop();
        _audioSource.clip = bgmClips[_currentBGMIndex];
        _audioSource.Play();

        try
        {
            _fadeHandle = LMotion.Create(0f, 1f, fadeTime)
                .WithEase(Ease.InSine)
                .Bind(v => _audioSource.volume = v)
                .AddTo(this);
            await _fadeHandle.ToUniTask();
        }
        catch (OperationCanceledException) {}
    }

    private void Awake()
    {
        _audioSource = this.GetComponent<AudioSource>();
        _audioSource.loop = true;
        
        if (bgmClips == null || bgmClips.Count == 0) throw new ArgumentNullException(nameof(bgmClips));
        
        _audioSource.clip = bgmClips[0];
        _audioSource.Play();
    }

    private void Start()
    {
        // タイマーで指定した間隔ごとにBGMを更新
        UniTaskAsyncEnumerable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(bgmUpdateInterval))
            .ForEachAsync(_ =>
            {
                var nextIndex = GameManager.Instance.Player.PlayerSpeedInt.CurrentValue;
                CrossFadeToAsync(nextIndex).Forget();
            }, this.GetCancellationTokenOnDestroy()).Forget();
    }
}

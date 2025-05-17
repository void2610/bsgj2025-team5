using System;
using System.Collections.Generic;
using UnityEngine;
using Cysharp.Threading.Tasks;
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
    [SerializeField] private float fadeTime = 1f; // フェード時間
    [SerializeField] private float throttleTime = 2f; // スロットル時間
    
    private int _currentBGMIndex = 0;
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
        
        _audioSource.clip = bgmClips[0];
        _audioSource.Play();
    }

    private void Start()
    {
        // プレイヤーの速度を監視
        GameManager.Instance.Player.PlayerSpeedInt
            .DistinctUntilChanged()
            .ThrottleFirst(TimeSpan.FromSeconds(throttleTime)) // スロットル時間を設定
            .Subscribe(async index => await CrossFadeToAsync(index))
            .AddTo(this);
    }
}

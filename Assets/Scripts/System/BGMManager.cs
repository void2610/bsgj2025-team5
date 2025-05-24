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
public class BGMManager : MonoBehaviour
{
    [SerializeField] private AudioClip bgmClip;
    [SerializeField] private float fadeTime = 0.5f; // フェード時間
    [SerializeField] private float bgmUpdateInterval = 1f; // スロットル時間
    
    [SerializeField] private float basePitch = 1.0f; // 基本ピッチ（スピード0）
    [SerializeField] private List<float> pitchMultipliers = new() { 1.0f, 1.1f, 1.2f, 1.35f, 1.5f }; // 各スピードレベルでのピッチ倍率
    
    private int _currentSpeedLevel;
    private AudioSource _audioSource;
    private MotionHandle _pitchHandle;

    private async UniTask ChangePitchAsync(int newSpeedLevel)
    {
        if (newSpeedLevel < 0 || newSpeedLevel >= pitchMultipliers.Count) return;
        if (_currentSpeedLevel == newSpeedLevel) return;

        var startPitch = _audioSource.pitch;
        var targetPitch = basePitch * pitchMultipliers[newSpeedLevel];
        
        _currentSpeedLevel = newSpeedLevel;

        try
        {
            _pitchHandle = LMotion.Create(startPitch, targetPitch, fadeTime)
                .WithEase(Ease.InOutQuad)
                .Bind(v => _audioSource.pitch = v)
                .AddTo(this);
            
            await _pitchHandle.ToUniTask();
        }
        catch (OperationCanceledException) {}
    }

    private void Awake()
    {
        _audioSource = this.GetComponent<AudioSource>();
        _audioSource.loop = true;

        if (bgmClip == null) throw new ArgumentNullException(nameof(bgmClip));

        _audioSource.clip = bgmClip;
        _audioSource.pitch = basePitch * pitchMultipliers[0];
        _audioSource.Play();
    }

    private void Start()
    {
        // タイマーで指定した間隔ごとにBGMのテンポを更新
        UniTaskAsyncEnumerable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(bgmUpdateInterval))
            .ForEachAsync(_ =>
            {
                var nextSpeedLevel = GameManager.Instance.Player.PlayerSpeedInt.CurrentValue;
                ChangePitchAsync(nextSpeedLevel).Forget();
            }, this.GetCancellationTokenOnDestroy()).Forget();
    }
}
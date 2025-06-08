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
    [Tooltip("BGMとして再生するオーディオクリップ")]
    [SerializeField] private AudioClip bgmClip;
    
    [Tooltip("ピッチ変更時のフェード時間（秒）。大きいほど滑らかに変化します")]
    [SerializeField] private float fadeTime = 0.5f;
    
    [Tooltip("BGMのテンポ更新をチェックする間隔（秒）")]
    [SerializeField] private float bgmUpdateInterval = 1f;
    
    [Tooltip("基本のピッチ（速度レベル0の時）")]
    [SerializeField] private float basePitch = 1.0f;
    
    [Tooltip("各速度レベル（0-4）でのBGMピッチ倍率。大きいほど速いテンポになります")]
    [SerializeField] private List<float> pitchMultipliers = new() { 1.0f, 1.1f, 1.2f, 1.35f, 1.5f };
    
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
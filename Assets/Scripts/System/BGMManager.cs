using System;
using System.Collections.Generic;
using UnityEngine;
using R3;

/// <summary>
/// プレイヤーの速度に応じてBGMを変更するクラス
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class BGMManager : MonoBehaviour
{
    [SerializeField] private List<AudioClip> bgmClips;
    
    private int _currentBGMIndex = 0;
    private AudioSource _audioSource;
    
    private void OnChangePlayerSpeedInt(int value)
    {
        if (_currentBGMIndex == value) return;
        
        _currentBGMIndex = value;
        _audioSource.Stop();
        _audioSource.clip = bgmClips[_currentBGMIndex];
        _audioSource.Play();
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
            .ThrottleFirst(TimeSpan.FromSeconds(2)) // 最低2秒間隔で切り替え
            .Subscribe(OnChangePlayerSpeedInt).AddTo(this);
    }
}

using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 効果音（SE）を一括管理するシングルトンマネージャー
/// AudioSource のプールを使って、複数同時再生やボリューム制御に対応する
/// プールのサイズを超えて再生できないので、パフォーマンスや音量を圧迫することがない
/// SeDataを入力することでSEの再生を行う
/// </summary>
public class SeManager : SingletonMonoBehaviour<SeManager>
{
    [Tooltip("SE用のオーディオミキサーグループ。音量調整やエフェクト処理に使用")]
    [SerializeField] private AudioMixerGroup seMixerGroup;
    
    [Tooltip("同時に再生できるSEの最大数。大きいほど多くの音を重ねられます")]
    [SerializeField] private int audioSourcePoolSize = 20;

    private readonly List<AudioSource> _seAudioSourceList = new();
    private readonly Dictionary<AudioSource, SeData> _playingSeData = new();
    private float _seVolume = 0.5f;

    /// <summary>
    /// SEのボリューム（0.0〜1.0）。変更時はAudioMixerのパラメータに反映され、PlayerPrefsにも保存される。
    /// </summary>
    public float SeVolume
    {
        get => _seVolume;
        set
        {
            _seVolume = value;
            if (value <= 0.0f)
            {
                value = 0.0001f;
            }
            seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(value) * 20);
            PlayerPrefs.SetFloat("SeVolume", value);
        }
    }
    
    public async UniTask PlaySeAsync(SeData data, float volume = 1.0f, float pitch = -1.0f, bool important = false)
    {
        PlaySe(data, volume, pitch, important);
        // 再生が完了するまで待機
        var clipLength = data.audioClip.length;
        await UniTask.Delay((int)(clipLength * 1000));
    }

    /// <summary>
    /// 指定のAudioClipを再生する。AudioSourceプールから空きがない場合、importantでなければ再生されない。
    /// </summary>
    /// <param name="clip">再生するAudioClip</param>
    /// <param name="volume">ボリューム（0.0〜1.0）</param>
    /// <param name="pitch">ピッチ（デフォルト1.0）</param>
    /// <param name="important">trueの場合は再生中のSEを中断してでも再生する</param>
    public void PlaySe(AudioClip clip, float volume = 1.0f, float pitch = 1.0f, bool important = false)
    {
        if (clip == null)
        {
            Debug.LogError("AudioClip could not be found.");
            return;
        }

        // まず空いている AudioSource を探す
        var audioSource = GetUnusedAudioSource();

        // プールが全て埋まっていて取得できなかった場合
        if (audioSource == null)
        {
            if (!important) return;
            // important な SE は強制的に上書き
            audioSource = _seAudioSourceList[0];
            audioSource.Stop();
        }

        // クリップ・ボリューム・ピッチをセットして再生
        audioSource.clip   = clip;
        audioSource.volume = volume;
        audioSource.pitch  = pitch;
        audioSource.Play();
    }

    /// <summary>
    /// SeData を使ってSEを再生する。ピッチに負の値を指定するとランダムピッチになる。
    /// </summary>
    /// <param name="data">SEデータ（AudioClipと基準ボリューム）</param>
    /// <param name="volume">追加のスケール倍率（0.0〜1.0）</param>
    /// <param name="pitch">ピッチ（負の値でランダムピッチ）</param>
    /// <param name="important">trueの場合、空きがなくても強制再生</param>
    public void PlaySe(SeData data, float volume = 1.0f, float pitch = -1.0f, bool important = false)
    {
        // 空いている AudioSource を探す
        var audioSource = GetUnusedAudioSource();
        if (!audioSource)
        {
            if (!important) return;
            // important な SE は強制的に上書き
            audioSource = _seAudioSourceList[0];
            audioSource.Stop();
            _playingSeData.Remove(audioSource);
        }

        audioSource.clip   = data.audioClip;
        audioSource.volume = data.volume * volume;
        
        // pitch 引数が負ならランダムピッチ
        if (pitch < 0f)
            pitch = Random.Range(0.9f, 1.1f);
        audioSource.pitch = pitch;

        audioSource.Play();
        
        // 現在再生中のSeDataを記録
        _playingSeData[audioSource] = data;
    }
    
    /// <summary>
    /// 指定時間待機してからSEを再生する。
    /// </summary>
    /// <param name="data">SEデータ（AudioClipと基準ボリューム）</param>
    /// <param name="delay">待機時間（秒）</param>
    /// <param name="volume">追加のスケール倍率（0.0〜1.0）</param>
    /// <param name="pitch">ピッチ（負の値でランダムピッチ）</param>
    /// <param name="important">trueの場合、空きがなくても強制再生</param>
    public async UniTask WaitAndPlaySe(SeData data, float delay, float volume = 1.0f, float pitch = -1.0f, bool important = false)
    {
        await UniTask.Delay((int)(delay * 1000));
        PlaySe(data, volume, pitch, important);
    }

    /// <summary>
    /// 継続的にSEを再生する（ループ再生）。
    /// </summary>
    /// <param name="data">SEデータ（AudioClipと基準ボリューム）</param>
    /// <param name="volume">追加のスケール倍率（0.0〜1.0）</param>
    /// <param name="pitch">ピッチ（負の値でランダムピッチ）</param>
    /// <param name="important">trueの場合、空きがなくても強制再生</param>
    /// <returns>再生に使用されたAudioSource。停止に使用可能</returns>
    public AudioSource PlaySeLoop(SeData data, float volume = 1.0f, float pitch = -1.0f, bool important = false)
    {
        // 空いている AudioSource を探す
        var audioSource = GetUnusedAudioSource();
        if (!audioSource)
        {
            if (!important) return null;
            // important な SE は強制的に上書き
            audioSource = _seAudioSourceList[0];
            audioSource.Stop();
        }

        audioSource.clip = data.audioClip;
        audioSource.volume = data.volume * volume;
        
        // pitch 引数が負ならランダムピッチ
        if (pitch < 0f)
            pitch = Random.Range(0.9f, 1.1f);
        audioSource.pitch = pitch;
        
        // ループ再生を有効にする
        audioSource.loop = true;
        audioSource.Play();
        
        // 現在再生中のSeDataを記録
        _playingSeData[audioSource] = data;
        
        return audioSource;
    }

    /// <summary>
    /// 指定したSeDataで再生されているSEを全て停止する。
    /// </summary>
    /// <param name="data">停止するSEデータ</param>
    public void StopSe(SeData data)
    {
        var audioSourcesToStop = new List<AudioSource>();
        
        // 指定したSeDataで再生中のAudioSourceを見つける
        foreach (var kvp in _playingSeData)
        {
            if (kvp.Value == data)
            {
                audioSourcesToStop.Add(kvp.Key);
            }
        }
        
        // 見つかったAudioSourceを停止
        foreach (var audioSource in audioSourcesToStop)
        {
            audioSource.Stop();
            audioSource.loop = false;
            _playingSeData.Remove(audioSource);
        }
    }

    /// <summary>
    /// 指定したAudioSourceの再生を停止する。
    /// </summary>
    /// <param name="audioSource">停止するAudioSource</param>
    public void StopSe(AudioSource audioSource)
    {
        if (audioSource != null)
        {
            audioSource.Stop();
            audioSource.loop = false;
            _playingSeData.Remove(audioSource);
        }
    }

    /// <summary>
    /// 再生中でないAudioSourceをリストから取得する。なければnull。
    /// </summary>
    [CanBeNull] private AudioSource GetUnusedAudioSource() => _seAudioSourceList.FirstOrDefault(t => !t.isPlaying);

    protected override void Awake()
    {
        base.Awake();
        // シーン遷移時に破棄されないようにする
        this.transform.parent = null;
        DontDestroyOnLoad(this.gameObject);
        // AudioSource の初期化
        for (var i = 0; i < audioSourcePoolSize; ++i)
        {
            var audioSource = this.gameObject.AddComponent<AudioSource>();
            audioSource.outputAudioMixerGroup = seMixerGroup;
            _seAudioSourceList.Add(audioSource);
        }
    }
    
    private void Start()
    {
        SeVolume = PlayerPrefs.GetFloat("SeVolume", 1.0f);
        seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
    }

    private void Update()
    {
        // 再生が終了したAudioSourceを_playingSeDataから削除
        var audioSourcesToRemove = new List<AudioSource>();
        foreach (var kvp in _playingSeData)
        {
            if (!kvp.Key.isPlaying)
            {
                audioSourcesToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var audioSource in audioSourcesToRemove)
        {
            _playingSeData.Remove(audioSource);
        }
    }
}

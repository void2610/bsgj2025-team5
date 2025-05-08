using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;

public class SeManager : SingletonMonoBehaviour<SeManager>
{
    [SerializeField] private AudioMixerGroup seMixerGroup;

    private readonly AudioSource[] _seAudioSourceList = new AudioSource[20];
    private float _seVolume = 0.5f;

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
        }

        audioSource.clip   = data.audioClip;
        audioSource.volume = data.volume * volume;
        // pitch 引数が負ならランダムピッチ
        if (pitch < 0f)
            pitch = Random.Range(0.9f, 1.1f);
        audioSource.pitch = pitch;

        audioSource.Play();
    }
    
    public void WaitAndPlaySe(SeData data, float delay, float volume = 1.0f, float pitch = 1.0f) => WaitAndPlaySeAsync(data, delay, volume, pitch).Forget();
    
    private async UniTaskVoid WaitAndPlaySeAsync(SeData data, float delay, float volume = 1.0f, float pitch = 1.0f)
    {
        await UniTask.Delay((int)(delay * 1000));
        PlaySe(data, volume, pitch);
    }

    private AudioSource GetUnusedAudioSource() => _seAudioSourceList.FirstOrDefault(t => t.isPlaying == false);

    protected override void Awake()
    {
        base.Awake();
        // シーン遷移時に破棄されないようにする
        DontDestroyOnLoad(this.gameObject);
        // AudioSource の初期化
        for (var i = 0; i < _seAudioSourceList.Length; ++i)
        {
            _seAudioSourceList[i] = gameObject.AddComponent<AudioSource>();
            _seAudioSourceList[i].outputAudioMixerGroup = seMixerGroup;
        }
    }
    
    private void Start()
    {
        SeVolume = PlayerPrefs.GetFloat("SeVolume", 1.0f);
        seMixerGroup.audioMixer.SetFloat("SeVolume", Mathf.Log10(_seVolume) * 20);
    }
}
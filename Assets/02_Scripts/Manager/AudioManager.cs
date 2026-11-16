using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("오디오 믹서 그룹")]
    public AudioMixerGroup bgmMixerGroup;
    public AudioMixerGroup sfxMixerGroup;
    public AudioMixerGroup voiceMixerGroup;
    public AudioMixerGroup uiMixerGroup;
    public AudioMixerGroup playerStatusMixerGroup;

    [Header("사운드 에셋 중앙 저장소")]
    [Tooltip("프로젝트 내의 모든 AudioClip을 이곳에 할당하십시오.")]
    public List<AudioClip> allAudioClips;

    [Header("글로벌 2D 오디오 소스 (인스펙터 할당 필수)")]
    public AudioSource bgmSource;
    public AudioSource statusSource;
    public AudioSource breathSource;

    [Header("사용자 음량 설정")]
    [Range(0f, 1f)] public float defaultMasterVolume = 0.85f;
    [Range(0f, 1f)] public float defaultSfxVolume = 0.9f;
    [Range(0f, 1f)] public float defaultBgmVolume = 0.55f;
    [Range(0f, 1f)] public float defaultVoiceVolume = 0.8f;

    public float MasterVolume { get; private set; } = 0.85f;
    public float SfxVolume { get; private set; } = 0.9f;
    public float BgmVolume { get; private set; } = 0.55f;
    public float VoiceVolume { get; private set; } = 0.8f;

    private float _baseBgmSourceVolume = 1f;
    private float _baseStatusSourceVolume = 1f;
    private float _baseBreathSourceVolume = 1f;
    private readonly Dictionary<AudioSource, float> _activePoolBaseVolumes = new Dictionary<AudioSource, float>();
    private readonly Dictionary<AudioSource, bool> _activePoolVoiceFlags = new Dictionary<AudioSource, bool>();

    private Dictionary<string, AudioClip> _clipDictionary = new Dictionary<string, AudioClip>();

    // [완전 최적화] SFX 오디오 소스 오브젝트 풀링 (Instantiate/Destroy 폐기)
    private Queue<AudioSource> _sfxPool = new Queue<AudioSource>();
    private List<AudioSource> _activeSfxSources = new List<AudioSource>();
    private GameObject _poolRoot;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        MasterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("404_Audio_Master", defaultMasterVolume));
        SfxVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("404_Audio_SFX", defaultSfxVolume));
        BgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("404_Audio_BGM", defaultBgmVolume));
        VoiceVolume = Mathf.Clamp01(PlayerPrefs.GetFloat("404_Audio_Voice", defaultVoiceVolume));

        _baseBgmSourceVolume = bgmSource != null ? bgmSource.volume : 1f;
        _baseStatusSourceVolume = statusSource != null ? statusSource.volume : 1f;
        _baseBreathSourceVolume = breathSource != null ? breathSource.volume : 1f;

        if (allAudioClips != null)
        {
            foreach (AudioClip clip in allAudioClips)
            {
                if (clip != null && !_clipDictionary.ContainsKey(clip.name))
                    _clipDictionary.Add(clip.name, clip);
            }
        }

        // 초기 오브젝트 풀 생성 (15개)
        _poolRoot = new GameObject("SFX_ObjectPool");
        _poolRoot.transform.SetParent(this.transform);
        for (int i = 0; i < 15; i++)
        {
            AudioSource src = _poolRoot.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            _sfxPool.Enqueue(src);
        }

        ApplyAudioVolumes();
        Debug.Log($"[AudioManager] 총 {_clipDictionary.Count}개의 사운드 인덱싱 및 15개의 오브젝트 풀 생성 완료.");
    }

    private void Update()
    {
        // [제로 가비지] 재생이 끝난 소스를 감지하여 풀에 자동 반환
        for (int i = _activeSfxSources.Count - 1; i >= 0; i--)
        {
            if (!_activeSfxSources[i].isPlaying)
            {
                _activeSfxSources[i].clip = null;
                _sfxPool.Enqueue(_activeSfxSources[i]);
                _activeSfxSources.RemoveAt(i);
            }
        }
    }

    public AudioClip GetClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        if (_clipDictionary.TryGetValue(clipName, out AudioClip clip)) return clip;

        // Resources/Audio 폴더에 추가된 클립은 Inspector 목록을 수정하지 않아도 이름으로 자동 로드합니다.
        clip = Resources.Load<AudioClip>("Audio/" + clipName);
        if (clip != null)
        {
            _clipDictionary[clipName] = clip;
            return clip;
        }

        Debug.LogWarning($"[AudioManager] 사운드 파일을 찾을 수 없습니다: {clipName}. allAudioClips 또는 Resources/Audio를 확인하세요.");
        return null;
    }

    public void PlayGlobal2D(string clipName, AudioMixerGroup mixerGroup = null, float volume = 1f)
    {
        PlayGlobal2D(clipName, mixerGroup, volume, 0f);
    }

    public void PlayGlobal2D(string clipName, AudioMixerGroup mixerGroup, float volume, float startTime)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        AudioSource source;
        if (_sfxPool.Count > 0) source = _sfxPool.Dequeue();
        else
        {
            source = _poolRoot.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
        }

        source.clip = clip;
        bool isVoiceGroup = voiceMixerGroup != null && mixerGroup == voiceMixerGroup;
        float baseVolume = Mathf.Clamp01(volume);
        _activePoolBaseVolumes[source] = baseVolume;
        _activePoolVoiceFlags[source] = isVoiceGroup;
        float categoryVolume = isVoiceGroup ? VoiceVolume : SfxVolume;
        source.volume = baseVolume * MasterVolume * categoryVolume;
        source.outputAudioMixerGroup = mixerGroup ?? sfxMixerGroup;
        source.time = Mathf.Clamp(startTime, 0f, Mathf.Max(0f, clip.length - 0.02f));
        source.Play();

        _activeSfxSources.Add(source);
    }

    public void PlayBGM(string clipName)
    {
        if (bgmSource == null) return;
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        if (bgmSource.clip == clip)
        {
            if (!bgmSource.isPlaying)
            {
                bgmSource.loop = true;
                bgmSource.Play();
            }
            return;
        }

        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.volume = _baseBgmSourceVolume * MasterVolume * BgmVolume;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void SetMasterVolume(float value)
    {
        MasterVolume = Mathf.Clamp01(value);
        SaveAudioVolumes();
        ApplyAudioVolumes();
    }

    public void SetSfxVolume(float value)
    {
        SfxVolume = Mathf.Clamp01(value);
        SaveAudioVolumes();
        ApplyAudioVolumes();
    }

    public void SetBgmVolume(float value)
    {
        BgmVolume = Mathf.Clamp01(value);
        SaveAudioVolumes();
        ApplyAudioVolumes();
    }

    public void SetVoiceVolume(float value)
    {
        VoiceVolume = Mathf.Clamp01(value);
        SaveAudioVolumes();
        ApplyAudioVolumes();
    }

    private void SaveAudioVolumes()
    {
        PlayerPrefs.SetFloat("404_Audio_Master", MasterVolume);
        PlayerPrefs.SetFloat("404_Audio_SFX", SfxVolume);
        PlayerPrefs.SetFloat("404_Audio_BGM", BgmVolume);
        PlayerPrefs.SetFloat("404_Audio_Voice", VoiceVolume);
        PlayerPrefs.Save();
    }

    private void ApplyAudioVolumes()
    {
        if (bgmSource != null)
            bgmSource.volume = _baseBgmSourceVolume * MasterVolume * BgmVolume;
        if (statusSource != null)
            statusSource.volume = _baseStatusSourceVolume * MasterVolume * SfxVolume;
        if (breathSource != null)
            breathSource.volume = _baseBreathSourceVolume * MasterVolume * SfxVolume;

        // 현재 재생 중인 효과음/음성에도 슬라이더 변경을 즉시 반영합니다.
        List<AudioSource> destroyedSources = null;
        foreach (var pair in _activePoolBaseVolumes)
        {
            AudioSource source = pair.Key;
            if (source == null)
            {
                if (destroyedSources == null) destroyedSources = new List<AudioSource>();
                destroyedSources.Add(source);
                continue;
            }

            bool isVoice = _activePoolVoiceFlags.TryGetValue(source, out bool voiceFlag) && voiceFlag;
            float categoryVolume = isVoice ? VoiceVolume : SfxVolume;
            source.volume = pair.Value * MasterVolume * categoryVolume;
        }

        if (destroyedSources != null)
        {
            foreach (AudioSource source in destroyedSources)
            {
                _activePoolBaseVolumes.Remove(source);
                _activePoolVoiceFlags.Remove(source);
            }
        }
    }

    public void StopBGM()
    {
        if (bgmSource != null && bgmSource.isPlaying) bgmSource.Stop();
    }

    public void PlayStatusSound(string clipName)
    {
        if (statusSource == null) return;
        AudioClip clip = GetClip(clipName);
        if (clip == null || statusSource.clip == clip) return;

        statusSource.outputAudioMixerGroup = playerStatusMixerGroup;
        statusSource.volume = _baseStatusSourceVolume * MasterVolume * SfxVolume;
        statusSource.clip = clip;
        statusSource.loop = true;
        statusSource.Play();
    }

    public void StopStatusSound()
    {
        if (statusSource != null && statusSource.isPlaying) statusSource.Stop();
    }

    public void PlayBreathSound(string clipName)
    {
        if (breathSource == null) return;
        AudioClip clip = GetClip(clipName);
        if (clip == null || breathSource.clip == clip) return;

        breathSource.outputAudioMixerGroup = playerStatusMixerGroup;
        breathSource.volume = _baseBreathSourceVolume * MasterVolume * SfxVolume;
        breathSource.clip = clip;
        breathSource.loop = true;
        breathSource.Play();
    }

    public void StopBreathSound()
    {
        if (breathSource != null && breathSource.isPlaying) breathSource.Stop();
    }
}

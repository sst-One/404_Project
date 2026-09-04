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

        foreach (AudioClip clip in allAudioClips)
        {
            if (clip != null && !_clipDictionary.ContainsKey(clip.name))
            {
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

        Debug.LogWarning($"[AudioManager] 사운드 파일을 찾을 수 없습니다: {clipName}");
        return null;
    }

    public void PlayGlobal2D(string clipName, AudioMixerGroup mixerGroup = null, float volume = 1f)
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
        source.volume = volume;
        source.outputAudioMixerGroup = mixerGroup ?? sfxMixerGroup;
        source.Play();

        _activeSfxSources.Add(source);
    }

    public void PlayBGM(string clipName)
    {
        if (bgmSource == null) return;
        AudioClip clip = GetClip(clipName);
        if (clip == null || bgmSource.clip == clip) return;

        bgmSource.outputAudioMixerGroup = bgmMixerGroup;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
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
        breathSource.clip = clip;
        breathSource.loop = true;
        breathSource.Play();
    }

    public void StopBreathSound()
    {
        if (breathSource != null && breathSource.isPlaying) breathSource.Stop();
    }
}
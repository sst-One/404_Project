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
    public AudioSource statusSource; // 심장소리, 이명 등
    public AudioSource breathSource; // [추가] 플레이어 숨소리 전담

    private Dictionary<string, AudioClip> clipDictionary = new Dictionary<string, AudioClip>();

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
            if (clip != null && !clipDictionary.ContainsKey(clip.name))
            {
                clipDictionary.Add(clip.name, clip);
            }
        }
        Debug.Log($"[AudioManager] 총 {clipDictionary.Count}개의 사운드 에셋 인덱싱 완료.");
    }

    public AudioClip GetClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        if (clipDictionary.TryGetValue(clipName, out AudioClip clip)) return clip;

        Debug.LogWarning($"[AudioManager] 사운드 파일을 찾을 수 없습니다: {clipName}");
        return null;
    }

    // 일회성 2D 환경음 생성 후 자동 파괴
    public void PlayGlobal2D(string clipName, AudioMixerGroup mixerGroup = null, float volume = 1f)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        GameObject tempObj = new GameObject("GlobalAudio_" + clipName);
        AudioSource source = tempObj.AddComponent<AudioSource>();

        source.clip = clip;
        source.spatialBlend = 0f;
        source.volume = volume;
        source.outputAudioMixerGroup = mixerGroup ?? sfxMixerGroup;
        source.Play();

        Destroy(tempObj, clip.length + 0.1f);
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

    // [추가] 플레이어 숨소리 전용 통제
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
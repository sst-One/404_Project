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

        // O(1) 고속 탐색을 위한 딕셔너리 캐싱 (유니티 클립 이름에는 확장자가 포함되지 않음)
        foreach (AudioClip clip in allAudioClips)
        {
            if (clip != null && !clipDictionary.ContainsKey(clip.name))
            {
                clipDictionary.Add(clip.name, clip);
            }
        }
        Debug.Log($"[AudioManager] 총 {clipDictionary.Count}개의 사운드 에셋 인덱싱 완료.");
    }

    // 씬 내의 3D 오브젝트들이 사운드 파일을 요청할 때 호출
    public AudioClip GetClip(string clipName)
    {
        if (clipDictionary.TryGetValue(clipName, out AudioClip clip))
        {
            return clip;
        }
        Debug.LogWarning($"[AudioManager] 사운드 파일을 찾을 수 없습니다: {clipName}");
        return null;
    }

    // UI나 전역 2D 사운드(환각 등 위치가 필요 없는 소리) 전용 강제 재생
    public void PlayGlobal2D(string clipName, AudioMixerGroup mixerGroup = null, float volume = 1f)
    {
        AudioClip clip = GetClip(clipName);
        if (clip == null) return;

        GameObject tempObj = new GameObject("GlobalAudio_" + clipName);
        AudioSource source = tempObj.AddComponent<AudioSource>();

        source.clip = clip;
        source.spatialBlend = 0f; // 완전 2D
        source.volume = volume;
        source.outputAudioMixerGroup = mixerGroup ?? sfxMixerGroup;
        source.Play();

        Destroy(tempObj, clip.length + 0.1f);
    }
}
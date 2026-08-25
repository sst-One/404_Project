using System.Collections;
using UnityEngine;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem crackedPhone;
    public InteractableItem endingGem;

    [Header("Item Audio Reference")]
    public AudioSource phoneAudioSource;
    public AudioSource tvAudioSource;

    // [핫픽스 2] TV 영상 재생기 레퍼런스 추가
    [Header("TV Video Reference")]
    public GameObject tvScreenDisplay;
    public UnityEngine.Video.VideoPlayer tvVideoPlayer;

    [Header("Audio Clip Names")]
    public string phoneRingClipName = "SND-058_PhoneRing_Loop";
    public string tvNewsClipName = "SND-060_NewsReport_Loop";
    public string tinnitusClipName = "SND-061_TinnitusRing_Loop_Timed";
    public string gemDropClipName = "SND-062_GemDrop_OneShot";

    private bool isPhoneAnswered = false;

    private void Start()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();
        if (endingGem != null) endingGem.gameObject.SetActive(false);
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(false); // 시작 시 화면 꺼둠

        if (crackedPhone != null)
        {
            crackedPhone.onInteractEvent.RemoveAllListeners();
            crackedPhone.onInteractEvent.AddListener(OnPhoneAnswered);
        }

        StartCoroutine(InitAndFadeInSequence());
    }

    private IEnumerator InitAndFadeInSequence()
    {
        if (UIManager.Instance != null && UIManager.Instance.globalFadeCanvasGroup != null)
        {
            UIManager.Instance.globalFadeCanvasGroup.alpha = 1f;
            float duration = 2.0f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                UIManager.Instance.globalFadeCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
            UIManager.Instance.globalFadeCanvasGroup.alpha = 0f;
        }
        else yield return new WaitForSeconds(2.0f);

        yield return new WaitForSeconds(3.0f);

        if (phoneAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phoneRingClipName);
            if (clip != null)
            {
                phoneAudioSource.clip = clip;
                phoneAudioSource.loop = true;
                phoneAudioSource.Play();
            }
        }
    }

    private void OnPhoneAnswered()
    {
        if (isPhoneAnswered) return;
        isPhoneAnswered = true;

        if (crackedPhone != null)
        {
            crackedPhone.enabled = false;
            if (crackedPhone.GetComponent<Collider>() != null) crackedPhone.GetComponent<Collider>().enabled = false;
        }

        if (phoneAudioSource != null) phoneAudioSource.Stop();

        StartCoroutine(CallAndNewsSequence());
    }

    private IEnumerator CallAndNewsSequence()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_Police);
        yield return new WaitForSeconds(4.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        // [핫픽스 2] Day 5 뉴스 영상 시각적 출력 로직 동기화
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(true);
        if (tvVideoPlayer != null) tvVideoPlayer.Play();

        if (tvAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null)
            {
                tvAudioSource.clip = clip;
                tvAudioSource.Play();
            }
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_TVNews);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStatusSound(tinnitusClipName);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        yield return new WaitForSeconds(6.0f);

        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(gemDropClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(true);
            endingGem.onInteractEvent.RemoveAllListeners();
            endingGem.onInteractEvent.AddListener(OnGemReached);
        }
    }

    private void OnGemReached()
    {
        if (endingGem != null)
        {
            endingGem.enabled = false;
            if (endingGem.GetComponent<Collider>() != null) endingGem.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(FlashbackAndEndingSequence());
    }

    private IEnumerator FlashbackAndEndingSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        yield return new WaitForSeconds(10.0f);

        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
    }
}
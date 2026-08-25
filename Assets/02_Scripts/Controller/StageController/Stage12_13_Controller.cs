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
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_Police));

        if (tvAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null)
            {
                tvAudioSource.clip = clip;
                tvAudioSource.Play();
            }
        }

        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_TVNews));

        // 이명은 2D 글로벌 Status 사운드 취급
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayStatusSound(tinnitusClipName);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        yield return new WaitForSeconds(6.0f);

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
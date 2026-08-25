using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem crackedPhone;
    public InteractableItem endingGem;

    [Header("Phone UI Reference")]
    public PhoneUIController phoneUI;

    [Header("Item Audio Reference")]
    public AudioSource phoneAudioSource;
    public AudioSource tvAudioSource;

    [Header("TV Video Reference")]
    public GameObject tvScreenDisplay;
    public UnityEngine.Video.VideoPlayer tvVideoPlayer;

    [Header("Flashback Overlays")]
    public Image[] flashbackImages;

    [Header("Audio Clip Names")]
    public string phoneRingClipName = "SND-058_PhoneRing_Loop";
    public string tvNewsClipName = "SND-060_NewsReport_Loop";
    public string tinnitusClipName = "SND-061_TinnitusRing_Loop_Timed";
    public string gemDropClipName = "SND-062_GemDrop_OneShot";
    public string flashbackSnd1 = "SND-068_Flashback_Layer1";
    public string flashbackSnd2 = "SND-069_Flashback_Layer2";
    public string flashbackSnd3 = "SND-070_Flashback_Layer3";

    private bool isPhoneAnswered = false;

    private void Start()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();
        if (endingGem != null) endingGem.gameObject.SetActive(false);
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(false);
        foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }

        if (crackedPhone != null)
        {
            crackedPhone.onInteractEvent.RemoveAllListeners();
            crackedPhone.onInteractEvent.AddListener(OnPhoneAnswered);
        }

        StartCoroutine(InitAndFadeInSequence());
    }

    private IEnumerator InitAndFadeInSequence()
    {
        if (phoneUI != null)
        {
            phoneUI.SetCrackedScreen(true);
            phoneUI.ShowPoliceReceive();
        }

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(5));

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
        if (phoneUI != null) phoneUI.HideAllScreens();

        StartCoroutine(CallAndNewsSequence());
    }

    private IEnumerator CallAndNewsSequence()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_Police);
        yield return new WaitForSeconds(4.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(true);
        if (tvVideoPlayer != null) tvVideoPlayer.Play();

        if (tvAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null) { tvAudioSource.clip = clip; tvAudioSource.Play(); }
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_TVNews);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayStatusSound(tinnitusClipName);
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        yield return new WaitForSeconds(6.0f);

        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(gemDropClipName, AudioManager.Instance.sfxMixerGroup);

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

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(flashbackSnd1, AudioManager.Instance.playerStatusMixerGroup);
            AudioManager.Instance.PlayGlobal2D(flashbackSnd2, AudioManager.Instance.playerStatusMixerGroup);
            AudioManager.Instance.PlayGlobal2D(flashbackSnd3, AudioManager.Instance.playerStatusMixerGroup);
        }

        float duration = 12.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            foreach (var img in flashbackImages)
            {
                if (img != null)
                {
                    bool isVisible = Random.value > 0.5f;
                    img.gameObject.SetActive(isVisible);
                    if (isVisible)
                    {
                        Color c = img.color;
                        c.a = Random.Range(0.2f, 0.8f);
                        img.color = c;
                    }
                }
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }
        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
    }
}
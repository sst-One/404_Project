using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem endingGem;

    [Header("Item Audio Reference")]
    public AudioSource phoneAudioSource;
    public AudioSource tvAudioSource;

    [Header("TV Video Reference")]
    public GameObject tvScreenDisplay;
    // [핫픽스] 캔버스가 꺼져 있어서 영상이 안 나오는 문제를 잡기 위한 부모 캔버스 참조 변수
    public Canvas tvCanvas;
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

        if (flashbackImages != null)
        {
            foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }
        }

        StartCoroutine(InitAndFadeInSequence());
    }

    private IEnumerator InitAndFadeInSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(5));
        yield return new WaitForSeconds(1.0f);

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null)
            {
                PhoneController.Instance.phoneUI.SetCrackedScreen(true);
                PhoneController.Instance.phoneUI.ShowPoliceReceive();
            }

            var interactable = PhoneController.Instance.GetComponent<InteractableItem>();
            if (interactable != null)
            {
                interactable.isInteractable = true;
                if (interactable.GetComponent<Collider>() != null) interactable.GetComponent<Collider>().enabled = true;
                interactable.onInteractEvent.RemoveAllListeners();
                interactable.onInteractEvent.AddListener(OnPhoneAnswered);
            }
        }

        yield return new WaitForSeconds(3.0f);

        if (phoneAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phoneRingClipName);
            if (clip != null) { phoneAudioSource.clip = clip; phoneAudioSource.loop = true; phoneAudioSource.Play(); }
        }
    }

    private void OnPhoneAnswered()
    {
        if (isPhoneAnswered) return;
        isPhoneAnswered = true;

        if (PhoneController.Instance != null)
        {
            var interactable = PhoneController.Instance.GetComponent<InteractableItem>();
            if (interactable != null)
            {
                interactable.isInteractable = false;
                if (interactable.GetComponent<Collider>() != null) interactable.GetComponent<Collider>().enabled = false;
            }
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.HideAllScreens();
            PhoneController.Instance.HidePhone();
        }

        if (phoneAudioSource != null) phoneAudioSource.Stop();
        StartCoroutine(CallAndNewsSequence());
    }

    private IEnumerator CallAndNewsSequence()
    {
        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_Police);
        yield return new WaitForSeconds(4.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        // [핵심 핫픽스] 영상 플레이 전 캔버스 전체를 강제 활성화합니다.
        if (tvCanvas != null) tvCanvas.gameObject.SetActive(true);
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
            endingGem.isInteractable = true;
            if (endingGem.GetComponent<Collider>() != null) endingGem.GetComponent<Collider>().enabled = true;
            endingGem.onInteractEvent.RemoveAllListeners();
            endingGem.onInteractEvent.AddListener(OnGemReached);
        }
    }

    private void OnGemReached()
    {
        if (endingGem != null)
        {
            endingGem.isInteractable = false;
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
            if (flashbackImages != null)
            {
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
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        if (flashbackImages != null)
        {
            foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }
        }

        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
    }
}
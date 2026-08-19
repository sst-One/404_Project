using System.Collections;
using UnityEngine;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem crackedPhone;
    public InteractableItem endingGem;

    [Header("Audio Sources")]
    public AudioSource phoneRingSource;
    public AudioSource tvNewsSource;
    public AudioSource tinnitusSource;
    public AudioSource gemDropSource;

    [Header("Audio Clip Names")]
    public string phoneRingClipName = "SND-058_PhoneRing_Loop";
    public string tvNewsClipName = "SND-060_NewsReport_Loop";
    public string tinnitusClipName = "SND-061_TinnitusRing_Loop_Timed";
    public string gemDropClipName = "SND-062_GemDrop_OneShot";

    private bool isPhoneAnswered = false;

    private void Start()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.ResetHeartbeat();
        }

        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(false);
        }

        if (crackedPhone != null)
        {
            crackedPhone.onInteractEvent.RemoveAllListeners();
            crackedPhone.onInteractEvent.AddListener(OnPhoneAnswered);
        }

        StartCoroutine(InitAndFadeInSequence());
    }

    private IEnumerator InitAndFadeInSequence()
    {
        // UIManager의 전역 페이드 캔버스를 활용한 페이드 인 (검은 화면 -> 밝아짐)
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
        else
        {
            yield return new WaitForSeconds(2.0f);
        }

        yield return new WaitForSeconds(3.0f);

        if (phoneRingSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phoneRingClipName);
            if (clip != null)
            {
                phoneRingSource.clip = clip;
                phoneRingSource.loop = true;
                phoneRingSource.Play();
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
            if (crackedPhone.GetComponent<Collider>() != null)
                crackedPhone.GetComponent<Collider>().enabled = false;
        }

        if (phoneRingSource != null) phoneRingSource.Stop();

        StartCoroutine(CallAndNewsSequence());
    }

    private IEnumerator CallAndNewsSequence()
    {
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_Police));
        }

        if (tvNewsSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null)
            {
                tvNewsSource.clip = clip;
                tvNewsSource.Play();
            }
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_TVNews));
        }

        if (tinnitusSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tinnitusClipName);
            if (clip != null)
            {
                tinnitusSource.clip = clip;
                tinnitusSource.Play();
            }
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(2);
        }

        // 이명 발생 후 보석 낙하 전까지의 대기 시간
        yield return new WaitForSeconds(6.0f);

        if (gemDropSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(gemDropClipName);
            if (clip != null) gemDropSource.PlayOneShot(clip);
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
            if (endingGem.GetComponent<Collider>() != null)
                endingGem.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(FlashbackAndEndingSequence());
    }

    private IEnumerator FlashbackAndEndingSequence()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(3);
        }

        // 플래시백 연출 시퀀스 대기 시간 (Timeline 또는 애니메이터가 재생되는 시간)
        yield return new WaitForSeconds(10.0f);

        // 엔딩 씬으로 전이 전 자연스러운 화면 페이드 아웃
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }
    }
}
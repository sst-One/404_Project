// 2. Stage12_13_Controller.cs
using System.Collections;
using UnityEngine;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem crackedPhone;
    public InteractableItem endingGem;

    [Header("Audio Triggers (AudioSources)")]
    public AudioSource phoneRingSource;
    public AudioSource tvNewsSource;
    public AudioSource tinnitusSource;
    public AudioSource gemDropSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string phoneRingClipName = "";
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
        GameObject blackObj = GameObject.Find("ForcedBlackScreen");
        if (blackObj != null)
        {
            UnityEngine.UI.Image img = blackObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
            {
                float duration = 2.0f;
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    img.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }
            Destroy(blackObj);
        }

        Debug.Log("[Stage12_13] Day 5 아침 시작. 3초 후 전화 수신.");
        yield return new WaitForSeconds(3.0f);

        if (phoneRingSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phoneRingClipName);
            if (clip != null)
            {
                phoneRingSource.clip = clip;
                phoneRingSource.Play();
            }
        }
        Debug.Log("[Stage12_13] 전화벨 울림. 휴대폰 Reach 입력 대기 중...");
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
        Debug.Log("[Stage12_13] 경찰 통화 및 TV 뉴스 재생 시작.");

        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_Police));
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

        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day5_TVNews));
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

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        yield return new WaitForSeconds(6.0f);

        Debug.Log("[Stage12_13] 땡그랑. 보석 낙하.");
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
        Debug.Log("[Stage12_13] 보석-반지 일치 확인. Heartbeat Overload 및 플래시백 시작.");

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(3);
        }

        yield return new WaitForSeconds(10.0f);

        Debug.Log("[Stage12_13] 연출 종료. 09_Ending 씬 로드 요청.");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }
    }
}
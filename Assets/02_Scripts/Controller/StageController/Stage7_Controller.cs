using System.Collections;
using UnityEngine;

public class Stage7_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem gemItem;
    public GameObject suspiciousMan;

    [Header("Audio Clip Names")]
    public string disasterAlertClip = "SND-028_Alert_OneShot";
    public string gemGetClipName = "SND-041_JemRecieve_OneShot";

    private void Start()
    {
        if (suspiciousMan != null)
        {
            suspiciousMan.SetActive(true);
            EnemyAI ai = suspiciousMan.GetComponent<EnemyAI>();
            if (ai != null) ai.isNarrativeMode = true;
        }

        if (gemItem != null)
        {
            gemItem.isInteractable = false;
            if (gemItem.GetComponent<Collider>() != null) gemItem.GetComponent<Collider>().enabled = false;
            gemItem.onInteractAction -= OnGemReached;
            gemItem.onInteractAction += OnGemReached;
        }

        StartCoroutine(Day4IntroSequence());
    }

    private void OnDestroy()
    {
        if (gemItem != null) gemItem.onInteractAction -= OnGemReached;
    }

    private IEnumerator Day4IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(4));

        yield return new WaitForSeconds(1.0f);

        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);

        if (phone != null)
        {
            phone.ShowPhoneInHand();
            if (phone.phoneUI != null) phone.phoneUI.ShowMsgDay4();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);

            yield return new WaitForSeconds(5.0f);
            phone.HidePhone();
        }

        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_1));

        // 음성 대사와 자막을 동시에 시작하고, 다음 대사로 넘어가는 시점은 음원 길이로 결정합니다.
        yield return StartCoroutine(PlayVoiceSubtitle(
            "아… 선생님도 여기 사세요?", "SND-035_6_선생님도여기사세요_OneShot"));
        yield return StartCoroutine(PlayVoiceSubtitle(
            "조심해서 들어가요.", "SND-035_7_조심해서들어가요_OneShot"));

        if (gemItem != null) gemItem.EnableInteractionWithLight();
    }

    private void OnGemReached()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(gemGetClipName, AudioManager.Instance.sfxMixerGroup);
        if (gemItem != null) gemItem.gameObject.SetActive(false);
        StartCoroutine(EndStage7Sequence());
    }

    private IEnumerator EndStage7Sequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        yield return new WaitForSeconds(2.0f);
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
    }

    private IEnumerator PlayVoiceSubtitle(string message, string clipName)
    {
        AudioClip clip = AudioManager.Instance != null ? AudioManager.Instance.GetClip(clipName) : null;
        float duration = clip != null ? clip.length : Mathf.Max(2.5f, message.Length * 0.1f);

        if (AudioManager.Instance != null && clip != null)
            AudioManager.Instance.PlayGlobal2D(clipName, AudioManager.Instance.voiceMixerGroup);
        else
            Debug.LogWarning($"[Stage7_Controller] 대사 음원을 찾을 수 없습니다: {clipName}");

        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(message, duration));
        else
            yield return new WaitForSecondsRealtime(duration);
    }
}
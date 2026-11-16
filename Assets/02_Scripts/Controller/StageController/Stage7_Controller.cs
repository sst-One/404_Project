using System.Collections;
using UnityEngine;

public class Stage7_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem gemItem;
    public GameObject suspiciousMan;
    [Tooltip("선택 A에서 보석을 제시할 남자의 손 위치. 씬에 손 소켓을 만들면 연결하세요.")]
    public Transform gemHandPoint;
    [Tooltip("선택 B에서 보석을 내려놓을 현관 문턱 위치. 씬에 위치점을 만들면 연결하세요.")]
    public Transform gemDoorThresholdPoint;

    [Header("Audio Clip Names")]
    public string disasterAlertClip = NarrativeData.EmergencyMessageSound;
    public string gemGetClipName = "SND-041_JemRecieve_OneShot";

    private void Start()
    {
        if (suspiciousMan != null)
        {
            // v07: Day4 재난문자를 먼저 보여준 뒤 남자가 등장합니다.
            suspiciousMan.SetActive(false);
            EnemyAI ai = suspiciousMan.GetComponent<EnemyAI>();
            if (ai != null) ai.isNarrativeMode = true;
        }

        if (gemItem != null)
        {
            // 대사가 끝나기 전에는 보석 오브젝트 자체를 숨겨 렌더링되지 않게 합니다.
            gemItem.isInteractable = false;
            gemItem.onInteractAction -= OnGemReached;
            gemItem.onInteractAction += OnGemReached;
            StageVisuals.ReplaceGem(gemItem);
            gemItem.gameObject.SetActive(false);
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
            if (phone.phoneUI != null) phone.phoneUI.ShowLockDay4();
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(1f);
            if (phone.phoneUI != null) phone.phoneUI.ShowMsgDay4();

            yield return new WaitForSeconds(5.0f);
            phone.HidePhone();
        }

        // v07 EVT-017: 재난문자를 읽은 뒤에 남자를 등장시킵니다.
        if (suspiciousMan != null) suspiciousMan.SetActive(true);
        if (suspiciousMan != null && PlayerController.Instance != null)
        {
            const float conversationDistance = 1.75f;
            Transform player = PlayerController.Instance.transform;
            while (suspiciousMan != null && player != null &&
                Vector3.ProjectOnPlane(suspiciousMan.transform.position - player.position, Vector3.up).sqrMagnitude >
                conversationDistance * conversationDistance)
                yield return null;
        }
        yield return StartCoroutine(ShowNarrativeLine("아… 선생님도 여기 사세요?", "SND-035_6_선생님도여기사세요_OneShot"));
        yield return StartCoroutine(ShowNarrativeLine("조심해서 들어가요.", "SND-035_7_조심해서들어가요_OneShot"));

        if (gemItem != null)
        {
            gemItem.gameObject.SetActive(true);
            gemItem.EnableInteractionWithLight();
            Physics.SyncTransforms();
        }
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
        yield return new WaitForSeconds(0.5f);
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day4_OpenDoor));
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
    }

    private IEnumerator ShowNarrativeLine(string message, string clipName = null)
    {
        float duration = Mathf.Clamp(message.Length * 0.085f, 2.2f, 6.0f);
        AudioManager audio = AudioManager.Instance;
        AudioClip clip = audio != null && clipName != null ? audio.GetClip(clipName) : null;
        if (clip != null)
        {
            duration = clip.length;
            audio.PlayGlobal2D(clipName, audio.voiceMixerGroup);
        }
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(message, duration));
        else
            yield return new WaitForSecondsRealtime(duration);
    }
}

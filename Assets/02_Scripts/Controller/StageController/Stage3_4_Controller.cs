using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Stage3_4_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem elevatorButton;
    public ElevatorDoorController doorController;
    public EnemyAI suspiciousMan;
    [SerializeField] private Vector3 passengerEntryOffset = new Vector3(0f, 0f, -0.48f);
    [SerializeField, Min(0.1f)] private float passengerEntryDuration = 1.35f;

    [Header("Item Audio Reference")]
    public AudioSource elevatorAudioSource;

    [Header("Audio Clip Names")]
    public string disasterAlertClip = NarrativeData.EmergencyMessageSound;
    public string malfunctionClipName = "SND-014_ButtonMalfunction_OneShot";
    public string moveClipName = "SND-004_ElevatorMove_Loop_Timed";
    public string doorGrabClipName = "SND-015_ElevatorDoorGrab_OneShot";
    public string suspectVoiceClipName = "SND-035_SuspectVoice_OneShot_임시";

    private int buttonPressCount = 0;
    private PhoneController _introPhone;

    private void Start()
    {
        if (doorController != null) doorController.SetDoorsOpenImmediately();

        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(false);
            suspiciousMan.isNarrativeMode = true;
        }

        if (elevatorButton != null)
        {
            // 엘리베이터 버튼은 거리 제한 없이 바라보고 손동작으로 상호작용합니다.
            elevatorButton.ignorePlayerDistanceLimit = true;
            elevatorButton.interactOnlyOnce = false;
            elevatorButton.isInteractable = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;

            elevatorButton.onInteractAction -= OnElevatorButtonPressed;
            elevatorButton.onInteractAction += OnElevatorButtonPressed;
        }

        StartCoroutine(Day2IntroSequence());
    }

    private void OnDestroy()
    {
        if (elevatorButton != null) elevatorButton.onInteractAction -= OnElevatorButtonPressed;
        if (_introPhone != null) _introPhone.HidePhone();
    }

    private IEnumerator Day2IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(2));
        yield return new WaitForSeconds(1.0f);

        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);

        if (phone != null)
        {
            _introPhone = phone;
            phone.ShowPhoneInHand();
            if (phone.phoneUI != null) phone.phoneUI.ShowLockDay2();
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(1f);
            if (phone.phoneUI != null) phone.phoneUI.ShowMsgDay2();
            yield return new WaitForSeconds(5.0f);
            phone.HidePhone();
            _introPhone = null;
        }

        if (elevatorButton != null) elevatorButton.EnableInteractionWithLight();
    }

    private void OnElevatorButtonPressed()
    {
        buttonPressCount++;
        if (elevatorButton != null)
        {
            elevatorButton.isInteractable = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (buttonPressCount == 1) StartCoroutine(Stage3_AnomalySequence());
        else if (buttonPressCount == 2) StartCoroutine(Stage4_EncounterSequence());
    }

    private IEnumerator Stage3_AnomalySequence()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(malfunctionClipName, AudioManager.Instance.sfxMixerGroup);

        yield return new WaitForSeconds(0.8f);
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle("분명 눌렀는데."));
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        yield return new WaitForSeconds(0.5f);

        if (elevatorButton != null) elevatorButton.EnableInteractionWithLight();
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);

        if (doorController != null) { doorController.animationDuration = 3f; doorController.CloseDoors(); }

        yield return new WaitForSeconds(3.5f);

        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
        }

        if (doorController != null) { doorController.animationDuration = 1.5f; doorController.OpenDoors(); }

        yield return new WaitForSeconds(0.15f);

        if (suspiciousMan != null)
        {
            suspiciousMan.TriggerNarrativeAnimation("Attack");
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(doorGrabClipName, AudioManager.Instance.sfxMixerGroup);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartCameraShake(0.5f, 0.2f);
        }

        yield return new WaitForSeconds(1.35f);

        if (suspiciousMan != null)
        {
            yield return StartCoroutine(BoardElevator());
        }

        yield return StartCoroutine(ShowNarrativeLine("하하 죄송합니다. 같이 타도 될까요?", "SND-035_1_같이타도될까요_OneShot"));
        yield return StartCoroutine(ShowNarrativeLine("요즘 좀 무섭잖아요.", "SND-035_2_요즘좀무섭잖아요_OneShot"));
        yield return StartCoroutine(ShowNarrativeLine("4층 사시는구나.", "SND-035_3_4층사시는구나_OneShot"));
        yield return StartCoroutine(ShowNarrativeLine("늦었는데…", "SND-035_4_늦었는데_OneShot"));
        yield return StartCoroutine(ShowNarrativeLine("들어가세요.", "SND-035_5_들어가세요_OneShot"));

        if (doorController != null) doorController.OpenDoors();
        yield return new WaitForSeconds(0.8f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }

    private IEnumerator BoardElevator()
    {
        Transform passenger = suspiciousMan.transform;
        if (suspiciousMan.TryGetComponent(out NavMeshAgent agent)) agent.enabled = false;
        Vector3 start = passenger.position;
        Vector3 destination = start + passengerEntryOffset;
        suspiciousMan.TriggerNarrativeAnimation("Walk");
        float duration = Mathf.Max(0.1f, passengerEntryDuration);
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            passenger.position = Vector3.Lerp(start, destination, elapsed / duration);
            yield return null;
        }
        passenger.position = destination;
        PlayerController player = PlayerController.Instance;
        if (player != null)
        {
            Vector3 towardPlayer = Vector3.ProjectOnPlane(player.transform.position - destination, Vector3.up);
            if (towardPlayer.sqrMagnitude > 0.001f)
                passenger.rotation = Quaternion.LookRotation(towardPlayer);
        }
        suspiciousMan.TriggerNarrativeAnimation("Idle");
    }

    private IEnumerator ShowNarrativeLine(string message, string clipName = null)
    {
        float duration = Mathf.Clamp(message.Length * 0.085f, 2.2f, 5.5f);
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

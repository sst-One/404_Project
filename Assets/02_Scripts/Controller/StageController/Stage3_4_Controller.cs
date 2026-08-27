using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem elevatorButton;
    public ElevatorDoorController doorController;
    public EnemyAI suspiciousMan;

    [Header("Item Audio Reference")]
    public AudioSource elevatorAudioSource;

    [Header("Audio Clip Names")]
    public string disasterAlertClip = "SND-028_Alert_OneShot";
    public string malfunctionClipName = "SND-014_ButtonMalfunction_OneShot";
    public string moveClipName = "SND-004_ElevatorMove_Loop_Timed";
    public string doorGrabClipName = "SND-015_ElevatorDoorGrab_OneShot";
    public string suspectVoiceClipName = "SND-035_SuspectVoice_OneShot_임시";

    private int buttonPressCount = 0;

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
            elevatorButton.interactOnlyOnce = false;
            elevatorButton.isInteractable = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;

            elevatorButton.onInteractAction -= OnElevatorButtonPressed;
            elevatorButton.onInteractAction += OnElevatorButtonPressed;
        }

        PhoneController.Instance.gameObject.SetActive(true);

        StartCoroutine(Day2IntroSequence());
    }

    private void OnDestroy()
    {
        if (elevatorButton != null) elevatorButton.onInteractAction -= OnElevatorButtonPressed;
    }

    private IEnumerator Day2IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(2));
        yield return new WaitForSeconds(1.0f);

        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);

        if (phone != null)
        {
            // [결함 2 픽스] SetParent 수정안 파기. 대신 앞뒤로 SetActive를 명시하여 씬 전환 미노출을 억지로 방어
            phone.gameObject.SetActive(true);
            phone.ShowPhoneInHand();
            phone.gameObject.SetActive(true);

            if (phone.phoneUI != null) phone.phoneUI.ShowMsgDay2();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(5.0f);
            phone.HidePhone();
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

        yield return new WaitForSeconds(1.5f);
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        yield return new WaitForSeconds(3.5f);

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

        yield return new WaitForSeconds(1.0f);

        if (suspiciousMan != null)
        {
            suspiciousMan.TriggerNarrativeAnimation("Idle");
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(suspectVoiceClipName, AudioManager.Instance.voiceMixerGroup);

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }
}
using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    [Header("Narrative Phone (UI 팝업용)")]
    public PhoneUIController narrativePhone;

    [Header("References")]
    public InteractableItem elevatorButton;
    public ElevatorDoorController doorController;
    public GameObject suspiciousMan;

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
            suspiciousMan.SetActive(false);
            EnemyAI ai = suspiciousMan.GetComponent<EnemyAI>();
            if (ai != null) ai.isNarrativeMode = true;
        }

        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;
            elevatorButton.onInteractEvent.RemoveAllListeners();
            elevatorButton.onInteractEvent.AddListener(OnElevatorButtonPressed);
        }

        StartCoroutine(Day2IntroSequence());
    }

    private IEnumerator Day2IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(2));

        yield return new WaitForSeconds(1.0f);

        if (narrativePhone != null)
        {
            narrativePhone.gameObject.SetActive(true);
            narrativePhone.ShowDay2Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(5.0f);
            narrativePhone.gameObject.SetActive(false);
        }

        if (elevatorButton != null)
        {
            elevatorButton.enabled = true;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = true;
        }
    }

    private void OnElevatorButtonPressed()
    {
        buttonPressCount++;
        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
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

        if (elevatorButton != null)
        {
            elevatorButton.enabled = true;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = true;
        }
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);
        if (doorController != null) { doorController.animationDuration = 2.5f; doorController.CloseDoors(); }

        yield return new WaitForSeconds(2.4f);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(doorGrabClipName, AudioManager.Instance.sfxMixerGroup);
        if (suspiciousMan != null) suspiciousMan.SetActive(true);
        yield return new WaitForSeconds(0.5f);

        if (doorController != null) { doorController.animationDuration = 1.5f; doorController.OpenDoors(); }

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
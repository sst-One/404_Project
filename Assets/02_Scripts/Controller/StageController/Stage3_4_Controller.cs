using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;
    private EnemyAI suspiciousMan;
    private int buttonPressCount = 0;

    [Header("사운드 에셋 연결 (AudioSources)")]
    public AudioSource errorAlarmSource;
    public AudioSource doorGrabSource;
    public AudioSource suspectVoiceSource;

    [Header("엘리베이터 이동음 설정")]
    public AudioSource elevatorMoveSource;
    public string elevatorMoveClipName = "SND-004_ElevatorMove_Loop_Timed";

    private void Start()
    {
        doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null) doorController.SetDoorsOpenImmediately();

        suspiciousMan = FindObjectOfType<EnemyAI>(true);
        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(false);
            suspiciousMan.isNarrativeMode = true;
        }

        GameObject btnObj = GameObject.Find("Item_ElevatorButton");
        if (btnObj != null)
        {
            elevatorButton = btnObj.GetComponent<InteractableItem>();
            if (elevatorButton != null)
            {
                elevatorButton.interactOnlyOnce = false;
                elevatorButton.isInteractable = true;
            }
            elevatorButton.onInteractEvent.RemoveAllListeners();
            elevatorButton.onInteractEvent.AddListener(OnElevatorButtonPressed);
        }
    }

    private void OnElevatorButtonPressed()
    {
        buttonPressCount++;
        if (buttonPressCount == 1) StartCoroutine(Stage3_AnomalySequence());
        else if (buttonPressCount == 2) StartCoroutine(Stage4_EncounterSequence());
    }

    private IEnumerator Stage3_AnomalySequence()
    {
        if (elevatorButton != null) elevatorButton.isInteractable = false;

        if (errorAlarmSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-014_ButtonMalfunction_OneShot");
            if (clip != null) errorAlarmSource.PlayOneShot(clip);
        }

        yield return new WaitForSeconds(0.8f);
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        yield return new WaitForSeconds(1.5f);

        if (elevatorButton != null) elevatorButton.isInteractable = true;
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (elevatorButton != null) elevatorButton.isInteractable = false;
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);

        if (doorController != null) doorController.CloseDoors();

        yield return new WaitForSeconds(1.5f);

        if (elevatorMoveSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(elevatorMoveClipName);
            if (clip != null)
            {
                elevatorMoveSource.clip = clip;
                elevatorMoveSource.loop = true;
                elevatorMoveSource.Play();
            }
        }

        yield return new WaitForSeconds(2.0f);

        if (elevatorMoveSource != null && elevatorMoveSource.isPlaying) elevatorMoveSource.Stop();

        if (doorGrabSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-015_ElevatorDoorGrab_OneShot");
            if (clip != null) doorGrabSource.PlayOneShot(clip);
        }

        if (suspiciousMan != null) suspiciousMan.gameObject.SetActive(true);
        if (doorController != null) doorController.OpenDoors();

        if (suspectVoiceSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-035_SuspectVoice_OneShot_임시");
            if (clip != null) suspectVoiceSource.PlayOneShot(clip);
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }
}
using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem elevatorButton;
    public ElevatorDoorController doorController;
    public GameObject suspiciousMan;

    [Header("Audio Sources")]
    public AudioSource errorAlarmSource;
    public AudioSource doorGrabSource;
    public AudioSource suspectVoiceSource;
    public AudioSource elevatorMoveSource;

    [Header("Audio Clip Names")]
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

        GameObject btnObj = GameObject.Find("Item_ElevatorButton");
        if (btnObj != null)
        {
            elevatorButton = btnObj.GetComponent<InteractableItem>();
            if (elevatorButton != null)
            {
                elevatorButton.interactOnlyOnce = false;
                elevatorButton.isInteractable = true;
                elevatorButton.onInteractEvent.RemoveAllListeners();
                elevatorButton.onInteractEvent.AddListener(OnElevatorButtonPressed);
            }
        }
    }

    private void OnElevatorButtonPressed()
    {
        buttonPressCount++;

        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
            if (elevatorButton.GetComponent<Collider>() != null)
                elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (buttonPressCount == 1)
        {
            StartCoroutine(Stage3_AnomalySequence());
        }
        else if (buttonPressCount == 2)
        {
            StartCoroutine(Stage4_EncounterSequence());
        }
    }

    private IEnumerator Stage3_AnomalySequence()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
        }

        if (errorAlarmSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(malfunctionClipName);
            if (clip != null) errorAlarmSource.PlayOneShot(clip);
        }

        yield return new WaitForSeconds(0.8f);

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(1);
        }

        yield return new WaitForSeconds(1.5f);

        if (elevatorButton != null)
        {
            elevatorButton.enabled = true;
            if (elevatorButton.GetComponent<Collider>() != null)
                elevatorButton.GetComponent<Collider>().enabled = true;
        }
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);
        }

        if (doorController != null)
        {
            // [수정점] 문 닫힘 애니메이션 시간 강제 주입
            doorController.animationDuration = 1.4f;
            doorController.CloseDoors();
        }

        yield return new WaitForSeconds(1.4f);

        if (elevatorMoveSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(moveClipName);
            if (clip != null)
            {
                elevatorMoveSource.clip = clip;
                elevatorMoveSource.loop = true;
                elevatorMoveSource.Play();
            }
        }

        yield return new WaitForSeconds(2.0f);

        if (elevatorMoveSource != null && elevatorMoveSource.isPlaying)
        {
            elevatorMoveSource.Stop();
        }

        if (doorGrabSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(doorGrabClipName);
            if (clip != null) doorGrabSource.PlayOneShot(clip);
        }

        if (suspiciousMan != null) suspiciousMan.SetActive(true);
        if (doorController != null)
        {
            doorController.animationDuration = 1.3f;
            doorController.OpenDoors();
        }

        if (suspectVoiceSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(suspectVoiceClipName);
            if (clip != null) suspectVoiceSource.PlayOneShot(clip);
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
        }
    }
}
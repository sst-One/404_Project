using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem elevatorButton;
    public ElevatorDoorController doorController;
    public GameObject suspiciousMan;

    [Header("Item Audio Reference")]
    public AudioSource elevatorAudioSource;

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
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (buttonPressCount == 1) StartCoroutine(Stage3_AnomalySequence());
        else if (buttonPressCount == 2) StartCoroutine(Stage4_EncounterSequence());
    }

    private IEnumerator Stage3_AnomalySequence()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(malfunctionClipName, AudioManager.Instance.sfxMixerGroup);

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

        if (doorController != null)
        {
            doorController.animationDuration = 3f;
            doorController.CloseDoors();
        }

        // 문이 닫히는 총 소요 시간(1.4초) 중 1.3초 대기 (거의 다 닫힐 때)
        yield return new WaitForSeconds(2.5f);

        // 남자 등장 및 쾅 소리
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(doorGrabClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (suspiciousMan != null) suspiciousMan.SetActive(true);

        // 남자 등장 후 0.5초간 가만히 대기
        yield return new WaitForSeconds(0.5f);

        if (doorController != null)
        {
            // 문 닫힘 속도와 동일하게 1.4f 속도로 서서히 열림
            doorController.animationDuration = 3f;
            doorController.OpenDoors();
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(suspectVoiceClipName, AudioManager.Instance.voiceMixerGroup);
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }
}
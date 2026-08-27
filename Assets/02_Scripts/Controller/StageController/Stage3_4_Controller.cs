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
            elevatorButton.interactOnlyOnce = false;
            elevatorButton.isInteractable = false;
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

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDay2Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(5.0f);
            PhoneController.Instance.HidePhone();
        }

        if (elevatorButton != null)
        {
            elevatorButton.isInteractable = true;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = true;
        }
    }

    private void OnElevatorButtonPressed()
    {
        buttonPressCount++;
        if (elevatorButton != null)
        {
            elevatorButton.isInteractable = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;

            // 핫픽스: 버튼을 2번 눌러 이벤트가 완전히 끝났을 때 PulseLight 강제 종료
            if (buttonPressCount == 2)
            {
                PulseLight pulse = elevatorButton.GetComponentInChildren<PulseLight>(true);
                if (pulse != null) pulse.StopPulse();
            }
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
            elevatorButton.isInteractable = true;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = true;
        }
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);

        // 타이밍 조절 안내 1: 아래 animationDuration 2.5f 를 줄이면 문이 더 빠르게 닫힙니다.
        if (doorController != null) { doorController.animationDuration = 3f; doorController.CloseDoors(); }

        // 타이밍 조절 안내 2: 문 닫힘 명령 후 남자가 등장하기 전까지의 대기 시간입니다. (현재 2.0초)
        yield return new WaitForSeconds(2.0f);

        if (suspiciousMan != null)
        {
            suspiciousMan.SetActive(true);
            Animator enemyAnim = suspiciousMan.GetComponentInChildren<Animator>();
            if (enemyAnim != null) enemyAnim.SetTrigger("Attack");
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(doorGrabClipName, AudioManager.Instance.sfxMixerGroup);

        yield return new WaitForSeconds(0.1f);

        // 타이밍 조절 안내 3: 아래 animationDuration 1.5f 를 줄이면 문이 더 빠르게 열립니다.
        if (doorController != null) { doorController.animationDuration = 1.5f; doorController.OpenDoors(); }

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartCameraShake(0.5f, 0.2f);
        }

        if (suspiciousMan != null)
        {
            Animator enemyAnim = suspiciousMan.GetComponentInChildren<Animator>();
            if (enemyAnim != null) enemyAnim.SetTrigger("Idle");
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
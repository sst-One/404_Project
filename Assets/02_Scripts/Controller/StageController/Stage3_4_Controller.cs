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

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDay2Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(5.0f);
            PhoneController.Instance.HidePhone();
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

            // [최적화] PulseLight 수동 소등 코드 삭제 완료 (InteractableItem이 자동 처리함)
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

        // 1. 문 닫기 (닫히는 데 3초 소요)
        if (doorController != null) { doorController.animationDuration = 3f; doorController.CloseDoors(); }

        // [핵심 핫픽스] 문이 완전히 닫힐 때까지 3초 + 텐션 조성 0.5초 대기 (총 3.5초)
        yield return new WaitForSeconds(3.5f);

        // 2. 적 모델링을 켜되 아직 애니메이션은 실행하지 않음 (닫힌 문 뒤에 존재)
        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
        }

        // 3. 문을 열기 시작함
        if (doorController != null) { doorController.animationDuration = 1.5f; doorController.OpenDoors(); }

        // 문이 살짝(0.15초) 열릴 때까지 아주 짧게 대기
        yield return new WaitForSeconds(0.15f);

        // 4. 문이 열리는 찰나에 적의 공격(잡기) 애니메이션 실행 및 사운드/셰이크 동시 발생
        if (suspiciousMan != null)
        {
            suspiciousMan.TriggerNarrativeAnimation("Attack");
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(doorGrabClipName, AudioManager.Instance.sfxMixerGroup);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartCameraShake(0.5f, 0.2f);
        }

        // 5. 공격 애니메이션(잡기)가 끝난 후 자연스럽게 대기 상태로 전환
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
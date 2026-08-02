// Stage3_4_Controller.cs
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

        // 1. 문이 닫히는 시간(1.5초) 대기
        yield return new WaitForSeconds(1.5f);

        // 2. 문 닫힘 직후 엘리베이터 이동음 재생
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

        // 3. 엘리베이터 정상 이동 중 돌발 상황 발생 전 대기 (2.0초)
        yield return new WaitForSeconds(2.0f);

        // 4. 문이 강제로 잡히기 직전 이동음 강제 정지
        if (elevatorMoveSource != null && elevatorMoveSource.isPlaying) elevatorMoveSource.Stop();

        // 5. 남자가 문을 강제로 잡는 소리 재생
        if (doorGrabSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-015_ElevatorDoorGrab_OneShot");
            if (clip != null) doorGrabSource.PlayOneShot(clip);
        }

        if (suspiciousMan != null) suspiciousMan.gameObject.SetActive(true);
        if (doorController != null) doorController.OpenDoors();

        // (주의: 제공된 사운드 목록에 남성 음성이 없어 보이스 코드는 생략 또는 대체 필요)
        if (suspectVoiceSource != null && AudioManager.Instance != null)
        {
            // 남성 음성 에셋이 확정되면 아래 문자열 교체 요망
            AudioClip clip = AudioManager.Instance.GetClip("SND-035_SuspectVoice_OneShot_임시");
            if (clip != null) suspectVoiceSource.PlayOneShot(clip);
        }

        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }
}
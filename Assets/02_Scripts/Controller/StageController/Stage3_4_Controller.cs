using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;
    private EnemyAI suspiciousMan;

    private int buttonPressCount = 0;

    public AudioSource errorAlarmSource;

    private void Start()
    {
        doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null)
        {
            doorController.SetDoorsOpenImmediately();
        }

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

            // [핵심 픽스] 스크립트가 수동으로 상태를 제어하므로 1회용 속성을 강제 해제하고 상호작용을 활성화
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
        if (elevatorButton != null) elevatorButton.isInteractable = false;

        // 에러 알람 사운드 재생
        if (errorAlarmSource != null) errorAlarmSource.Play();

        yield return new WaitForSeconds(0.8f);

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

        yield return new WaitForSeconds(1.5f);

        if (elevatorButton != null) elevatorButton.isInteractable = true;
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        // 2회차 누름: 남자 등장 이벤트가 시작되었으므로 엘리베이터 버튼 영구 잠금
        if (elevatorButton != null) elevatorButton.isInteractable = false;

        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);

        float doorAnimTime = 1.5f; // 기본값

        if (doorController != null)
        {
            doorAnimTime = doorController.animationDuration;
            doorController.CloseDoors();
        }

        float interruptTime = Mathf.Max(0.1f, doorAnimTime - 0.15f);
        yield return new WaitForSeconds(interruptTime);

        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
        }

        Debug.Log("[Stage3_4] 쾅! 문 강제 개방 및 남자 등장.");

        if (doorController != null)
        {
            doorController.OpenDoors();
        }

        Debug.Log("[Stage3_4] 남자 대사 출력 대기...");
        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_1));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_2));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day2_Elevator_3));
        }

        Debug.Log("[Stage3_4] 대사 종료. 404호(Stage 5_6) 씬 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage5_Clue);
    }
}
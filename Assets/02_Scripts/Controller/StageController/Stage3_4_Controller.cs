using System.Collections;
using UnityEngine;

public class Stage3_4_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;
    private EnemyAI suspiciousMan;

    private int buttonPressCount = 0;

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
            elevatorButton.enabled = true;
            if (elevatorButton.GetComponent<Collider>() != null)
                elevatorButton.GetComponent<Collider>().enabled = true;

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
        if (elevatorButton != null) elevatorButton.enabled = false;

        yield return new WaitForSeconds(0.8f);

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

        yield return new WaitForSeconds(1.5f);

        if (elevatorButton != null) elevatorButton.enabled = true;
    }

    private IEnumerator Stage4_EncounterSequence()
    {
        if (elevatorButton != null) elevatorButton.enabled = false;

        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage4_Man);

        float doorAnimTime = 1.5f; // 기본값

        if (doorController != null)
        {
            doorAnimTime = doorController.animationDuration;
            doorController.CloseDoors();
        }

        // 핵심 수정: 문이 완전히 닫히기 직전(0.15초 전)까지 대기하여 시야를 완전히 차단함
        float interruptTime = Mathf.Max(0.1f, doorAnimTime - 0.15f);
        yield return new WaitForSeconds(interruptTime);

        // 시야가 좁아진 틈새 뒤에서 남자 렌더링 활성화 (팝인 방지)
        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
        }

        // EVT-013: 쾅! 문 강제 개방
        Debug.Log("[Stage3_4] 쾅! 문 강제 개방 및 남자 등장.");
        // 추후 이 라인에 오디오 매니저를 통한 SND-015(쾅 소리) 트리거 추가 필요

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
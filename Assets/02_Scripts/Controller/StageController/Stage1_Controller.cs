using System.Collections;
using UnityEngine;

public class Stage1_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;

    private void Start()
    {
        // 씬 시작 시 도어 컨트롤러를 찾아 문을 1층 대기 상태(열림)로 강제 고정
        doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null)
        {
            doorController.SetDoorsOpenImmediately();
        }

        // 버튼 바인딩
        GameObject btnObj = GameObject.Find("Item_ElevatorButton");
        if (btnObj != null)
        {
            elevatorButton = btnObj.GetComponent<InteractableItem>();
            elevatorButton.onInteractEvent.RemoveAllListeners();
            elevatorButton.onInteractEvent.AddListener(OnButtonReached);
            Debug.Log("[Stage1_Controller] 4층 버튼 바인딩 완료.");
        }
    }

    private void OnButtonReached()
    {
        Debug.Log("[Stage1_Controller] 버튼 입력 감지. 이동 시퀀스 시작.");
        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
            if (elevatorButton.GetComponent<Collider>() != null)
                elevatorButton.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(ElevatorTravelSequence());
    }

    private IEnumerator ElevatorTravelSequence()
    {
        // 1. 문 닫힘
        if (doorController != null) doorController.CloseDoors();
        yield return new WaitForSeconds(1.5f);

        // 2. 4층 이동 대기
        Debug.Log("[Stage1_Controller] 4층으로 이동 중 (4초 대기)...");
        yield return new WaitForSeconds(4.0f);

        // 3. 4층 도착 후 문 열림
        Debug.Log("[Stage1_Controller] 4층 도착. 문 열림.");
        if (doorController != null) doorController.OpenDoors();
        yield return new WaitForSeconds(1.5f);

        // 4. 다음 씬(Stage 2) 로드
        Debug.Log("[Stage1_Controller] Stage 1 완료. Stage 2 (404호) 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
    }
}
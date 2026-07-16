using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SequenceTriggerController : MonoBehaviour
{
    private InteractableItem stage1ElevatorButton;
    private InteractableItem stage2TvRemote;
    private InteractableItem stage6FuseBox;
    private InteractableItem stage7Gem;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindTriggersForCurrentScene(scene.name);
    }

    private void BindTriggersForCurrentScene(string sceneName)
    {
        if (sceneName == "02_Elevator")
        {
            if (GameFlowManager.Instance.currentStage == GameStage.Stage1_Elevator)
            {
                GameObject btnObj = GameObject.Find("Item_ElevatorButton");
                if (btnObj != null)
                {
                    stage1ElevatorButton = btnObj.GetComponent<InteractableItem>();
                    stage1ElevatorButton.onInteractEvent.RemoveAllListeners();
                    stage1ElevatorButton.onInteractEvent.AddListener(OnStage1ButtonReached);
                }
            }
        }
        else if (sceneName == "04_Room404")
        {
            // [중요 수정] 스테이지에 따른 적(Enemy) 활성화 통제 로직
            EnemyAI enemy = FindObjectOfType<EnemyAI>(true); // 비활성화된 오브젝트까지 포함하여 탐색
            if (enemy != null)
            {
                bool isChaseStage = (GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                                     GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);
                enemy.gameObject.SetActive(isChaseStage);
                Debug.Log($"[SequenceController] 현재 스테이지({GameFlowManager.Instance.currentStage}). 침입자 활성화 여부: {isChaseStage}");
            }

            GameObject tvObj = GameObject.Find("Item_TvRemote");
            if (tvObj != null)
            {
                stage2TvRemote = tvObj.GetComponent<InteractableItem>();
                stage2TvRemote.onInteractEvent.RemoveAllListeners();
                stage2TvRemote.onInteractEvent.AddListener(OnStage2Completed);

                bool isStage2 = GameFlowManager.Instance.currentStage == GameStage.Stage2_Room;
                stage2TvRemote.enabled = isStage2;
                if (stage2TvRemote.GetComponent<Collider>() != null) stage2TvRemote.GetComponent<Collider>().enabled = isStage2;
            }

            GameObject fuseObj = GameObject.Find("Item_FuseBox");
            if (fuseObj != null)
            {
                stage6FuseBox = fuseObj.GetComponent<InteractableItem>();
                stage6FuseBox.onInteractEvent.RemoveAllListeners();
                stage6FuseBox.onInteractEvent.AddListener(OnStage6Completed);

                bool isStage6 = GameFlowManager.Instance.currentStage == GameStage.Stage6_Blackout;
                stage6FuseBox.enabled = isStage6;
                if (stage6FuseBox.GetComponent<Collider>() != null) stage6FuseBox.GetComponent<Collider>().enabled = isStage6;
            }
        }
        else if (sceneName == "03_Corridor_4F")
        {
            GameObject gemObj = GameObject.Find("Item_Gem_Normal");
            if (gemObj != null)
            {
                stage7Gem = gemObj.GetComponent<InteractableItem>();
                stage7Gem.onInteractEvent.RemoveAllListeners();
                stage7Gem.onInteractEvent.AddListener(OnStage7Completed);

                bool isStage7 = GameFlowManager.Instance.currentStage == GameStage.Stage7_Gem;
                stage7Gem.enabled = isStage7;
                if (stage7Gem.GetComponent<Collider>() != null) stage7Gem.GetComponent<Collider>().enabled = isStage7;
            }
        }
    }

    private void OnStage1ButtonReached()
    {
        if (stage1ElevatorButton != null)
        {
            stage1ElevatorButton.enabled = false;
            if (stage1ElevatorButton.GetComponent<Collider>() != null) stage1ElevatorButton.GetComponent<Collider>().enabled = false;
        }
        StartCoroutine(ElevatorTravelSequence());
    }

    private IEnumerator ElevatorTravelSequence()
    {
        yield return new WaitForSeconds(4.0f);

        ElevatorDoorController doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null) doorController.OpenDoors();

        yield return new WaitForSeconds(1.5f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
    }

    private void OnStage2Completed()
    {
        if (stage2TvRemote != null)
        {
            stage2TvRemote.enabled = false;
            if (stage2TvRemote.GetComponent<Collider>() != null) stage2TvRemote.GetComponent<Collider>().enabled = false;
        }
        StartCoroutine(TvNewsAndSleepSequence());
    }

    // [신규] Stage 2 코어 시퀀스 추가
    private IEnumerator TvNewsAndSleepSequence()
    {
        Debug.Log("[SequenceController] Stage 2 진행 중: TV 켜짐. 뉴스 재생 시작 (약 12초 대기)...");
        // 추후 TV 사운드 및 자막(UI) 활성화 코드가 들어갈 위치
        yield return new WaitForSeconds(12.0f);

        Debug.Log("[SequenceController] Stage 2 진행 중: 뉴스 종료. 수면 페이드 아웃 연출 (약 2초 대기)...");
        // 추후 화면 페이드 아웃(VFX/UI) 활성화 코드가 들어갈 위치
        yield return new WaitForSeconds(2.0f);

        Debug.Log("[SequenceController] Stage 2 완료. Stage 3 (02_Elevator) 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
    }

    private void OnStage6Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
        if (stage6FuseBox != null)
        {
            stage6FuseBox.enabled = false;
            if (stage6FuseBox.GetComponent<Collider>() != null) stage6FuseBox.GetComponent<Collider>().enabled = false;
        }
    }

    private void OnStage7Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
        if (stage7Gem != null)
        {
            stage7Gem.enabled = false;
            if (stage7Gem.GetComponent<Collider>() != null) stage7Gem.GetComponent<Collider>().enabled = false;
        }
    }
}
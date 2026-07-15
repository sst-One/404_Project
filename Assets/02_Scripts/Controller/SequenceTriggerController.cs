using UnityEngine;

public class SequenceTriggerController : MonoBehaviour
{
    [Header("Stage Triggers (InteractableItem)")]
    public InteractableItem stage1ElevatorButton;
    public InteractableItem stage2TvRemote;
    public InteractableItem stage6FuseBox;
    public InteractableItem stage7Gem;

    private void Start()
    {
        InitializeTriggers();
    }

    private void InitializeTriggers()
    {
        if (stage1ElevatorButton != null)
        {
            stage1ElevatorButton.onInteractEvent.AddListener(OnStage1Completed);
        }
        if (stage2TvRemote != null)
        {
            stage2TvRemote.onInteractEvent.AddListener(OnStage2Completed);
            stage2TvRemote.gameObject.SetActive(false); // Stage 1 끝난 후 활성화
        }
        if (stage6FuseBox != null)
        {
            stage6FuseBox.onInteractEvent.AddListener(OnStage6Completed);
            stage6FuseBox.gameObject.SetActive(false);
        }
        if (stage7Gem != null)
        {
            stage7Gem.onInteractEvent.AddListener(OnStage7Completed);
            stage7Gem.gameObject.SetActive(false);
        }

        Debug.Log("[SequenceController] 모든 스테이지 트리거 초기화 완료. Stage 1 엘리베이터 탑승 대기 중.");
    }

    private void OnStage1Completed()
    {
        Debug.Log("[SequenceController] Stage 1 완료: 엘리베이터 이동");
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);

        if (stage1ElevatorButton != null) stage1ElevatorButton.gameObject.SetActive(false);
        if (stage2TvRemote != null) stage2TvRemote.gameObject.SetActive(true);
    }

    private void OnStage2Completed()
    {
        Debug.Log("[SequenceController] Stage 2 완료: TV 뉴스 시청 및 수면");
        // 빠른 프로토타입 시연을 위해 Stage 3,4,5 생략 후 Stage 6 암전으로 점프
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);

        if (stage2TvRemote != null) stage2TvRemote.gameObject.SetActive(false);
        if (stage6FuseBox != null) stage6FuseBox.gameObject.SetActive(true);
    }

    private void OnStage6Completed()
    {
        Debug.Log("[SequenceController] Stage 6 완료: 암전 복구 및 환각 종료");
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);

        if (stage6FuseBox != null) stage6FuseBox.gameObject.SetActive(false);
        if (stage7Gem != null) stage7Gem.gameObject.SetActive(true);
    }

    private void OnStage7Completed()
    {
        Debug.Log("[SequenceController] Stage 7 완료: 보석 획득, 추격 루프 진입");
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);

        if (stage7Gem != null) stage7Gem.gameObject.SetActive(false);

        Debug.Log("[SequenceController] 본격적인 추격 시퀀스가 시작됩니다. 위협(EnemyAI) 활성화 및 은신/신고를 진행하십시오.");
    }
}
using System.Collections;
using UnityEngine;

public class EndingSequenceController : MonoBehaviour
{
    [Header("이벤트 연결 (Stage 11 -> 13)")]
    public PhoneController phoneController;
    public InteractableItem endingGem; // 다음날 아침 발견할 보석 큐브

    private void Start()
    {
        if (phoneController != null)
        {
            phoneController.onCallSuccess.AddListener(OnStage11Completed);
        }

        if (endingGem != null)
        {
            endingGem.onInteractEvent.AddListener(OnStage13Completed);
            endingGem.gameObject.SetActive(false); // 엔딩 전까지 비활성화
        }
    }

    private void OnStage11Completed()
    {
        Debug.Log("[EndingController] Stage 12 진입: 경찰 도착. 모든 위협이 소거됩니다.");
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);

        // 경찰 도착 시 적 AI의 추격 상태를 강제 해제합니다.
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();

        StartCoroutine(PoliceArrivalAndNextDayRoutine());
    }

    private IEnumerator PoliceArrivalAndNextDayRoutine()
    {
        // Stage 12 연출 (사이렌 및 안도감 부여)
        Debug.Log("[EndingController] (사이렌 소리) 경찰 진입 완료. 상황이 종료되었습니다.");
        yield return new WaitForSeconds(4.0f);

        // Day 5 전환 (Stage 13 진입 준비)
        Debug.Log("[EndingController] (암전 페이드아웃) 다음 날 아침, Day 5...");
        yield return new WaitForSeconds(2.0f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);

        Debug.Log("[EndingController] 경찰의 전화: 용의자가 증거 불충분으로 풀려났습니다.");
        Debug.Log("[EndingController] TV 뉴스: 얼굴 없는 연쇄 살인범 석방 소식 보도 중...");
        yield return new WaitForSeconds(3.0f);

        Debug.Log("[EndingController] 바닥에 무언가 떨어지는 소리(땡그랑). 바닥의 보석(endingGem)을 주우십시오.");

        // 씬 내 찌꺼기 방지를 위해 이전 휴대폰 비활성화 후 엔딩 보석 활성화
        if (phoneController != null) phoneController.gameObject.SetActive(false);
        if (endingGem != null) endingGem.gameObject.SetActive(true);
    }

    private void OnStage13Completed()
    {
        Debug.Log("[EndingController] 보석 획득 성공. (Stage 13 반전 연출 시작)");
        StartCoroutine(FlashbackAndCreditRoutine());
    }

    private IEnumerator FlashbackAndCreditRoutine()
    {
        // Heartbeat 최대로 끌어올림 (FEAT-035)
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        Debug.Log("[EndingController] 내가 주운 보석이 내 반지의 빈 구멍과 완벽히 일치합니다.");
        yield return new WaitForSeconds(1.5f);

        Debug.Log("[EndingController] (플래시백 재생) 죽은 아내의 손가락... 시신을 훼손하던 기억들이 중첩됩니다...");
        yield return new WaitForSeconds(4.0f);

        Debug.Log("[EndingController] (비명소리와 함께 암전)");
        yield return new WaitForSeconds(2.0f);

        Debug.Log("[EndingController] [ THE END 404 ] 타이틀 출력. 엔딩 크레딧이 올라갑니다. 수고하셨습니다.");
    }
}
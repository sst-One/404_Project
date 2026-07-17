using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Stage8_11_Controller : MonoBehaviour
{
    private PhoneController phoneController;
    private InteractableItem endingGem;

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
        if (scene.name != "07_Stage8_11" && scene.name != "08_Stage12_13") return;

        GameObject phoneObj = GameObject.Find("Item_Phone");
        if (phoneObj != null)
        {
            phoneController = phoneObj.GetComponent<PhoneController>();
            if (phoneController != null)
            {
                phoneController.onCallSuccess.RemoveAllListeners();
                phoneController.onCallSuccess.AddListener(OnStage11Completed);
            }
        }

        GameObject gemObj = GameObject.Find("Item_EndingGem");
        if (gemObj != null)
        {
            endingGem = gemObj.GetComponent<InteractableItem>();
            if (endingGem != null)
            {
                endingGem.onInteractEvent.RemoveAllListeners();
                endingGem.onInteractEvent.AddListener(OnStage13Completed);

                bool isEndingStage = (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Stage13_Ending);
                endingGem.gameObject.SetActive(isEndingStage);
            }
        }
    }

    private void OnStage11Completed()
    {
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();

        // 씬 전환 전, 07_Stage8_11 내부에서 경찰 도착 오디오 연출을 먼저 실행
        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        Debug.Log("[EndingSequence] 112 신고 성공. 경찰 도착 연출 시작.");

        // 1. Heartbeat 급감 및 긴장 해소 (EVT-053)
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        // 2. 오디오 시퀀스 재생 대기 (총 8초 가량의 딜레이 확보)
        // 추후 AudioManager 연동 부위: 사이렌 페이드 인 시작
        Debug.Log("[EndingSequence] 사이렌 소리 페이드 인...");
        yield return new WaitForSeconds(4.0f);

        // 발소리 멀어짐 및 문 두드림 연출
        Debug.Log("[EndingSequence] 적 발소리 멀어짐, 문 두드림 소리 발생.");
        yield return new WaitForSeconds(4.0f);

        // 3. 연출 종료 후 다음 씬(08_Stage12_13) 로드 요청
        Debug.Log("[EndingSequence] 연출 종료. Stage 12/13 씬 로드 요청.");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
        }

        // 씬 로드 직후 바로 Stage 13(다음날 아침) 연출로 이어짐
        StartCoroutine(NextDayRoutine());
    }

    private IEnumerator NextDayRoutine()
    {
        // Stage 12 상태에서 짧은 여운 대기 후 Stage 13 진입
        yield return new WaitForSeconds(3.0f);

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Stage12_Police)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }

        if (phoneController != null) phoneController.gameObject.SetActive(false);
        if (endingGem != null) endingGem.gameObject.SetActive(true);
    }

    private void OnStage13Completed()
    {
        StartCoroutine(FlashbackAndCreditRoutine());
    }

    private IEnumerator FlashbackAndCreditRoutine()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);
        yield return new WaitForSeconds(4.0f);
        // 크레딧 UI 호출 로직 추가 위치
    }
}
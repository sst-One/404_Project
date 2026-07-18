using System.Collections;
using UnityEngine;

public class Stage7_Controller : MonoBehaviour
{
    private InteractableItem gemItem;
    private EnemyAI suspiciousMan;

    private void Start()
    {
        // 1. NPC 설정 (Narrative Mode 강제 적용으로 추격 차단)
        suspiciousMan = FindObjectOfType<EnemyAI>(true);
        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
            suspiciousMan.isNarrativeMode = true;
        }
        else
        {
            Debug.LogError("[Stage7_Controller] 씬에 Enemy_AI_Unit이 없습니다.");
        }

        // 2. 보석 바인딩 (대사 종료 전까지 스크립트와 콜라이더 강제 잠금)
        GameObject gemObj = GameObject.Find("Item_Gem_Normal");
        if (gemObj != null)
        {
            gemItem = gemObj.GetComponent<InteractableItem>();

            gemItem.enabled = false;
            if (gemItem.GetComponent<Collider>() != null)
            {
                gemItem.GetComponent<Collider>().enabled = false;
            }

            gemItem.onInteractEvent.RemoveAllListeners();
            gemItem.onInteractEvent.AddListener(OnGemReached);
        }
        else
        {
            Debug.LogError("[Stage7_Controller] 씬에 Item_Gem_Normal이 없습니다.");
        }

        // 3. 서사 시퀀스 시작
        StartCoroutine(Stage7_NarrativeSequence());
    }

    private IEnumerator Stage7_NarrativeSequence()
    {
        Debug.Log("[Stage7_Controller] Stage 7 시작. 남자 대사 재생 중...");

        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_1));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_2));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_3));
        }

        if (gemItem != null)
        {
            gemItem.enabled = true;
            if (gemItem.GetComponent<Collider>() != null)
            {
                gemItem.GetComponent<Collider>().enabled = true;
            }
        }
    }

    private void OnGemReached()
    {
        Debug.Log("[Stage7_Controller] 보석(단서) 획득 감지.");

        if (gemItem != null)
        {
            // 보석 획득 연출 (오브젝트 비활성화)
            gemItem.gameObject.SetActive(false);
        }

        StartCoroutine(EndStage7Sequence());
    }

    private IEnumerator EndStage7Sequence()
    {
        // 획득 직후 기괴함으로 인한 불쾌한 감정(Heartbeat 상승) 연출
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(1);
        }

        yield return new WaitForSeconds(2.0f); // 획득 후 여운 대기

        Debug.Log("[Stage7_Controller] Stage 7 종료. Stage 8 (404호 침입 인지) 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
    }
}
using System.Collections;
using UnityEngine;

public class Stage2_Controller : MonoBehaviour
{
    private InteractableItem tvRemote;

    private void Start()
    {
        // 1. 적(Enemy) 상태 오염 방지 - Stage 2는 완전한 일상 상태
        EnemyAI enemy = FindObjectOfType<EnemyAI>(true);
        if (enemy != null)
        {
            enemy.gameObject.SetActive(false);
            Debug.Log("[Stage2_Controller] Stage 2 명세에 따라 적 개체를 강제 비활성화합니다.");
        }

        // 2. TV 리모컨 바인딩
        GameObject tvObj = GameObject.Find("Item_TvRemote");
        if (tvObj != null)
        {
            tvRemote = tvObj.GetComponent<InteractableItem>();
            tvRemote.onInteractEvent.RemoveAllListeners();
            tvRemote.onInteractEvent.AddListener(OnTvRemoteReached);
            Debug.Log("[Stage2_Controller] TV 리모컨 바인딩 완료.");
        }
        else
        {
            Debug.LogError("[Stage2_Controller] Item_TvRemote를 찾을 수 없습니다.");
        }
    }

    private void OnTvRemoteReached()
    {
        Debug.Log("[Stage2_Controller] 리모컨 입력 감지. 뉴스 재생 시퀀스 시작.");

        // 중복 입력 방지 (디자인 에셋 유지를 위해 컴포넌트만 끔)
        if (tvRemote != null)
        {
            tvRemote.enabled = false;
            Collider remoteCollider = tvRemote.GetComponent<Collider>();
            if (remoteCollider != null)
            {
                remoteCollider.enabled = false;
            }
        }

        StartCoroutine(TvNewsAndSleepSequence());
    }

    private IEnumerator TvNewsAndSleepSequence()
    {
        // FEAT-013: 뉴스 재생. 추후 UI/사운드 매니저 호출 코드 삽입 위치.
        Debug.Log("[Stage2_Controller] TV 켜짐. 뉴스 오디오/자막 재생 중 (12초 대기)...");
        yield return new WaitForSeconds(2.0f);// 나중에 12초로 바꾸기

        // 수면 페이드 연출. 추후 VFX 카메라 페이드 아웃 호출 위치.
        Debug.Log("[Stage2_Controller] 뉴스 종료. 수면 페이드 아웃 연출 (2초 대기)...");
        yield return new WaitForSeconds(2.0f);

        // 다음 씬 전환
        Debug.Log("[Stage2_Controller] Stage 2 완료. Stage 3/4 (엘리베이터) 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
    }
}
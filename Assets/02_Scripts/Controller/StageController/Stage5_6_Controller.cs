using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Lighting Settings")]
    [Tooltip("거실을 밝히는 메인 조명(Directional 또는 Point Light)을 연결하십시오.")]
    public Light mainRoomLight;

    [Header("Hallucination Objects")]
    [Tooltip("피와 얼굴 가죽이 포함된 부모 오브젝트를 연결하십시오.")]
    public GameObject hallucinationDecals;

    private InteractableItem fuseBox;
    private bool isBlackout = false;

    private void Start()
    {
        // 1. 초기 상태 세팅: 조명 켜짐, 환각 숨김
        if (mainRoomLight != null) mainRoomLight.enabled = true;
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);

        // 2. 두꺼비집 바인딩 (초기에는 비활성화 상태)
        GameObject fuseObj = GameObject.Find("Item_FuseBox");
        if (fuseObj != null)
        {
            fuseBox = fuseObj.GetComponent<InteractableItem>();

            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null)
            {
                fuseBox.GetComponent<Collider>().enabled = false;
            }

            fuseBox.onInteractEvent.RemoveAllListeners();
            fuseBox.onInteractEvent.AddListener(OnFuseBoxReached);
        }
        else
        {
            Debug.LogError("[Stage5_6_Controller] 씬에서 Item_FuseBox를 찾을 수 없습니다.");
        }

        // 3. Stage 5 시작 (단서 확인 구간)
        StartCoroutine(Stage5_ClueSequence());
    }

    private IEnumerator Stage5_ClueSequence()
    {
        Debug.Log("[Stage5_6] Stage 5 진입. 거실 단서 탐색 대기...");

        // 실내 진입 후 약 8초 대기 후 강제 암전(Stage 6 진입) 발생
        yield return new WaitForSeconds(8.0f);

        Debug.Log("[Stage5_6] Stage 6 진입. 쾅 소리와 함께 암전 발생.");

        if (mainRoomLight != null) mainRoomLight.enabled = false;
        isBlackout = true;

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(2); // 정전으로 인한 급상승
        }

        // 두꺼비집 활성화 (암전 상태에서만 조작 가능)
        if (fuseBox != null)
        {
            fuseBox.enabled = true;
            if (fuseBox.GetComponent<Collider>() != null)
            {
                fuseBox.GetComponent<Collider>().enabled = true;
            }
        }
    }

    private void OnFuseBoxReached()
    {
        if (!isBlackout) return;

        Debug.Log("[Stage5_6] 두꺼비집 조작 감지. 조명 복구 및 환각 시퀀스 시작.");

        if (fuseBox != null)
        {
            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null)
            {
                fuseBox.GetComponent<Collider>().enabled = false;
            }
        }

        StartCoroutine(Stage6_HallucinationSequence());
    }

    private IEnumerator Stage6_HallucinationSequence()
    {
        // 1. 조명 켜짐 (전기 복구)
        if (mainRoomLight != null) mainRoomLight.enabled = true;
        isBlackout = false;

        // 2. 전기 복구 직후 0.5초 뒤 바닥 피/얼굴 가죽 환각 노출
        yield return new WaitForSeconds(0.5f);
        Debug.Log("[Stage5_6] 환각 오브젝트 노출.");
        if (hallucinationDecals != null) hallucinationDecals.SetActive(true);

        // 3. 환각 인지 유지 시간 (1.5초)
        yield return new WaitForSeconds(1.5f);

        // 4. 깜빡임 효과 및 환각 소거
        Debug.Log("[Stage5_6] 찢어지는 비명소리와 함께 재암전(깜빡임).");
        if (mainRoomLight != null) mainRoomLight.enabled = false;

        yield return new WaitForSeconds(0.2f); // 깜빡임 간격

        Debug.Log("[Stage5_6] 환각 소거 및 조명 완전 정상화.");
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);
        if (mainRoomLight != null) mainRoomLight.enabled = true;

        yield return new WaitForSeconds(2.0f); // 여운 대기

        Debug.Log("[Stage5_6] Stage 6 완료. Stage 7 (집 앞 보석 획득) 로드 요청.");
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
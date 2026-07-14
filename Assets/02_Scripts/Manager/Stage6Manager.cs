using System.Collections;
using UnityEngine;

public class Stage6Manager : MonoBehaviour
{
    [Header("조명 설정")]
    public Light mainRoomLight;
    public Light phoneFlashlightProxy;

    [Header("상호작용 대상")]
    public InteractableItem fuseBox;
    public GameObject hallucinationObject;

    [Header("환각 파라미터 (PARAM-038)")]
    public float hallucinationDuration = 2.5f;

    private void Start()
    {
        InitializeBlackout();

        if (fuseBox != null)
        {
            fuseBox.onInteractEvent.AddListener(HandleFuseBoxInteracted);
        }
    }

    private void OnDestroy()
    {
        if (fuseBox != null)
        {
            fuseBox.onInteractEvent.RemoveListener(HandleFuseBoxInteracted);
        }
    }

    private void InitializeBlackout()
    {
        if (mainRoomLight != null) mainRoomLight.enabled = false;
        if (phoneFlashlightProxy != null) phoneFlashlightProxy.enabled = true;
        if (hallucinationObject != null) hallucinationObject.SetActive(false);

        Debug.Log("[Stage6Manager] 암전 발생: 두꺼비집을 찾으십시오.");
    }

    private void HandleFuseBoxInteracted()
    {
        Debug.Log("[Stage6Manager] 두꺼비집 조작 감지: 전기를 복구합니다.");
        StartCoroutine(HallucinationSequence());
    }

    private IEnumerator HallucinationSequence()
    {
        // 1. 전기 복구
        if (mainRoomLight != null) mainRoomLight.enabled = true;
        if (phoneFlashlightProxy != null) phoneFlashlightProxy.enabled = false;

        // 2. 환각 노출 (Threat)
        if (hallucinationObject != null)
        {
            hallucinationObject.SetActive(true);
            Debug.Log("[Stage6Manager] 피/얼굴 환각 발생!");
        }

        // 3. Heartbeat 급상승 (SYS-003)
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddHeartbeat(2); // High 단계로 즉시 상승
        }

        // 4. 유지 시간 대기
        yield return new WaitForSeconds(hallucinationDuration);

        // 5. 환각 종료
        if (hallucinationObject != null)
        {
            hallucinationObject.SetActive(false);
            Debug.Log("[Stage6Manager] 환각 소거 완료. 상황 종료.");
        }
    }
}
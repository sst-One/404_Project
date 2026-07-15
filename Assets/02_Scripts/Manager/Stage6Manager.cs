using System.Collections;
using UnityEngine;

public class Stage6Manager : MonoBehaviour
{
    public static Stage6Manager Instance { get; private set; }

    [Header("Scene References (Room404 전용)")]
    public Light mainLight;
    public Light phoneFlashlight;
    public InteractableItem fuseBoxItem;

    [Header("Parameters (PARAM-038)")]
    public float hallucinationDuration = 2.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 씬 전용이므로 DontDestroyOnLoad를 절대 사용하지 않습니다.
        Debug.Log("[Stage6Manager] Room404 씬 로컬 매니저 활성화 완료.");
    }

    public void TriggerBlackout()
    {
        Debug.Log("[Stage6Manager] 정전 발생! (Stage 6 진입)");
        if (mainLight != null) mainLight.enabled = false;
        if (phoneFlashlight != null) phoneFlashlight.enabled = true;

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);
    }

    public void RestorePower()
    {
        Debug.Log("[Stage6Manager] 두꺼비집 복구 성공. 조명 재가동.");
        if (phoneFlashlight != null) phoneFlashlight.enabled = false;
        if (mainLight != null) mainLight.enabled = true;

        StartCoroutine(HallucinationRoutine());
    }

    private IEnumerator HallucinationRoutine()
    {
        Debug.Log("[Stage6Manager] 환각 노출 연출 시작 (피/얼굴 가죽)");
        yield return new WaitForSeconds(hallucinationDuration);
        Debug.Log("[Stage6Manager] 환각 종료 및 현실 복구");

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
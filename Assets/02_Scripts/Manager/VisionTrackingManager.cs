using UnityEngine;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    [Header("Vision Raw Data (팀원이 플러그인에서 주입할 변수)")]
    public bool isTracking = false;
    public Vector3 currentHeadPosition;
    public Vector3 currentHandPosition;

    [Header("Calibration Data (SCN-001 원점)")]
    public Vector3 baselineHeadPosition;
    public Vector3 baselineHandPosition;

    [Header("Thresholds (판정 임계값)")]
    public float leanDepthThreshold = 0.2f;
    public float reachDepthThreshold = 0.3f;
    public float freezeMotionLimit = 0.08f;

    public bool IsInFallbackMode { get; private set; } = false;
    private float lostTrackingTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!isTracking)
        {
            lostTrackingTimer += Time.deltaTime;
            HandleTrackingLostPolicy();
        }
        else
        {
            lostTrackingTimer = 0f;
            IsInFallbackMode = false;
        }
    }

    private void HandleTrackingLostPolicy()
    {
        if (lostTrackingTimer > 5.0f && !IsInFallbackMode)
        {
            IsInFallbackMode = true;
            Debug.LogWarning("[VisionTrackingManager] 트래킹 5초 이상 상실. 키보드/마우스 Fallback 모드 전환.");
        }
    }

    // --- Core Interaction Output (타 게임 스크립트가 호출할 함수들) ---

    public Vector2 GetGazeScreenPosition()
    {
        if (!isTracking) return new Vector2(Screen.width / 2f, Screen.height / 2f);
        return new Vector2(Screen.width / 2f + (currentHeadPosition.x * 1000f), Screen.height / 2f + (currentHeadPosition.y * 1000f));
    }

    public bool GetReachState()
    {
        if (!isTracking) return false;
        return (currentHandPosition.z - baselineHandPosition.z) > reachDepthThreshold;
    }

    public bool GetLeanState()
    {
        if (!isTracking) return false;
        return (currentHeadPosition.z - baselineHeadPosition.z) > leanDepthThreshold;
    }

    public bool GetOriginFreezeState()
    {
        if (!isTracking) return false;
        float headDiff = Vector3.Distance(currentHeadPosition, baselineHeadPosition);
        float handDiff = Vector3.Distance(currentHandPosition, baselineHandPosition);

        return (headDiff < freezeMotionLimit) && (handDiff < freezeMotionLimit);
    }
}
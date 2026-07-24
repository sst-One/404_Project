using UnityEngine;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    [Header("Vision Raw Data (플러그인에서 업데이트)")]
    public bool isTracking = false;
    public Vector3 currentHeadPosition;
    public Vector3 currentHandPosition;

    [Header("Calibration Data (FEAT-033)")]
    public Vector3 baselineHeadPosition;
    public Vector3 baselineHandPosition;

    [Header("Thresholds (PARAM-006~009)")]
    public float leanDepthThreshold = 0.2f;
    public float reachDepthThreshold = 0.3f;
    public float freezeMotionLimit = 0.08f;

    public bool IsInFallbackMode { get; private set; } = false;

    private float _lostTrackingTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (!isTracking)
        {
            _lostTrackingTimer += Time.deltaTime;
            HandleTrackingLostPolicy();
        }
        else
        {
            _lostTrackingTimer = 0f;
            IsInFallbackMode = false; // 인식이 복구되면 Fallback 해제
        }
    }

    private void HandleTrackingLostPolicy()
    {
        if (_lostTrackingTimer > 5.0f)
        {
            if (!IsInFallbackMode)
            {
                IsInFallbackMode = true;
                Debug.LogWarning("[VisionTrackingManager] 인식 5초 이상 실패. 마우스/키보드 Fallback 시스템 가동.");
            }
        }
        else if (_lostTrackingTimer > 2.0f)
        {
            // 추후 UI-024 최소 안내 렌더링 호출 구간
        }
    }

    public Vector2 GetGazeScreenPosition()
    {
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

        return headDiff < freezeMotionLimit && handDiff < freezeMotionLimit;
    }
}
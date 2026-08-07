using UnityEngine;
using UnityEngine.InputSystem;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    [Header("Vision Raw Data")]
    public bool isTracking = false;
    public Vector3 currentGazePosition;
    public Vector3 currentHeadPosition;
    public Vector3 currentHandPosition;

    [Header("Calibration Data")]
    public Vector3 baselineHeadPosition;
    public Vector3 baselineHandPosition;

    [Header("Thresholds")]
    public float leanDepthThreshold = 0.2f;
    public float reachDepthThreshold = 0.3f;
    public float freezeMotionLimit = 0.08f;

    public bool IsInFallbackMode { get; private set; } = false;

    private float lastDataReceivedTime = 0f;
    private float trackingTimeoutTolerance = 0.5f;

    private readonly object _dataLock = new object();
    private Vector3 _threadGazePos;
    private Vector3 _threadHeadPos;
    private Vector3 _threadHandPos;
    private bool _hasNewFaceData = false;
    private bool _hasNewHandData = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void UpdateFaceData(Vector3 gazePos, Vector3 headPos)
    {
        lock (_dataLock)
        {
            _threadGazePos = gazePos;
            _threadHeadPos = headPos;
            _hasNewFaceData = true;
        }
    }

    public void UpdateHandData(Vector3 pos)
    {
        lock (_dataLock)
        {
            _threadHandPos = pos;
            _hasNewHandData = true;
        }
    }

    private void Update()
    {
        // [핵심 핫픽스] 실시간 영점 조준 (Recalibration)
        // 플레이 도중 자세가 틀어져 화면이 혼자 돌아갈 때 R 키를 누르면 즉시 현재 자세를 중앙으로 재설정합니다.
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RecalibrateOrigin();
        }

        lock (_dataLock)
        {
            if (_hasNewFaceData)
            {
                currentGazePosition = _threadGazePos;
                currentHeadPosition = _threadHeadPos;
                lastDataReceivedTime = Time.time;
                isTracking = true;
                IsInFallbackMode = false;
                _hasNewFaceData = false;
            }

            if (_hasNewHandData)
            {
                currentHandPosition = _threadHandPos;
                if (baselineHandPosition == Vector3.zero)
                {
                    baselineHandPosition = currentHandPosition;
                }
                _hasNewHandData = false;
            }
        }

        if (isTracking && (Time.time - lastDataReceivedTime > trackingTimeoutTolerance))
        {
            isTracking = false;
            IsInFallbackMode = true;
        }
    }

    // 외부 스크립트(UI 등)에서도 호출할 수 있도록 public 개방
    public void RecalibrateOrigin()
    {
        if (!isTracking) return;

        baselineHeadPosition = currentHeadPosition;
        if (currentHandPosition != Vector3.zero)
        {
            baselineHandPosition = currentHandPosition;
        }

        Debug.Log("[VisionTrackingManager] 실시간 영점(Calibration) 재설정 완료. 현재 자세를 정중앙으로 인식합니다.");
    }

    public Vector2 GetGazeScreenPosition()
    {
        if (!isTracking) return new Vector2(Screen.width / 2f, Screen.height / 2f);
        return new Vector2(Screen.width / 2f + (currentGazePosition.x * 1000f), Screen.height / 2f + (currentGazePosition.y * 1000f));
    }

    public bool GetReachState()
    {
        if (!isTracking || baselineHandPosition == Vector3.zero) return false;
        return Mathf.Abs(currentHandPosition.z - baselineHandPosition.z) > reachDepthThreshold;
    }

    public bool GetLeanState()
    {
        if (!isTracking || baselineHeadPosition == Vector3.zero) return false;
        return Mathf.Abs(currentHeadPosition.z - baselineHeadPosition.z) > leanDepthThreshold;
    }

    public bool GetOriginFreezeState()
    {
        if (!isTracking || baselineHeadPosition == Vector3.zero) return false;
        float headDiff = Vector3.Distance(currentHeadPosition, baselineHeadPosition);
        return headDiff < freezeMotionLimit;
    }
}
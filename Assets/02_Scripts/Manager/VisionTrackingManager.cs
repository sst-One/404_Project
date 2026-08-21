using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    [Header("Vision Raw Data")]
    public bool isTracking = false;
    public Vector3 currentGazePosition;
    public Vector3 currentHeadPosition;
    public Vector3 currentHeadRotation;
    public Vector3 currentHandPosition;

    [Header("Calibration Data")]
    public Vector3 baselineHeadPosition;
    public Vector3 baselineHeadRotation;
    public Vector3 baselineHandPosition;
    public float calibrationDuration = 3.0f;
    public bool IsCalibrated { get; private set; } = false;

    [Header("Thresholds (데드존 확립)")]
    [Tooltip("이동: 이 수치보다 앞으로 숙여야 발동 (기본 0.2)")]
    public float leanDepthThreshold = 3f;
    [Tooltip("상호작용: 이 수치보다 손을 뻗어야 발동 (기본 0.3)")]
    public float reachDepthThreshold = 0.3f;
    [Tooltip("호흡참기: 이 수치보다 뒤로 확실히 젖혀야 발동 (관성 오작동 방지를 위해 -0.3 셋팅)")]
    public float backwardLeanThreshold = -3f;

    public bool IsInFallbackMode { get; private set; } = false;

    private float lastDataReceivedTime = 0f;
    private float trackingTimeoutTolerance = 0.5f;

    private readonly object _dataLock = new object();
    private Vector3 _threadGazePos;
    private Vector3 _threadHeadPos;
    private Vector3 _threadHeadRot;
    private Vector3 _threadHandPos;
    private bool _hasNewFaceData = false;
    private bool _hasNewHandData = false;
    private bool _isInitialBaselineSet = false;

    public event Action OnCalibrationSuccess;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // PlayerPrefs 호출 시 기본값도 -0.3f로 하드코딩
        leanDepthThreshold = PlayerPrefs.GetFloat("LeanThreshold", 1.5f);
        reachDepthThreshold = PlayerPrefs.GetFloat("ReachThreshold", 0.3f);
        backwardLeanThreshold = PlayerPrefs.GetFloat("BackwardLeanThreshold", -1.5f);
    }

    public void UpdateFaceData(Vector3 gazePos, Vector3 headPos, Vector3 headRot)
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title) return;

        lock (_dataLock)
        {
            _threadGazePos = gazePos;
            _threadHeadPos = headPos;
            _threadHeadRot = headRot;
            _hasNewFaceData = true;
        }
    }

    public void UpdateHandData(Vector3 pos)
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title) return;

        lock (_dataLock)
        {
            _threadHandPos = pos;
            _hasNewHandData = true;
        }
    }

    private void Update()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            isTracking = false;
            return;
        }

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
                currentHeadRotation = _threadHeadRot;
                lastDataReceivedTime = Time.time;
                isTracking = true;
                IsInFallbackMode = false;
                _hasNewFaceData = false;

                if (!_isInitialBaselineSet)
                {
                    RecalibrateOrigin();
                    _isInitialBaselineSet = true;
                }
            }

            if (_hasNewHandData)
            {
                currentHandPosition = _threadHandPos;
                if (baselineHandPosition == Vector3.zero) baselineHandPosition = currentHandPosition;
                _hasNewHandData = false;
            }
        }

        if (isTracking && (Time.time - lastDataReceivedTime > trackingTimeoutTolerance))
        {
            isTracking = false;
            IsInFallbackMode = true;
        }
    }

    public void StartCalibration()
    {
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        IsCalibrated = false;
        yield return new WaitForSeconds(calibrationDuration);

        RecalibrateOrigin();
        IsCalibrated = true;
        OnCalibrationSuccess?.Invoke();
    }

    public void RecalibrateOrigin()
    {
        if (!isTracking) return;
        baselineHeadPosition = currentHeadPosition;
        baselineHeadRotation = currentHeadRotation;
        if (currentHandPosition != Vector3.zero) baselineHandPosition = currentHandPosition;
    }

    public bool GetReachState()
    {
        if (!isTracking || baselineHandPosition == Vector3.zero) return false;
        return (currentHandPosition.z - baselineHandPosition.z) > reachDepthThreshold;
    }

    public bool GetLeanState()
    {
        if (!isTracking || baselineHeadPosition == Vector3.zero) return false;
        return (currentHeadPosition.z - baselineHeadPosition.z) > leanDepthThreshold;
    }

    public bool GetOriginFreezeState()
    {
        if (!isTracking || baselineHeadPosition == Vector3.zero) return false;
        // z값이 -0.3f 이하로 내려가야 호흡참기 발동
        return (currentHeadPosition.z - baselineHeadPosition.z) < backwardLeanThreshold;
    }
}
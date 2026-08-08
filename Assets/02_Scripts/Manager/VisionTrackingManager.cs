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

    [Header("Thresholds")]
    public float leanDepthThreshold = 0.2f;
    public float reachDepthThreshold = 0.3f;
    public float backwardLeanThreshold = -0.15f;

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

        leanDepthThreshold = PlayerPrefs.GetFloat("LeanThreshold", 0.2f);
        reachDepthThreshold = PlayerPrefs.GetFloat("ReachThreshold", 0.3f);
        backwardLeanThreshold = PlayerPrefs.GetFloat("BackwardLeanThreshold", -0.15f);
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

    // --- [병합된 CalibrationManager 로직] ---
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
    // --------------------------------------

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
        return (currentHeadPosition.z - baselineHeadPosition.z) < backwardLeanThreshold;
    }
}
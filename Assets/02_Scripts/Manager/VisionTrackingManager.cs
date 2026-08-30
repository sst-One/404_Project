using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    public string SelectedDeviceName { get; private set; }
    public bool IsCameraReady { get; private set; } = false;
    public event Action<string> OnCameraConfirmed;

    [HideInInspector] public bool allowCameraActivation = false;

    [Header("Vision Public Data (Gameplay)")]
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
    public float leanDepthThreshold = 3f;
    public float reachDepthThreshold = 0.3f;
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

    private Vector3 _rawGazePos;
    private Vector3 _rawHeadPos;
    private Vector3 _rawHeadRot;
    private Vector3 _rawHandPos;

    public event Action OnCalibrationSuccess;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        leanDepthThreshold = PlayerPrefs.GetFloat("LeanThreshold", 1.5f);
        reachDepthThreshold = PlayerPrefs.GetFloat("ReachThreshold", 0.3f);
        backwardLeanThreshold = PlayerPrefs.GetFloat("BackwardLeanThreshold", -1.5f);

        IsCalibrated = PlayerPrefs.GetInt("IsCalibrated", 0) == 1;
        allowCameraActivation = false;
    }

    public void ReceiveCameraSelection(string deviceName)
    {
        SelectedDeviceName = deviceName;

        if (!allowCameraActivation)
        {
            Debug.Log("[VisionTrackingManager] 타이틀 대기 상태이므로 설정값만 저장하고 카메라는 켜지 않습니다.");
            return;
        }

        IsCameraReady = true;
        OnCameraConfirmed?.Invoke(SelectedDeviceName);
    }

    public void UpdateFaceData(Vector3 gazePos, Vector3 headPos, Vector3 headRot)
    {
        if (!IsCameraReady) return;
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
        if (!IsCameraReady) return;
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title) return;

        lock (_dataLock)
        {
            _threadHandPos = pos;
            _hasNewHandData = true;
        }
    }

    private void Update()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameStage.Title)
        {
            allowCameraActivation = true;
        }

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
                _rawGazePos = _threadGazePos;
                _rawHeadPos = _threadHeadPos;
                _rawHeadRot = _threadHeadRot;

                if (IsCalibrated)
                {
                    currentGazePosition = _rawGazePos;
                    currentHeadPosition = _rawHeadPos;
                    currentHeadRotation = _rawHeadRot;
                }

                lastDataReceivedTime = Time.time;
                isTracking = true;
                IsInFallbackMode = false;
                _hasNewFaceData = false;
            }

            if (_hasNewHandData)
            {
                _rawHandPos = _threadHandPos;

                if (IsCalibrated)
                {
                    currentHandPosition = _rawHandPos;

                    // [결함 픽스] 상호작용 복구 로직: 손이 화면에 처음 들어온 순간을 영점으로 자동 할당
                    if (baselineHandPosition == Vector3.zero && currentHandPosition != Vector3.zero)
                    {
                        baselineHandPosition = currentHandPosition;
                    }
                }
                _hasNewHandData = false;
            }
        }

        if (isTracking && (Time.time - lastDataReceivedTime > trackingTimeoutTolerance))
        {
            isTracking = false;
            IsInFallbackMode = true;
            baselineHandPosition = Vector3.zero; // 손 추적이 끊기면 기준점 리셋 (다시 손을 들 때 새 기준점 확보)
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
        OnCalibrationSuccess?.Invoke();
    }

    public void RecalibrateOrigin()
    {
        baselineHeadPosition = _rawHeadPos;
        baselineHeadRotation = _rawHeadRot;

        // [결함 픽스] 캘리브레이션 시 손 영점을 0으로 초기화하여, 이후 손을 뻗을 때 정상적으로 새 기준점을 잡도록 유도
        baselineHandPosition = Vector3.zero;
        if (_rawHandPos != Vector3.zero) baselineHandPosition = _rawHandPos;

        IsCalibrated = true;

        PlayerPrefs.SetInt("IsCalibrated", 1);
        PlayerPrefs.Save();
    }

    public bool GetReachState()
    {
        if (!isTracking || !IsCalibrated || baselineHandPosition == Vector3.zero) return false;
        return (currentHandPosition.z - baselineHandPosition.z) > reachDepthThreshold;
    }

    public bool GetLeanState()
    {
        if (!isTracking || !IsCalibrated || baselineHeadPosition == Vector3.zero) return false;
        return (currentHeadPosition.z - baselineHeadPosition.z) > leanDepthThreshold;
    }

    public bool GetOriginFreezeState()
    {
        if (!isTracking || !IsCalibrated || baselineHeadPosition == Vector3.zero) return false;
        return (currentHeadPosition.z - baselineHeadPosition.z) < backwardLeanThreshold;
    }
}
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;
// [핵심 픽스] 플러그인 컴파일 충돌을 막기 위해 Mediapipe 네임스페이스를 완전히 삭제했습니다.

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
        Debug.Log("[VisionTrackingManager] 카메라 변경 요청 수신: " + deviceName);

        // [핵심 픽스] 타입 제약 없이 씬에 존재하는 "WebCamSource" 스크립트를 블라인드 탐색합니다.
        MonoBehaviour webCamSource = null;
        MonoBehaviour[] allScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script != null && script.GetType().Name == "WebCamSource")
            {
                webCamSource = script;
                break;
            }
        }

        if (webCamSource != null)
        {
            try
            {
                // 1. 기존에 잘못된 이름(OBS 등)으로 돌고 있는 코루틴 멱살 잡고 강제 중지
                var isPlayingProp = webCamSource.GetType().GetProperty("isPlaying");
                if (isPlayingProp != null && (bool)isPlayingProp.GetValue(webCamSource))
                {
                    var stopMethod = webCamSource.GetType().GetMethod("Stop");
                    stopMethod?.Invoke(webCamSource, null);
                    Debug.Log("[VisionTrackingManager] 기존 카메라 코루틴 강제 중단 완료.");
                }

                // 2. 새로운 기기 이름 주입 (변수 이름 오차까지 완벽 방어)
                var deviceNameProp = webCamSource.GetType().GetProperty("deviceName");
                if (deviceNameProp != null && deviceNameProp.CanWrite)
                {
                    deviceNameProp.SetValue(webCamSource, SelectedDeviceName);
                }
                else
                {
                    var field = webCamSource.GetType().GetField("m_DeviceName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    if (field != null)
                    {
                        field.SetValue(webCamSource, SelectedDeviceName);
                    }
                }
                Debug.Log("[VisionTrackingManager] WebCamSource 기기 이름 갱신 완료: " + SelectedDeviceName);

                // 3. 새 카메라 재가동
                if (allowCameraActivation)
                {
                    var playMethod = webCamSource.GetType().GetMethod("Play");
                    if (playMethod != null)
                    {
                        IEnumerator playCoroutine = playMethod.Invoke(webCamSource, null) as IEnumerator;
                        if (playCoroutine != null)
                        {
                            StartCoroutine(playCoroutine);
                            Debug.Log("[VisionTrackingManager] 새 하드웨어로 카메라 렌즈 가동 성공.");
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[VisionTrackingManager] WebCamSource 리플렉션 제어 중 예외 발생: " + ex.Message);
            }
        }
        else
        {
            Debug.LogWarning("[VisionTrackingManager] 씬에서 WebCamSource 컴포넌트를 찾을 수 없습니다. (에러 아님, 튜토리얼 씬 대기)");
        }

        if (!allowCameraActivation)
        {
            Debug.Log("[VisionTrackingManager] 타이틀 대기 상태이므로 카메라는 켜지 않습니다.");
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
            baselineHandPosition = Vector3.zero;
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
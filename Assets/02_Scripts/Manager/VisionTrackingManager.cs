using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Collections;

public class VisionTrackingManager : MonoBehaviour
{
    public static VisionTrackingManager Instance { get; private set; }

    public string SelectedDeviceName { get; private set; }
    public bool IsCameraReady { get; private set; }
    public event Action<string> OnCameraConfirmed;

    [HideInInspector] public bool allowCameraActivation;

    [Header("Vision Public Data (Gameplay)")]
    public bool isTracking;
    public Vector3 currentGazePosition;
    public Vector3 currentHeadPosition;
    public Vector3 currentHeadRotation;
    public Vector3 currentHandPosition;

    [Tooltip("손바닥 랜드마크의 화면상 크기. 디버깅용입니다.")]
    public float currentHandScale;

    [Tooltip("손바닥의 중립 자세 기준 크기. 디버깅용입니다.")]
    public float baselineHandScale;

    [Header("Calibration Data")]
    public Vector3 baselineHeadPosition;
    public Vector3 baselineHeadRotation;
    public Vector3 baselineHandPosition;
    public float calibrationDuration = 3f;
    public bool IsCalibrated { get; private set; }

    [Header("Movement / Interaction Thresholds")]
    [Tooltip("앞뒤 기울임 판정 기준. 얼굴 크기 기반 깊이 대용값의 변화량입니다.")]
    public float leanDepthThreshold = 0.15f;

    [Tooltip("구버전 호환용 값입니다. 좌우 이동은 현재 머리의 좌우 기울기 각도로 판정합니다.")]
    public float lateralLeanThreshold = 0.15f;

    [Tooltip("좌우 이동을 발생시키는 머리 기울기 각도(도). 시작값 8도 권장.")]
    public float lateralLeanAngleThreshold = 8f;

    [Tooltip("고개를 크게 좌우로 돌리는 중에는 좌우 이동을 막는 yaw 각도(도).")]
    public float maxHeadYawForLateralMovement = 25f;

    [Tooltip("손을 앞으로 뻗었다고 판단하는 손바닥 크기 증가 비율. 1.2는 기준보다 20% 커졌을 때입니다.")]
    public float reachScaleRatioThreshold = 1.2f;

    [Tooltip("손 크기 증가량의 최소값. 기존 조작 민감도 슬라이더와 연결됩니다.")]
    public float reachDepthThreshold = 0.2f;

    public float freezeTolerance = 0.08f;

    [Header("Hand Over Mouth Detection")]
    [Tooltip("손바닥 중심과 코 랜드마크 사이의 최대 2D 거리. 좌표 스케일은 MediaPipeIntegrationBridge의 positionMultiplier를 따릅니다.")]
    public float handMouthDistanceThreshold = 1.5f;

    [Tooltip("손과 코 사이의 X축 허용 거리.")]
    public float handMouthHorizontalTolerance = 1.0f;

    [Tooltip("손바닥 중심이 코보다 아래로 얼마나 떨어질 수 있는지 허용하는 값(좌표 단위).")]
    public float handMouthVerticalBelowTolerance = 1.3f;

    [Tooltip("손바닥 중심이 코보다 위로 얼마나 올라갈 수 있는지 허용하는 값(좌표 단위).")]
    public float handMouthVerticalAboveTolerance = 0.8f;

    [Tooltip("손 추적 데이터가 이 시간보다 오래되면 손이 없는 것으로 처리합니다.")]
    public float handTrackingTimeout = 0.35f;

    [Tooltip("좌우 이동이 반대로 느껴지면 켜세요.")]
    public bool invertLateralMovement;

    [Header("Tracking Safety")]
    public float trackingTimeoutTolerance = 0.5f;
    public bool showTrackingDebugLogs;

    public bool IsInFallbackMode { get; private set; }

    private float _lastFaceDataReceivedTime = -100f;
    private float _lastHandDataReceivedTime = -100f;
    private bool _hasReceivedFaceData;
    private bool _hasReceivedHandData;
    private bool _hasValidHeadBaseline;
    private bool _hasValidHandBaseline;
    private float _rawHandScale;
    private float _threadHandScale;
    private float _baselineHandScale;

    private readonly object _dataLock = new object();

    private Vector3 _threadGazePos;
    private Vector3 _threadHeadPos;
    private Vector3 _threadHeadRot;
    private Vector3 _threadHandPos;

    private bool _hasNewFaceData;
    private bool _hasNewHandData;

    private Vector3 _rawGazePos;
    private Vector3 _rawHeadPos;
    private Vector3 _rawHeadRot;
    private Vector3 _rawHandPos;

    private Coroutine _calibrationRoutine;

    public event Action OnCalibrationSuccess;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        leanDepthThreshold = PlayerPrefs.GetFloat("LeanThreshold", leanDepthThreshold);
        reachDepthThreshold = PlayerPrefs.GetFloat("ReachThreshold", reachDepthThreshold);
        freezeTolerance = PlayerPrefs.GetFloat("FreezeTolerance", freezeTolerance);

        IsCalibrated = false;
        baselineHeadPosition = Vector3.zero;
        baselineHeadRotation = Vector3.zero;
        baselineHandPosition = Vector3.zero;
        baselineHandScale = 0f;
        currentHandScale = 0f;
        _baselineHandScale = 0f;
        _hasValidHeadBaseline = false;
        _hasValidHandBaseline = false;
        allowCameraActivation = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void ReceiveCameraSelection(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            Debug.LogWarning("[VisionTrackingManager] 카메라 이름이 비어 있어 선택을 적용하지 않았습니다.");
            return;
        }

        bool changedDevice = !string.Equals(
            SelectedDeviceName,
            deviceName,
            StringComparison.Ordinal
        );

        SelectedDeviceName = deviceName;
        IsCameraReady = true;

        if (changedDevice)
        {
            IsCalibrated = false;
            _hasValidHeadBaseline = false;
            _hasValidHandBaseline = false;
            baselineHeadPosition = Vector3.zero;
            baselineHandPosition = Vector3.zero;
            baselineHandScale = 0f;
            _baselineHandScale = 0f;
        }

        OnCameraConfirmed?.Invoke(SelectedDeviceName);
    }

    public void UpdateFaceData(Vector3 gazePos, Vector3 headPos, Vector3 headRot)
    {
        if (!IsCameraReady)
            return;

        lock (_dataLock)
        {
            _threadGazePos = gazePos;
            _threadHeadPos = headPos;
            _threadHeadRot = headRot;
            _hasNewFaceData = true;
        }
    }

    // 기존 호출부 호환용 오버로드
    public void UpdateHandData(Vector3 pos)
    {
        UpdateHandData(pos, 0f);
    }

    // apparentScale은 손바닥의 화면상 크기이며,
    // 손을 카메라 쪽으로 뻗을 때 커지는 정도를 이용합니다.
    public void UpdateHandData(Vector3 pos, float apparentScale)
    {
        if (!IsCameraReady)
            return;

        lock (_dataLock)
        {
            _threadHandPos = pos;
            _threadHandScale = apparentScale;
            _hasNewHandData = true;
        }
    }

    private void Update()
    {
        allowCameraActivation = true;

        if (Keyboard.current != null &&
            Keyboard.current.rKey.wasPressedThisFrame)
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
                _hasReceivedFaceData = true;
                _lastFaceDataReceivedTime = Time.time;

                if (IsCalibrated)
                {
                    currentGazePosition = _rawGazePos;
                    currentHeadPosition = _rawHeadPos;
                    currentHeadRotation = _rawHeadRot;
                }

                _hasNewFaceData = false;
            }

            if (_hasNewHandData)
            {
                _rawHandPos = _threadHandPos;
                _rawHandScale = _threadHandScale;
                _hasReceivedHandData = true;
                _lastHandDataReceivedTime = Time.time;

                if (IsCalibrated)
                {
                    currentHandPosition = _rawHandPos;
                    currentHandScale = _rawHandScale;

                    // 캘리브레이션 중 손이 보이지 않았다면 첫 손 프레임을 중립 기준으로 삼습니다.
                    if (!_hasValidHandBaseline && _rawHandScale > 0.001f)
                    {
                        baselineHandPosition = _rawHandPos;
                        _baselineHandScale = _rawHandScale;
                        baselineHandScale = _baselineHandScale;
                        _hasValidHandBaseline = true;

                        if (showTrackingDebugLogs)
                        {
                            Debug.Log($"[VisionTrackingManager] 손 기준 크기 자동 설정: {_baselineHandScale:F3}");
                        }
                    }
                }

                _hasNewHandData = false;
            }
        }

        bool faceFresh = HasFreshFaceData();
        isTracking = faceFresh;
        IsInFallbackMode = !faceFresh;
    }

    private bool HasFreshFaceData()
    {
        return _hasReceivedFaceData &&
               Time.time - _lastFaceDataReceivedTime <= trackingTimeoutTolerance;
    }

    private bool HasFreshHandData()
    {
        return _hasReceivedHandData &&
               Time.time - _lastHandDataReceivedTime <= handTrackingTimeout;
    }

    public void StartCalibration()
    {
        if (_calibrationRoutine != null)
            StopCoroutine(_calibrationRoutine);

        _calibrationRoutine = StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        IsCalibrated = false;
        _hasValidHeadBaseline = false;
        _hasValidHandBaseline = false;
        _baselineHandScale = 0f;
        baselineHandScale = 0f;

        float waitElapsed = 0f;

        while (!HasFreshFaceData() && waitElapsed < 10f)
        {
            waitElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!HasFreshFaceData())
        {
            Debug.LogError("[VisionTrackingManager] 캘리브레이션 실패: 얼굴 추적 데이터가 없습니다. 카메라와 FaceLandmarkerRunner를 확인하세요.");
            _calibrationRoutine = null;
            yield break;
        }

        float calibrationElapsed = 0f;

        while (calibrationElapsed < calibrationDuration)
        {
            if (!HasFreshFaceData())
            {
                Debug.LogWarning("[VisionTrackingManager] 캘리브레이션 중 얼굴 추적이 끊겼습니다. 다시 시도합니다.");
                calibrationElapsed = 0f;
            }
            else
            {
                calibrationElapsed += Time.unscaledDeltaTime;
            }

            yield return null;
        }

        if (RecalibrateOrigin())
        {
            OnCalibrationSuccess?.Invoke();
        }

        _calibrationRoutine = null;
    }

    public bool RecalibrateOrigin()
    {
        if (!HasFreshFaceData())
        {
            IsCalibrated = false;
            _hasValidHeadBaseline = false;
            Debug.LogWarning("[VisionTrackingManager] 기준 자세를 저장할 수 없습니다. 얼굴 추적이 안정된 뒤 다시 시도하세요.");
            return false;
        }

        baselineHeadPosition = _rawHeadPos;
        baselineHeadRotation = _rawHeadRot;
        _hasValidHeadBaseline = true;

        currentGazePosition = _rawGazePos;
        currentHeadPosition = _rawHeadPos;
        currentHeadRotation = _rawHeadRot;

        _hasValidHandBaseline =
            HasFreshHandData() &&
            _rawHandPos != Vector3.zero &&
            _rawHandScale > 0.001f;

        baselineHandPosition = _hasValidHandBaseline
            ? _rawHandPos
            : Vector3.zero;

        _baselineHandScale = _hasValidHandBaseline
            ? _rawHandScale
            : 0f;

        baselineHandScale = _baselineHandScale;

        if (_hasValidHandBaseline)
        {
            currentHandPosition = _rawHandPos;
            currentHandScale = _rawHandScale;
        }

        IsCalibrated = true;

        if (showTrackingDebugLogs)
        {
            Debug.Log($"[VisionTrackingManager] 캘리브레이션 완료. Head baseline={baselineHeadPosition}, Hand baseline valid={_hasValidHandBaseline}");
        }

        return true;
    }

    // Reach는 손을 앞으로 뻗는 상호작용 입력입니다.
    public bool GetReachState()
    {
        if (!isTracking || !IsCalibrated || !HasFreshHandData())
            return false;

        if (currentHandScale <= 0.001f)
            return false;

        if (!_hasValidHandBaseline || _baselineHandScale <= 0.001f)
            return false;

        float scaleIncrease = currentHandScale - _baselineHandScale;
        float scaleRatio = currentHandScale / _baselineHandScale;

        return scaleIncrease >= Mathf.Max(0.05f, reachDepthThreshold) ||
               scaleRatio >= reachScaleRatioThreshold;
    }

    // X: 좌우 이동(머리 roll/옆 기울임), Y: 앞뒤 이동(얼굴 크기 변화 기반 깊이 대용값).
    // 머리 yaw(좌우 회전)와 코의 화면상 X 이동을 좌우 이동 입력으로 사용하지 않습니다.
    public Vector2 GetLeanMovementInput()
    {
        if (!isTracking || !IsCalibrated || !_hasValidHeadBaseline)
            return Vector2.zero;

        float deltaDepth = currentHeadPosition.z - baselineHeadPosition.z;
        float deltaRoll = Mathf.DeltaAngle(
            baselineHeadRotation.z,
            currentHeadRotation.z
        );
        float deltaYaw = Mathf.DeltaAngle(
            baselineHeadRotation.y,
            currentHeadRotation.y
        );

        float horizontal = 0f;

        if (Mathf.Abs(deltaYaw) <= maxHeadYawForLateralMovement &&
            Mathf.Abs(deltaRoll) >= lateralLeanAngleThreshold)
        {
            horizontal = Mathf.Sign(deltaRoll);
        }

        horizontal *= -1f;

        if (invertLateralMovement)
            horizontal *= -1f;

        float forward = Mathf.Abs(deltaDepth) >= leanDepthThreshold
            ? Mathf.Sign(deltaDepth)
            : 0f;

        Vector2 input = new Vector2(horizontal, forward);

        return input.sqrMagnitude > 1f
            ? input.normalized
            : input;
    }

    public bool GetLeanState()
    {
        return GetLeanMovementInput().sqrMagnitude > 0.01f;
    }

    // 숨 참기는 손이 입 주변에 있을 때만 활성화합니다.
    // 상체를 뒤로 젖혀 얼굴 크기 기반 깊이 값이 감소하면 숨 참기를 차단합니다.
    public bool GetHandOverMouthState()
    {
        if (!isTracking ||
            !IsCalibrated ||
            !_hasValidHeadBaseline ||
            !HasFreshHandData())
        {
            return false;
        }

        if (currentHeadPosition == Vector3.zero ||
            currentHandPosition == Vector3.zero)
        {
            return false;
        }

        // 추가된 부분: 뒤로 젖히는 동작에서 숨 참기가 오작동하지 않도록 차단
        float depthDeltaFromBaseline =
            currentHeadPosition.z - baselineHeadPosition.z;

        float backwardLeanGuard =
            Mathf.Max(0.05f, leanDepthThreshold * 0.75f);

        if (depthDeltaFromBaseline < -backwardLeanGuard)
        {
            if (showTrackingDebugLogs)
            {
                Debug.Log(
                    $"[VisionTrackingManager] 숨 참기 차단: 상체 뒤로 젖힘 감지 " +
                    $"(depth delta={depthDeltaFromBaseline:F3}, 기준={-backwardLeanGuard:F3})"
                );
            }

            return false;
        }

        Vector2 facePoint = new Vector2(
            currentHeadPosition.x,
            currentHeadPosition.y
        );

        Vector2 handPoint = new Vector2(
            currentHandPosition.x,
            currentHandPosition.y
        );

        Vector2 delta = handPoint - facePoint;

        bool insideMouthRegion =
            delta.magnitude <= handMouthDistanceThreshold &&
            Mathf.Abs(delta.x) <= handMouthHorizontalTolerance &&
            delta.y >= -handMouthVerticalBelowTolerance &&
            delta.y <= handMouthVerticalAboveTolerance;

        return insideMouthRegion;
    }

    // 기존 스크립트 호환성을 위해 이름은 유지합니다.
    public bool GetOriginFreezeState()
    {
        return GetHandOverMouthState();
    }
}
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class VisionInputData
{
    public Vector2 RawHandScreenPosition = new Vector2(0.5f, 0.5f);
    public bool IsGripActive;
    public float WristZ;
}

public class PlayerInputMapper : MonoBehaviour, IPlayerInput
{
    // ... [기존 Inspector 변수 및 LPF/상태머신 변수 동일 유지 (생략 없이 원본 유지)] ...
    [Header("시각적 피드백")]
    public RectTransform handCursor;
    public Camera mainCamera;

    [Header("인식 보정 파라미터")]
    public float handAimLerpSpeed = 15f;
    public float gripDistanceThreshold = 0.05f;
    public float freezeVarianceThreshold = 0.08f;

    [Header("Lean 파라미터")]
    public float leanZThreshold = 0.05f;
    public float leanCommitMaxTime = 0.8f;

    [Header("Gaze 판정 파라미터 (INP-001)")]
    public float maxGazeDistance = 15f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float readyTimeThreshold = 0.5f;
    public float loseFocusGraceTime = 0.5f;

    private readonly object _lockObj = new object();
    private VisionInputData _latestData = new VisionInputData();
    private bool _hasNewData = false;
    private VisionInputData _currentMainThreadData = new VisionInputData();
    private Vector2 _smoothedHandPosition;

    private bool _wasGripActive = false;
    private float _gripTimer = 0f;
    private bool _gripToggledThisFrame = false;
    private Vector2 _lastRawPos;
    private float _varianceTimer = 0f;
    private bool _isFreezing = false;
    private float _baselineWristZ = 0f;
    private bool _isLeaning = false;
    private float _leanTimer = 0f;
    private bool _leanCommittedThisFrame = false;

    // [신규 통합 브릿지용 변수]
    private Transform _currentHoverTarget;
    private Vector3 _currentHitPoint;
    private Vector3 _currentHitNormal;
    private float _currentReadyTimer = 0f;
    private float _currentGraceTimer = 0f;
    private bool _isReadyTriggered = false;

    private void OnEnable()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked += ReceiveHandData;
    }

    private void OnDisable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked -= ReceiveHandData;
    }

    private void ReceiveHandData(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0) return;
        var landmarks = result.handLandmarks[0].landmarks;
        float gripDist = Vector2.Distance(new Vector2(landmarks[4].x, landmarks[4].y), new Vector2(landmarks[8].x, landmarks[8].y));
        lock (_lockObj)
        {
            _latestData.RawHandScreenPosition = new Vector2(landmarks[8].x, landmarks[8].y);
            _latestData.IsGripActive = gripDist < gripDistanceThreshold;
            _latestData.WristZ = landmarks[0].z;
            _hasNewData = true;
        }
    }

    private void Start()
    {
        _smoothedHandPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Invoke(nameof(CalibrateBaseline), 3.0f);
    }

    private void Update()
    {
        lock (_lockObj)
        {
            if (_hasNewData)
            {
                _currentMainThreadData.RawHandScreenPosition = _latestData.RawHandScreenPosition;
                _currentMainThreadData.IsGripActive = _latestData.IsGripActive;
                _currentMainThreadData.WristZ = _latestData.WristZ;
                _hasNewData = false;
            }
        }

        float screenX = _currentMainThreadData.RawHandScreenPosition.x * Screen.width;
        float screenY = (1.0f - _currentMainThreadData.RawHandScreenPosition.y) * Screen.height;
        _smoothedHandPosition = Vector2.Lerp(_smoothedHandPosition, new Vector2(screenX, screenY), Time.deltaTime * handAimLerpSpeed);

        if (handCursor != null) handCursor.position = _smoothedHandPosition;

        ProcessGripLogic();
        ProcessFreezeLogic();
        ProcessLeanLogic();
        ProcessGazeInteraction();
    }

    // ... [ProcessGripLogic, ProcessFreezeLogic, ProcessLeanLogic 기존 동일 유지 (생략)] ...
    private void ProcessGripLogic() { /* 동일 */ _gripToggledThisFrame = false; if (_currentMainThreadData.IsGripActive && !_wasGripActive) _gripTimer = 0f; else if (_currentMainThreadData.IsGripActive) _gripTimer += Time.deltaTime; else if (!_currentMainThreadData.IsGripActive && _wasGripActive) { if (_gripTimer >= 0.15f && _gripTimer <= 0.35f) _gripToggledThisFrame = true; } _wasGripActive = _currentMainThreadData.IsGripActive; }
    private void ProcessFreezeLogic() { /* 동일 */ float movementVariance = Vector2.Distance(_currentMainThreadData.RawHandScreenPosition, _lastRawPos); if (movementVariance < freezeVarianceThreshold) { _varianceTimer += Time.deltaTime; if (_varianceTimer >= 1.0f) _isFreezing = true; } else { _varianceTimer = 0f; _isFreezing = false; } _lastRawPos = _currentMainThreadData.RawHandScreenPosition; }
    private void ProcessLeanLogic() { /* 동일 */ _leanCommittedThisFrame = false; bool isCurrentlyLeaning = (_currentMainThreadData.WristZ < _baselineWristZ - leanZThreshold); if (isCurrentlyLeaning && !_isLeaning) { _isLeaning = true; _leanTimer = 0f; } else if (isCurrentlyLeaning) _leanTimer += Time.deltaTime; else if (!isCurrentlyLeaning && _isLeaning) { if (_leanTimer <= leanCommitMaxTime && _leanTimer > 0.1f) _leanCommittedThisFrame = true; _isLeaning = false; } }

    private void ProcessGazeInteraction()
    {
        if (mainCamera == null) return;
        Ray ray = mainCamera.ScreenPointToRay(_smoothedHandPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxGazeDistance, targetMask))
        {
            Transform hitTarget = hit.transform;
            Vector3 dirToTarget = hit.point - mainCamera.transform.position;

            if (!Physics.Raycast(mainCamera.transform.position, dirToTarget.normalized, dirToTarget.magnitude, obstacleMask))
            {
                // 충돌 정보 캐싱 (브릿지용)
                _currentHitPoint = hit.point;
                _currentHitNormal = hit.normal;

                if (_currentHoverTarget == hitTarget)
                {
                    _currentGraceTimer = 0f;
                    if (!_isReadyTriggered)
                    {
                        _currentReadyTimer += Time.deltaTime;
                        if (_currentReadyTimer >= readyTimeThreshold)
                        {
                            _isReadyTriggered = true;
                        }
                    }
                }
                else
                {
                    ResetGazeState();
                    _currentHoverTarget = hitTarget;
                }
            }
            else ApplyGracePeriod();
        }
        else ApplyGracePeriod();
    }

    private void ApplyGracePeriod()
    {
        if (_currentHoverTarget != null)
        {
            _currentGraceTimer += Time.deltaTime;
            if (_currentGraceTimer >= loseFocusGraceTime) ResetGazeState();
        }
    }

    private void ResetGazeState()
    {
        _currentHoverTarget = null;
        _currentReadyTimer = 0f;
        _currentGraceTimer = 0f;
        _isReadyTriggered = false;
    }

    // ====================================================================
    // IPlayerInput 인터페이스 통합 반환부
    // ====================================================================
    public Vector3 GetHandAimTarget() { return _smoothedHandPosition; }
    public bool IsGripToggled() { return _gripToggledThisFrame; }
    public bool IsLeanCommitted() { return _leanCommittedThisFrame; }
    public bool IsFreezing() { return _isFreezing; }

    public Transform GetHoveredTarget() { return _currentHoverTarget; }
    public Vector3 GetHoveredPoint() { return _currentHitPoint; }
    public Vector3 GetHoveredNormal() { return _currentHitNormal; }
    public bool IsTargetReady() { return _isReadyTriggered; }

    public void CalibrateBaseline() { _baselineWristZ = _currentMainThreadData.WristZ; }
}
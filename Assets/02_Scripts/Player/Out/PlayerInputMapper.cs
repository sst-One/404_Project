using UnityEngine;
using UnityEngine.AI; // NavMesh 사용
using Mediapipe.Tasks.Vision.HandLandmarker;

public class VisionInputData
{
    public Vector2 RawHandScreenPosition = new Vector2(0.5f, 0.5f);
    public bool IsGripActive;
    public float WristZ;
}

public class PlayerInputMapper : MonoBehaviour, IPlayerInput
{
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

    [Header("바닥 동적 탐색 (추가)")]
    public LayerMask floorLayer;
    public GameObject moveUIIndicator;

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

    // 브릿지 변수
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
        if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
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

    private void ProcessGripLogic() { /* 원본 유지 */ _gripToggledThisFrame = false; if (_currentMainThreadData.IsGripActive && !_wasGripActive) _gripTimer = 0f; else if (_currentMainThreadData.IsGripActive) _gripTimer += Time.deltaTime; else if (!_currentMainThreadData.IsGripActive && _wasGripActive) { if (_gripTimer >= 0.15f && _gripTimer <= 0.35f) _gripToggledThisFrame = true; } _wasGripActive = _currentMainThreadData.IsGripActive; }
    private void ProcessFreezeLogic() { /* 원본 유지 */ float movementVariance = Vector2.Distance(_currentMainThreadData.RawHandScreenPosition, _lastRawPos); if (movementVariance < freezeVarianceThreshold) { _varianceTimer += Time.deltaTime; if (_varianceTimer >= 1.0f) _isFreezing = true; } else { _varianceTimer = 0f; _isFreezing = false; } _lastRawPos = _currentMainThreadData.RawHandScreenPosition; }
    private void ProcessLeanLogic() { /* 원본 유지 */ _leanCommittedThisFrame = false; bool isCurrentlyLeaning = (_currentMainThreadData.WristZ < _baselineWristZ - leanZThreshold); if (isCurrentlyLeaning && !_isLeaning) { _isLeaning = true; _leanTimer = 0f; } else if (isCurrentlyLeaning) _leanTimer += Time.deltaTime; else if (!isCurrentlyLeaning && _isLeaning) { if (_leanTimer <= leanCommitMaxTime && _leanTimer > 0.1f) _leanCommittedThisFrame = true; _isLeaning = false; } }

    private void ProcessGazeInteraction()
    {
        if (mainCamera == null) return;
        Ray ray = mainCamera.ScreenPointToRay(_smoothedHandPosition);
        RaycastHit hit;

        bool hitValidObject = false;

        // 1. 오브젝트 타겟 (Interactable 등) 탐색
        if (Physics.Raycast(ray, out hit, maxGazeDistance, targetMask))
        {
            Vector3 dirToTarget = hit.point - mainCamera.transform.position;

            if (!Physics.Raycast(mainCamera.transform.position, dirToTarget.normalized, dirToTarget.magnitude, obstacleMask))
            {
                hitValidObject = true;
                if (moveUIIndicator != null) moveUIIndicator.SetActive(false); // 오브젝트를 볼 땐 이동 마커 끔

                _currentHitPoint = hit.point;
                _currentHitNormal = hit.normal;

                if (_currentHoverTarget == hit.transform)
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
                    _currentHoverTarget = hit.transform;
                }
            }
        }

        // 2. 오브젝트를 보고 있지 않을 때 바닥(Floor) NavMesh 탐색
        if (!hitValidObject)
        {
            ApplyGracePeriod();

            if (Physics.Raycast(ray, out hit, maxGazeDistance, floorLayer))
            {
                NavMeshHit navHit;
                if (NavMesh.SamplePosition(hit.point, out navHit, 0.5f, NavMesh.AllAreas))
                {
                    _currentHitPoint = navHit.position; // 외부 매니저가 이 좌표를 읽어감
                    _currentHitNormal = Vector3.up;

                    // 이동 마커 UI 동적 렌더링
                    if (moveUIIndicator != null)
                    {
                        moveUIIndicator.transform.position = navHit.position + Vector3.up * 0.05f;
                        moveUIIndicator.SetActive(true);
                    }
                }
                else
                {
                    if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
                }
            }
            else
            {
                if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
            }
        }
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

    // 인터페이스 반환부 (외부 InteractionManager 등이 이 값들을 가져가 MovementManager로 넘깁니다)
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
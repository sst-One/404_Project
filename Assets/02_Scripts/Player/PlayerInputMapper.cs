using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

/// <summary>
/// 스레드 간 데이터 전송을 위한 DTO (Data Transfer Object)
/// </summary>
public class VisionInputData
{
    public Vector2 RawHandScreenPosition;
    public bool IsGripActive;
    public float WristZ;
}

public class PlayerInputMapper : MonoBehaviour, IPlayerInput
{
    [Header("시각적 피드백")]
    public RectTransform handCursor;
    public Camera mainCamera;

    [Header("인식 보정 파라미터 (LPF & Threshold)")]
    [Tooltip("손 떨림 보정을 위한 Lerp 속도. 값이 작을수록 부드럽지만 지연 발생")]
    public float handAimLerpSpeed = 15f;
    [Tooltip("엄지와 검지 사이 거리 임계값 (그립 판정)")]
    public float gripDistanceThreshold = 0.05f;
    [Tooltip("Freeze 상태를 유지하기 위한 움직임 허용치 (PARAM-009)")]
    public float freezeVarianceThreshold = 0.08f;

    // 스레드 동기화 락 및 우체통
    private readonly object _lockObj = new object();
    private VisionInputData _latestData = new VisionInputData();
    private bool _hasNewData = false;

    // 메인 스레드 연산용 상태 변수
    private VisionInputData _currentMainThreadData = new VisionInputData();
    private Vector2 _smoothedHandPosition;

    // 상태 머신 변수
    private bool _wasGripActive = false;
    private float _gripTimer = 0f;
    private bool _gripToggledThisFrame = false;

    private Vector2 _lastRawPos;
    private float _varianceTimer = 0f;
    private bool _isFreezing = false;

    [Header("Lean (상체 기울이기) 파라미터")]
    [Tooltip("손목 Z축이 기준점 대비 얼마나 가까워져야 Lean으로 인정할지 (단위: 미터/비율)")]
    public float leanZThreshold = 0.05f;
    [Tooltip("Lean 동작이 완료되어야 하는 최대 시간 (PARAM-006: 0.8초)")]
    public float leanCommitMaxTime = 0.8f;

    // Lean 상태 머신 변수
    private float _baselineWristZ = 0f;
    private bool _isLeaning = false;
    private float _leanTimer = 0f;
    private bool _leanCommittedThisFrame = false;

    private void OnEnable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked += ReceiveHandData;
    }

    private void OnDisable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked -= ReceiveHandData;
    }

    // ❌ 백그라운드 스레드 실행 (유니티 API 호출 금지)
    private void ReceiveHandData(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0) return;

        var landmarks = result.handLandmarks[0].landmarks;
        var wrist = landmarks[0];
        var thumbTip = landmarks[4];
        var indexTip = landmarks[8];

        // 엄지와 검지 끝의 2D 거리 계산으로 Grip 상태 임시 판정
        float gripDist = Vector2.Distance(new Vector2(thumbTip.x, thumbTip.y), new Vector2(indexTip.x, indexTip.y));
        bool isGrip = gripDist < gripDistanceThreshold;

        lock (_lockObj)
        {
            _latestData.RawHandScreenPosition = new Vector2(indexTip.x, indexTip.y);
            _latestData.IsGripActive = isGrip;
            _latestData.WristZ = wrist.z;
            _hasNewData = true;
        }
    }

    private void Start()
    {
        // FEAT-033: 시작 후 3초 뒤 자동 캘리브레이션 (임시 연출)
        Invoke(nameof(CalibrateBaseline), 3.0f);
    }

    // 🟢 메인 스레드 실행 (유니티 API 접근 안전 구역)
    private void Update()
    {
        // 1. 스레드 세이프 데이터 팝업
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

        // 2. 좌표 보정 (Low-Pass Filter) -> 미세 떨림 방지
        float screenX = _currentMainThreadData.RawHandScreenPosition.x * Screen.width;
        float screenY = (1.0f - _currentMainThreadData.RawHandScreenPosition.y) * Screen.height;
        Vector2 targetScreenPos = new Vector2(screenX, screenY);

        _smoothedHandPosition = Vector2.Lerp(_smoothedHandPosition, targetScreenPos, Time.deltaTime * handAimLerpSpeed);

        if (handCursor != null)
        {
            handCursor.position = _smoothedHandPosition;
        }

        // 3. 상태 머신: Grip 판정 (PARAM-004, PARAM-005)
        _gripToggledThisFrame = false;
        if (_currentMainThreadData.IsGripActive && !_wasGripActive)
        {
            _gripTimer = 0f; // 그립 시작
        }
        else if (_currentMainThreadData.IsGripActive)
        {
            _gripTimer += Time.deltaTime; // 그립 유지 중
        }
        else if (!_currentMainThreadData.IsGripActive && _wasGripActive)
        {
            // 그립을 풀었을 때 타이머가 조건에 맞으면 Toggle 판정
            if (_gripTimer >= 0.15f && _gripTimer <= 0.35f)
            {
                _gripToggledThisFrame = true;
            }
        }
        _wasGripActive = _currentMainThreadData.IsGripActive;

        // 4. 상태 머신: Freeze 판정 (PARAM-008, PARAM-009)
        float movementVariance = Vector2.Distance(_currentMainThreadData.RawHandScreenPosition, _lastRawPos);
        if (movementVariance < freezeVarianceThreshold)
        {
            _varianceTimer += Time.deltaTime;
            if (_varianceTimer >= 1.0f) _isFreezing = true;
        }
        else
        {
            _varianceTimer = 0f;
            _isFreezing = false;
        }
        _lastRawPos = _currentMainThreadData.RawHandScreenPosition;

        // 5. 상태 머신: Lean 판정 (PARAM-006, INP-003)
        _leanCommittedThisFrame = false;

        // Z축이 기준점보다 임계치 이상 가까워졌는지 확인 (카메라에 가까워지면 Z값이 보통 음수로 작아짐)
        bool isCurrentlyLeaning = (_currentMainThreadData.WristZ < _baselineWristZ - leanZThreshold);

        if (isCurrentlyLeaning && !_isLeaning)
        {
            _isLeaning = true;
            _leanTimer = 0f; // Lean 시작
        }
        else if (isCurrentlyLeaning)
        {
            _leanTimer += Time.deltaTime; // Lean 유지 중
        }
        else if (!isCurrentlyLeaning && _isLeaning)
        {
            // Lean을 풀고 제자리로 돌아왔을 때, 허용 시간 내에 이루어졌다면 Commit 판정
            if (_leanTimer <= leanCommitMaxTime && _leanTimer > 0.1f)
            {
                _leanCommittedThisFrame = true;
                Debug.Log("[Project 404] Lean Commit 감지 (이동 확정)!");
            }
            _isLeaning = false;
        }
    }

    // ====================================================================
    // IPlayerInput 인터페이스 구현부 (InteractionManager 및 FreezeManager에서 호출)
    // ====================================================================

    public Vector3 GetHandAimTarget()
    {
        return _smoothedHandPosition; // 보정된 위치를 반환하여 Raycast 오차 방지
    }

    public bool IsGripToggled()
    {
        return _gripToggledThisFrame;
    }

    public bool IsLeanCommitted()
    {
        return _leanCommittedThisFrame; ;
    }

    public bool IsFreezing()
    {
        return _isFreezing;
    }

    public void CalibrateBaseline()
    {
        _baselineWristZ = _currentMainThreadData.WristZ;
        Debug.Log($"[Project 404] 입력 캘리브레이션 완료. 기준 Z축: {_baselineWristZ}");
    }
}
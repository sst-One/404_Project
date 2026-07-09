using System;
using UnityEngine;
using UnityEngine.AI; // NavMesh 검증용 모듈 추가

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    public event Action<bool> OnFocusChanged;

    [Header("Layer Settings")]
    public LayerMask interactableLayer; // 문, 아이템 등 전용
    public LayerMask floorLayer;        // 신규: 바닥 평면 전용

    [Header("Interaction Settings")]
    public float readyDelay = 0.5f;
    public float rayDistance = 15f;
    public float navMeshSampleDistance = 1.0f; // NavMesh 유효 반경 검증용

    [Header("Visual Indicator")]
    [Tooltip("플레이어가 바라보는 바닥 위치에 표시될 시각적 표식 (예: 원형 스프레드 패널)")]
    public Transform floorIndicator;

    private Camera _mainCamera;
    private IInteractable _currentFocusedObject;
    private float _focusTimer = 0f;
    private bool _isReady = false;

    // 바닥 제어용 상태 변수
    private bool _isFocusingFloor = false;
    private Vector3 _targetFloorPosition;
    private Material _indicatorMaterial;
    private Color _originalIndicatorColor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (floorIndicator != null)
        {
            // [TPM 핫픽스] 할당된 오브젝트가 씬(Scene)에 존재하지 않는 원본 프리팹(Asset)일 경우, 자동으로 씬에 복제본(Instance) 생성
            if (!floorIndicator.gameObject.scene.IsValid())
            {
                Transform instance = Instantiate(floorIndicator);
                instance.name = "FloorIndicator_Instance";
                floorIndicator = instance; // 이제부터 원본 프리팹 대신 복제된 인스턴스를 추적
                Debug.Log("[InteractionManager] 원본 프리팹 할당을 감지하여 런타임 인스턴스를 자동 생성했습니다.");
            }

            floorIndicator.gameObject.SetActive(false);

            var renderer = floorIndicator.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                // 인스턴스의 마테리얼이므로 더 이상 에러가 발생하지 않음
                _indicatorMaterial = renderer.material;
                _originalIndicatorColor = _indicatorMaterial.color;
            }
            else
            {
                Debug.LogError("[InteractionManager] FloorIndicator 프리팹 내부에 MeshRenderer 컴포넌트를 찾을 수 없습니다.");
            }
        }
    }

    private void Update()
    {
        if (InputManager.Instance == null || _mainCamera == null) return;

        // 호흡을 참는 Freeze 상태에서는 상호작용 및 이동 포커스 전면 차단
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            ClearAllFocus();
            return;
        }

        IPlayerInput input = InputManager.Instance.GetInput();
        Vector3 aimTarget = input.GetHandAimTarget();
        Ray ray = _mainCamera.ScreenPointToRay(aimTarget);

        // 🔍 [우선순위 1] 오브젝트 상호작용체 레이캐스트 검사
        if (Physics.Raycast(ray, out RaycastHit hitObject, rayDistance, interactableLayer))
        {
            DisableFloorIndicator();
            IInteractable interactable = hitObject.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (_currentFocusedObject != interactable)
                {
                    ChangeObjectFocus(interactable);
                }
                else
                {
                    ProcessObjectFocusTimer(input);
                }
            }
            else
            {
                ClearObjectFocus();
            }
            return; // 바닥 검사 스킵
        }

        // 오브젝트 타겟 상실 시 초기화
        ClearObjectFocus();

        // 🔍 [우선순위 2] 자유 이동 바닥 레이캐스트 검사 (큐브 배제 구조)
        if (Physics.Raycast(ray, out RaycastHit hitFloor, rayDistance, floorLayer))
        {
            // NavMesh 검증을 통해 이동 가능한 영역인지 필터링 (Boundary Break 방지)
            if (NavMesh.SamplePosition(hitFloor.point, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
            {
                _isFocusingFloor = true;
                _targetFloorPosition = navHit.position; // 가장 가까운 유효 NavMesh 좌표로 보정

                UpdateFloorIndicatorPosition(hitFloor.point, hitFloor.normal);
                ProcessFloorFocusTimer(input);
            }
            else
            {
                DisableFloorIndicator();
                _isFocusingFloor = false;
            }
        }
        else
        {
            DisableFloorIndicator();
            _isFocusingFloor = false;
            _focusTimer = 0f;
            _isReady = false;
        }
    }

    // ====================================================================
    // 내부 로직 처리부 (오브젝트 / 바닥 분리 제어)
    // ====================================================================

    private void ProcessFloorFocusTimer(IPlayerInput input)
    {
        if (!_isReady)
        {
            _focusTimer += Time.deltaTime;
            if (_indicatorMaterial != null)
            {
                // 포커싱 중일 때 노란색 연출
                _indicatorMaterial.color = Color.yellow;
            }

            if (_focusTimer >= readyDelay)
            {
                _isReady = true;
                Debug.Log("[InteractionManager] 바닥 이동 Ready 상태 도달.");
            }
        }
        else
        {
            if (_indicatorMaterial != null)
            {
                // 이동 대기(Ready) 상태 시 청록색 연출
                _indicatorMaterial.color = Color.cyan;
            }

            // 시선 유지 상태에서 Lean Commit 입력 발생 시 실시간 이동 처리
            if (input.IsLeanCommitted())
            {
                if (MovementManager.Instance != null && !MovementManager.Instance.IsMoving)
                {
                    if (_indicatorMaterial != null) _indicatorMaterial.color = Color.magenta;

                    MovementManager.Instance.MoveTo(_targetFloorPosition);
                    ResetFloorFocusState();
                }
            }
        }
    }

    private void UpdateFloorIndicatorPosition(Vector3 position, Vector3 normal)
    {
        if (floorIndicator == null) return;

        if (!floorIndicator.gameObject.activeSelf)
        {
            floorIndicator.gameObject.SetActive(true);
            OnFocusChanged?.Invoke(true);
        }

        floorIndicator.position = position + (normal * 0.01f); // 바닥 면 분할(Z-Fighting) 방지 미세 상향 조정
        floorIndicator.rotation = Quaternion.FromToRotation(Vector3.up, normal);
    }

    private void DisableFloorIndicator()
    {
        if (floorIndicator != null && floorIndicator.gameObject.activeSelf)
        {
            floorIndicator.gameObject.SetActive(false);
            OnFocusChanged?.Invoke(false);
        }
    }

    private void ResetFloorFocusState()
    {
        _focusTimer = 0f;
        _isReady = false;
        if (_indicatorMaterial != null) _indicatorMaterial.color = _originalIndicatorColor;
    }

    private void ChangeObjectFocus(IInteractable newInteractable)
    {
        ClearObjectFocus();
        _currentFocusedObject = newInteractable;
        _currentFocusedObject.OnFocusEnter();
        OnFocusChanged?.Invoke(true);
    }

    private void ProcessObjectFocusTimer(IPlayerInput input)
    {
        if (!_isReady)
        {
            _focusTimer += Time.deltaTime;
            if (_focusTimer >= readyDelay)
            {
                _isReady = true;
                _currentFocusedObject.OnReadyStateReached();
            }
        }
        else
        {
            if (input.IsGripToggled())
            {
                _currentFocusedObject.OnInteract();
                ClearObjectFocus();
            }
        }
    }

    private void ClearObjectFocus()
    {
        if (_currentFocusedObject != null)
        {
            _currentFocusedObject.OnFocusExit();
            _currentFocusedObject = null;
        }
        if (!_isFocusingFloor)
        {
            _focusTimer = 0f;
            _isReady = false;
        }
    }

    private void ClearAllFocus()
    {
        ClearObjectFocus();
        DisableFloorIndicator();
        _isFocusingFloor = false;
        _focusTimer = 0f;
        _isReady = false;
        OnFocusChanged?.Invoke(false);
    }
}
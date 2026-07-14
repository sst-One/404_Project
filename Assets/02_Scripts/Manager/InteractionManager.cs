using System;
using UnityEngine;
using UnityEngine.AI;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    public event Action<bool> OnFocusChanged;

    [Header("Layer Settings")]
    public LayerMask floorLayer; // Inspector에서 Floor 레이어를 반드시 할당

    [Header("Interaction Settings")]
    public float navMeshSampleDistance = 1.0f;

    [Header("Visual Indicator")]
    public Transform floorIndicator;

    private Camera _mainCamera;
    private IInteractable _currentFocusedObject;

    private bool _isFocusingFloor = false;
    private Vector3 _targetFloorPosition;
    private Material _indicatorMaterial;
    private Color _originalIndicatorColor;

    private bool _isObjectReadyTriggered = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        _mainCamera = Camera.main;
        if (floorIndicator != null)
        {
            if (!floorIndicator.gameObject.scene.IsValid())
            {
                Transform instance = Instantiate(floorIndicator);
                instance.name = "FloorIndicator_Instance";
                floorIndicator = instance;
            }
            floorIndicator.gameObject.SetActive(false);
            var renderer = floorIndicator.GetComponentInChildren<MeshRenderer>();
            if (renderer != null)
            {
                _indicatorMaterial = renderer.material;
                _originalIndicatorColor = _indicatorMaterial.color;
            }
        }
    }

    private void Update()
    {
        if (InputManager.Instance == null || _mainCamera == null) return;

        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            ClearAllFocus();
            return;
        }

        IPlayerInput input = InputManager.Instance.GetInput();
        Transform targetTransform = input.GetHoveredTarget();
        bool isReady = input.IsTargetReady();

        if (targetTransform != null)
        {
            // 우선순위 1. Interactable 오브젝트 판정
            IInteractable interactable = targetTransform.GetComponent<IInteractable>();
            if (interactable != null)
            {
                DisableFloorIndicator();
                _isFocusingFloor = false;

                if (_currentFocusedObject != interactable)
                {
                    ChangeObjectFocus(interactable);
                }
                else
                {
                    if (isReady)
                    {
                        if (!_isObjectReadyTriggered)
                        {
                            _isObjectReadyTriggered = true;
                            _currentFocusedObject.OnReadyStateReached();
                        }
                        if (input.IsGripToggled())
                        {
                            _currentFocusedObject.OnInteract();
                            ClearObjectFocus();
                        }
                    }
                    else
                    {
                        _isObjectReadyTriggered = false;
                    }
                }
                return;
            }

            // 우선순위 2. Floor 자유 이동 판정
            if (((1 << targetTransform.gameObject.layer) & floorLayer) != 0)
            {
                ClearObjectFocus();

                Vector3 hitPoint = input.GetHoveredPoint();
                Vector3 hitNormal = input.GetHoveredNormal();

                // 닿은 바닥 지점이 유효한 내비메쉬인지 검증
                if (NavMesh.SamplePosition(hitPoint, out NavMeshHit navHit, navMeshSampleDistance, NavMesh.AllAreas))
                {
                    _isFocusingFloor = true;
                    _targetFloorPosition = navHit.position;
                    UpdateFloorIndicatorPosition(hitPoint, hitNormal);

                    if (isReady)
                    {
                        if (_indicatorMaterial != null) _indicatorMaterial.color = Color.cyan;

                        if (input.IsLeanCommitted())
                        {
                            if (MovementManager.Instance != null && !MovementManager.Instance.IsMoving)
                            {
                                if (_indicatorMaterial != null) _indicatorMaterial.color = Color.magenta;
                                MovementManager.Instance.MoveTo(_targetFloorPosition);
                                DisableFloorIndicator();
                            }
                        }
                    }
                    else
                    {
                        if (_indicatorMaterial != null) _indicatorMaterial.color = Color.yellow;
                    }
                }
                else
                {
                    DisableFloorIndicator();
                    _isFocusingFloor = false;
                }
                return;
            }
        }

        ClearAllFocus();
    }

    private void UpdateFloorIndicatorPosition(Vector3 position, Vector3 normal)
    {
        if (floorIndicator == null) return;
        if (!floorIndicator.gameObject.activeSelf)
        {
            floorIndicator.gameObject.SetActive(true);
            OnFocusChanged?.Invoke(true);
        }
        floorIndicator.position = position + (normal * 0.01f);
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

    private void ChangeObjectFocus(IInteractable newInteractable)
    {
        ClearObjectFocus();
        _currentFocusedObject = newInteractable;
        _isObjectReadyTriggered = false;
        _currentFocusedObject.OnFocusEnter();
        OnFocusChanged?.Invoke(true);
    }

    private void ClearObjectFocus()
    {
        if (_currentFocusedObject != null)
        {
            _currentFocusedObject.OnFocusExit();
            _currentFocusedObject = null;
        }
        _isObjectReadyTriggered = false;
    }

    private void ClearAllFocus()
    {
        ClearObjectFocus();
        DisableFloorIndicator();
        _isFocusingFloor = false;
        OnFocusChanged?.Invoke(false);
    }
}
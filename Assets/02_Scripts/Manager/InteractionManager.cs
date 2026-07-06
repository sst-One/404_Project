using System;
using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    // 신규 추가: UI 갱신을 위한 이벤트 브로드캐스팅
    public event Action<bool> OnFocusChanged;

    [Header("Interaction Settings")]
    public LayerMask interactableLayer;
    public float readyDelay = 0.5f;
    public float rayDistance = 15f;

    private Camera _mainCamera;
    private IInteractable _currentFocusedObject;
    private float _focusTimer = 0f;
    private bool _isReady = false;

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
    }

    private void Update()
    {
        if (InputManager.Instance == null || _mainCamera == null) return;

        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            ClearFocus();
            return;
        }

        IPlayerInput input = InputManager.Instance.GetInput();
        Vector3 aimTarget = input.GetHandAimTarget();
        Ray ray = _mainCamera.ScreenPointToRay(aimTarget);

        if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactableLayer))
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                if (_currentFocusedObject != interactable)
                {
                    ChangeFocus(interactable);
                }
                else
                {
                    ProcessFocusTimer(input);
                }
            }
            else
            {
                ClearFocus();
            }
        }
        else
        {
            ClearFocus();
        }
    }

    private void ChangeFocus(IInteractable newInteractable)
    {
        ClearFocus();
        _currentFocusedObject = newInteractable;
        _currentFocusedObject.OnFocusEnter();

        // 신규 추가: 타겟 포착 이벤트 발생
        OnFocusChanged?.Invoke(true);
    }

    private void ProcessFocusTimer(IPlayerInput input)
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
                ClearFocus();
            }
            else if (input.IsLeanCommitted())
            {
                _currentFocusedObject.OnLean();
                ClearFocus();
            }
        }
    }

    private void ClearFocus()
    {
        if (_currentFocusedObject != null)
        {
            _currentFocusedObject.OnFocusExit();
            _currentFocusedObject = null;
        }
        _focusTimer = 0f;
        _isReady = false;

        // 신규 추가: 타겟 상실 이벤트 발생
        OnFocusChanged?.Invoke(false);
    }
}
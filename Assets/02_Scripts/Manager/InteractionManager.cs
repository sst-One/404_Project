using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

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
            ClearFocus(); // 바라보던 대상 초기화
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
            // Grip 입력 처리
            if (input.IsGripToggled())
            {
                _currentFocusedObject.OnInteract();
                ClearFocus();
            }
            // Lean 입력 처리 추가
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
    }
}
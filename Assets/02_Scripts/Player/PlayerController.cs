using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }

    [Header("Input Mode")]
    public bool useWebcam = true;
    public bool forceFallbackMode = false;

    [Header("Gaze & Interaction Settings")]
    public Camera mainCamera;
    public float maxGazeDistance = 5.0f;
    public float gazeRadius = 0.05f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float readyTimeThreshold = 0.5f;

    [Header("Movement Settings")]
    public float stepDistance = 1.5f;
    public float moveSpeed = 3.0f;
    public float footstepInterval = 0.4f;

    [Header("Audio Settings")]
    public AudioSource playerFootstepSource;
    public string footstepClipName = "SND-006_FootWalk_Oneshot_Loop";

    // Public Read-Only States
    public Vector2 GazeScreenPosition { get; private set; }
    public bool IsGripTriggered { get; private set; }
    public bool IsGripHeld { get; private set; }
    public bool IsLeanTriggered { get; private set; }
    public bool IsFreezeActive { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsFloorValid { get; private set; }
    public Transform CurrentHoverTarget { get; private set; }
    public bool IsTargetReady { get; private set; }

    // Internal States
    private bool _wasReachHeldLastFrame;
    private bool _wasLeanHeldLastFrame;
    private float _currentReadyTimer;
    private IInteractable _currentFocusedObject;
    private bool _isObjectReadyTriggered;
    private CharacterController _cc;
    private Coroutine _activeMoveCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cc = GetComponent<CharacterController>();
        if (mainCamera == null) mainCamera = Camera.main;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            forceFallbackMode = !forceFallbackMode;
        }

        IsGripTriggered = false;
        IsLeanTriggered = false;

        // 통합된 UIManager 블로킹 판정 
        bool isTutorialBlocking = TutorialCalibrationUI.Instance != null &&
                                  TutorialCalibrationUI.Instance.gameObject.activeInHierarchy &&
                                  !TutorialCalibrationUI.Instance.IsInCalibrationTestMode;

        if ((UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) || isTutorialBlocking)
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            ClearAllFocus();
            return;
        }

        GazeScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);
        ProcessInputs();

        if (!IsMoving)
        {
            ProcessGazeAndInteraction();
            if (IsLeanTriggered) CalculateAndMoveForward();
        }
    }

    private void ProcessInputs()
    {
        if (forceFallbackMode || !useWebcam || VisionTrackingManager.Instance == null || !VisionTrackingManager.Instance.isTracking)
        {
            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) IsGripTriggered = true;
                if (Mouse.current.leftButton.isPressed) IsGripHeld = true;
            }
            if (Keyboard.current != null)
            {
                if (Keyboard.current.wKey.wasPressedThisFrame) IsLeanTriggered = true;
                if (Keyboard.current.sKey.isPressed) IsFreezeActive = true;
            }
        }
        else
        {
            bool isCurrentlyReaching = VisionTrackingManager.Instance.GetReachState();
            IsGripHeld = isCurrentlyReaching;
            if (isCurrentlyReaching && !_wasReachHeldLastFrame) IsGripTriggered = true;
            _wasReachHeldLastFrame = isCurrentlyReaching;

            bool isCurrentlyLeaning = VisionTrackingManager.Instance.GetLeanState();
            if (isCurrentlyLeaning && !_wasLeanHeldLastFrame) IsLeanTriggered = true;
            _wasLeanHeldLastFrame = isCurrentlyLeaning;

            IsFreezeActive = VisionTrackingManager.Instance.GetOriginFreezeState();
        }
    }

    private void ProcessGazeAndInteraction()
    {
        if (mainCamera == null) return;
        IsFloorValid = false;

        Ray ray = mainCamera.ScreenPointToRay(GazeScreenPosition);
        if (Physics.SphereCast(ray, gazeRadius, out RaycastHit hit, maxGazeDistance, targetMask | obstacleMask))
        {
            if ((obstacleMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                ClearAllFocus();
                return;
            }

            int walkableLayer = LayerMask.NameToLayer("Walkable");
            int hidingSpotLayer = LayerMask.NameToLayer("HidingSpot");

            if (hit.transform.gameObject.layer == walkableLayer || hit.transform.gameObject.layer == hidingSpotLayer)
            {
                IsFloorValid = true;
                ClearObjectFocus();
            }
            else if ((targetMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                ProcessTargetHover(hit.transform);
            }
        }
        else
        {
            ClearAllFocus();
        }
    }

    private void ProcessTargetHover(Transform hitTransform)
    {
        if (CurrentHoverTarget == hitTransform)
        {
            if (!IsTargetReady)
            {
                _currentReadyTimer += Time.deltaTime;
                if (_currentReadyTimer >= readyTimeThreshold) IsTargetReady = true;
            }
        }
        else
        {
            CurrentHoverTarget = hitTransform;
            _currentReadyTimer = 0f;
            IsTargetReady = false;
        }

        IInteractable interactable = hitTransform.GetComponent<IInteractable>();
        if (interactable != null)
        {
            if (_currentFocusedObject != interactable)
            {
                ChangeObjectFocus(interactable);
            }
            else if (IsTargetReady)
            {
                if (!_isObjectReadyTriggered)
                {
                    _isObjectReadyTriggered = true;
                    _currentFocusedObject.OnReadyStateReached();
                }

                if (IsGripTriggered)
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
    }

    private void ChangeObjectFocus(IInteractable newInteractable)
    {
        ClearObjectFocus();
        _currentFocusedObject = newInteractable;
        _isObjectReadyTriggered = false;
        _currentFocusedObject.OnFocusEnter();
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
        CurrentHoverTarget = null;
        _currentReadyTimer = 0f;
        IsTargetReady = false;
        IsFloorValid = false;
    }

    private void CalculateAndMoveForward()
    {
        if (mainCamera == null || _cc == null || !_cc.enabled) return;

        Vector3 forwardDir = mainCamera.transform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        if (forwardDir.sqrMagnitude > 0.01f)
        {
            if (_activeMoveCoroutine != null) StopCoroutine(_activeMoveCoroutine);
            _activeMoveCoroutine = StartCoroutine(MoveRoutine(forwardDir));
        }
    }

    private IEnumerator MoveRoutine(Vector3 direction)
    {
        IsMoving = true;
        float movedDistance = 0f;

        // 발소리 코루틴을 강제 통제하기 위한 생명주기 캡슐화
        Coroutine footstepCoroutine = null;
        bool keepPlayingFootstep = true;

        if (stepDistance > 0.01f)
        {
            footstepCoroutine = StartCoroutine(FootstepLoopRoutine(() => keepPlayingFootstep));

            while (movedDistance < stepDistance)
            {
                if (_cc == null || !_cc.enabled) break;

                float step = moveSpeed * Time.deltaTime;
                _cc.Move(direction * step);
                movedDistance += step;
                yield return null;
            }
        }

        keepPlayingFootstep = false; // 플래그를 꺼서 발소리 즉각 중단
        if (footstepCoroutine != null) StopCoroutine(footstepCoroutine);

        IsMoving = false;
        _activeMoveCoroutine = null;

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.2f);
    }

    // 무한루프 고스트 사운드 발생을 막는 델리게이트 조건부 코루틴
    private IEnumerator FootstepLoopRoutine(System.Func<bool> condition)
    {
        while (condition())
        {
            if (playerFootstepSource != null && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.GetClip(footstepClipName);
                if (clip != null) playerFootstepSource.PlayOneShot(clip);
            }
            yield return new WaitForSeconds(footstepInterval);
        }
    }
}
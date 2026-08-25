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
    public float gazeRadius = 0.3f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float readyTimeThreshold = 0.5f;

    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;
    public float footstepInterval = 0.4f;

    [Header("Audio Settings (3D Spatial)")]
    public AudioSource playerFootstepSource;
    public string footstepClipName = "SND-006_FootWalk_Oneshot_Loop";

    public Vector2 GazeScreenPosition { get; private set; }
    public bool IsGripTriggered { get; private set; }
    public bool IsGripHeld { get; private set; }
    public bool IsLeanTriggered { get; private set; }
    public bool IsLeanHeld { get; private set; }
    public bool IsFreezeActive { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsFloorValid { get; private set; }
    public Transform CurrentHoverTarget { get; private set; }
    public bool IsTargetReady { get; private set; }

    private bool _wasReachHeldLastFrame;
    private bool _wasLeanHeldLastFrame;
    private float _currentReadyTimer;
    private IInteractable _currentFocusedObject;
    private bool _isObjectReadyTriggered;
    private CharacterController _cc;
    private float _footstepTimer = 0f;
    private int _walkableLayer;
    private int _hidingSpotLayer;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        _cc = GetComponent<CharacterController>();
        if (mainCamera == null) mainCamera = Camera.main;

        // [최적화] 레이어 해시 캐싱
        _walkableLayer = LayerMask.NameToLayer("Walkable");
        _hidingSpotLayer = LayerMask.NameToLayer("HidingSpot");
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            forceFallbackMode = !forceFallbackMode;
        }

        IsGripTriggered = false;
        IsLeanTriggered = false;

        bool isTutorialBlocking = TutorialCalibrationUI.Instance != null &&
                                  TutorialCalibrationUI.Instance.gameObject.activeInHierarchy &&
                                  !TutorialCalibrationUI.Instance.IsInCalibrationTestMode;

        if ((UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) || isTutorialBlocking)
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            IsLeanHeld = false;
            IsMoving = false;
            ClearAllFocus();
            return;
        }

        GazeScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        ProcessInputs();
        ProcessGazeAndInteraction();

        if (IsLeanHeld) MoveContinuously();
        else StopMovement();
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
                IsLeanHeld = Keyboard.current.wKey.isPressed;
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
            IsLeanHeld = isCurrentlyLeaning;
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
            GameObject hitObj = hit.transform.gameObject;
            int hitLayer = hitObj.layer;

            if ((obstacleMask & (1 << hitLayer)) != 0)
            {
                ClearAllFocus();
                return;
            }

            if (hitLayer == _walkableLayer || hitLayer == _hidingSpotLayer)
            {
                IsFloorValid = true;
                ClearObjectFocus();
            }
            else if ((targetMask & (1 << hitLayer)) != 0)
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

    private void MoveContinuously()
    {
        if (mainCamera == null || _cc == null || !_cc.enabled) return;

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Stage1_Elevator)
        {
            StopMovement();
            return;
        }

        Vector3 forwardDir = mainCamera.transform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        if (forwardDir.sqrMagnitude > 0.01f)
        {
            IsMoving = true;
            _cc.Move(forwardDir * moveSpeed * Time.deltaTime);

            _footstepTimer += Time.deltaTime;
            if (_footstepTimer >= footstepInterval)
            {
                _footstepTimer = 0f;
                if (playerFootstepSource != null && AudioManager.Instance != null)
                {
                    AudioClip clip = AudioManager.Instance.GetClip(footstepClipName);
                    if (clip != null) playerFootstepSource.PlayOneShot(clip);
                }
            }

            if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.05f * Time.deltaTime);
        }
    }

    private void StopMovement()
    {
        IsMoving = false;
        _footstepTimer = footstepInterval;
    }
}
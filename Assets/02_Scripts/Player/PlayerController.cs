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
    public bool canMove = true;

    [Header("Gaze & Interaction Settings")]
    public Camera mainCamera;
    public float maxGazeDistance = 1f;
    public float gazeRadius = 0.3f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float readyTimeThreshold = 0.5f;

    [Header("Movement Settings")]
    [Tooltip("앞뒤 이동 속도 (m/s)")]
    public float moveSpeed = 3.0f;
    [Tooltip("좌우 이동 속도 배율. 0.55는 앞뒤 속도의 55%입니다.")]
    [Range(0.1f, 1f)] public float lateralMoveSpeedMultiplier = 0.55f;
    public float footstepInterval = 0.4f;

    [Header("Audio Settings (3D Spatial)")]
    public AudioSource playerFootstepSource;
    public string footstepClipName = "SND-006_FootWalk_Oneshot_Loop";

    public bool IsMovementLocked { get; private set; } = false;
    public bool IsCameraLocked { get; private set; } = false;

    public event System.Action<Transform> OnInteractTriggered;

    public Vector2 GazeScreenPosition { get; private set; }
    public bool IsGripTriggered { get; private set; }
    public bool IsGripHeld { get; private set; }
    public bool IsLeanTriggered { get; private set; }
    public bool IsLeanHeld { get; private set; }
    public Vector2 LeanMovementInput { get; private set; }
    public bool IsFreezeActive { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsFloorValid { get; private set; }
    public Transform CurrentHoverTarget { get; private set; }
    public bool IsTargetReady { get; private set; }

    public CharacterController CC { get; private set; }
    public FirstPersonCameraLook CamLook { get; private set; }

    private bool _wasReachHeldLastFrame;
    private bool _wasLeanHeldLastFrame;
    private float _currentReadyTimer;
    private IInteractable _currentFocusedObject;
    private bool _isObjectReadyTriggered;
    private float _footstepTimer = 0f;
    private int _walkableLayer;

    private AudioClip _cachedFootstepClip;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        CC = GetComponent<CharacterController>();
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) CamLook = mainCamera.GetComponent<FirstPersonCameraLook>();

        _walkableLayer = LayerMask.NameToLayer("Walkable");
    }

    private void Start()
    {
        if (AudioManager.Instance != null)
            _cachedFootstepClip = AudioManager.Instance.GetClip(footstepClipName);
    }

    public void SetMovementLock(bool isLocked)
    {
        IsMovementLocked = isLocked;
        if (isLocked) StopMovement();
    }

    public void SetCameraLock(bool isLocked)
    {
        IsCameraLocked = isLocked;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) forceFallbackMode = !forceFallbackMode;

        IsGripTriggered = false;
        IsLeanTriggered = false;

        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
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

        if (LeanMovementInput.sqrMagnitude > 0.01f && !IsMovementLocked && canMove) MoveContinuously();
        else StopMovement();
    }

    private void ProcessInputs()
    {
        LeanMovementInput = Vector2.zero;
        IsGripHeld = false;
        IsFreezeActive = false;

        if (forceFallbackMode || !useWebcam || VisionTrackingManager.Instance == null || !VisionTrackingManager.Instance.isTracking)
        {
            _wasReachHeldLastFrame = false;

            if (Mouse.current != null)
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) IsGripTriggered = true;
                IsGripHeld = Mouse.current.leftButton.isPressed;
            }

            if (Keyboard.current != null)
            {
                float horizontal = 0f;
                float vertical = 0f;
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) horizontal -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) horizontal += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) vertical -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) vertical += 1f;
                LeanMovementInput = new Vector2(horizontal, vertical);
                if (LeanMovementInput.sqrMagnitude > 1f) LeanMovementInput.Normalize();
                IsLeanHeld = LeanMovementInput.sqrMagnitude > 0.01f;
                IsLeanTriggered = IsLeanHeld && !_wasLeanHeldLastFrame;
                _wasLeanHeldLastFrame = IsLeanHeld;
                IsFreezeActive = Keyboard.current.leftShiftKey.isPressed;
            }
            else
            {
                IsLeanHeld = false;
                IsFreezeActive = false;
            }
            return;
        }

        VisionTrackingManager tracking = VisionTrackingManager.Instance;
        IsFreezeActive = tracking.GetHandOverMouthState();
        bool isCurrentlyReaching = !IsFreezeActive && tracking.GetReachState();
        IsGripHeld = isCurrentlyReaching;
        IsGripTriggered = isCurrentlyReaching && !_wasReachHeldLastFrame;
        _wasReachHeldLastFrame = isCurrentlyReaching;

        LeanMovementInput = tracking.GetLeanMovementInput();
        IsLeanHeld = LeanMovementInput.sqrMagnitude > 0.01f;
        IsLeanTriggered = IsLeanHeld && !_wasLeanHeldLastFrame;
        _wasLeanHeldLastFrame = IsLeanHeld;
    }

    private void ProcessGazeAndInteraction()
    {
        if (mainCamera == null) return;
        IsFloorValid = false;

        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);

        if (Physics.SphereCast(ray, gazeRadius, out RaycastHit hit, maxGazeDistance, targetMask | obstacleMask))
        {
            GameObject hitObj = hit.transform.gameObject;
            int hitLayer = hitObj.layer;

            if ((obstacleMask & (1 << hitLayer)) != 0)
            {
                ClearAllFocus();
                return;
            }

            if (hitLayer == _walkableLayer)
            {
                IsFloorValid = true;
                ClearObjectFocus();
            }
            else if ((targetMask & (1 << hitLayer)) != 0)
            {
                ProcessTargetHover(hit.transform);
            }
        }
        else ClearAllFocus();
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
            if (_currentFocusedObject != interactable) ChangeObjectFocus(interactable);
            else if (IsTargetReady)
            {
                if (!_isObjectReadyTriggered)
                {
                    _isObjectReadyTriggered = true;
                    _currentFocusedObject.OnReadyStateReached();
                }

                if (IsGripTriggered)
                {
                    OnInteractTriggered?.Invoke(hitTransform);
                    _currentFocusedObject.OnInteract();
                    ClearObjectFocus();
                }
            }
            else _isObjectReadyTriggered = false;
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
        if (CC == null || !CC.enabled) return;

        if (GameFlowManager.Instance != null)
        {
            GameStage stage = GameFlowManager.Instance.currentStage;
            if (stage == GameStage.Stage1_Elevator || stage == GameStage.Stage3_Anomaly || stage == GameStage.Stage4_Man)
            {
                StopMovement();
                return;
            }
        }

        Vector3 forwardDir = transform.forward;
        Vector3 rightDir = transform.right;
        forwardDir.y = 0f;
        rightDir.y = 0f;
        forwardDir.Normalize();
        rightDir.Normalize();

        // 좌우 속도를 앞뒤 속도와 분리합니다. 입력 벡터를 다시 Normalize하면
        // 좌우 속도 배율이 사라지므로 여기서는 축별 속도를 그대로 유지합니다.
        Vector3 movement =
            rightDir * (LeanMovementInput.x * moveSpeed * lateralMoveSpeedMultiplier) +
            forwardDir * (LeanMovementInput.y * moveSpeed);

        if (movement.sqrMagnitude <= 0.001f)
        {
            StopMovement();
            return;
        }

        IsMoving = true;
        CC.Move(movement * Time.deltaTime);

        _footstepTimer += Time.deltaTime;
        if (_footstepTimer >= footstepInterval)
        {
            _footstepTimer = 0f;
            if (playerFootstepSource != null && _cachedFootstepClip != null)
                playerFootstepSource.PlayOneShot(_cachedFootstepClip);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.05f * Time.deltaTime);
    }

    private void StopMovement()
    {
        IsMoving = false;
        _footstepTimer = footstepInterval;
    }

    public void StartCameraShake(float duration, float magnitude)
    {
        StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        if (mainCamera == null) yield break;
        Vector3 originalPos = mainCamera.transform.localPosition;
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            mainCamera.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }
        mainCamera.transform.localPosition = originalPos;
    }
}
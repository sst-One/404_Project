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
    public float moveSpeed = 3.0f;
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
    public bool IsFreezeActive { get; private set; }
    public bool IsMoving { get; private set; }
    public bool IsFloorValid { get; private set; }
    public Transform CurrentHoverTarget { get; private set; }
    public bool IsTargetReady { get; private set; }

    // 외부 컴포넌트 접근 최적화를 위한 퍼블릭 프로퍼티
    public CharacterController CC { get; private set; }
    public FirstPersonCameraLook CamLook { get; private set; }

    private bool _wasReachHeldLastFrame;
    private bool _wasLeanHeldLastFrame;
    private float _currentReadyTimer;
    private IInteractable _currentFocusedObject;
    private bool _isObjectReadyTriggered;
    private float _footstepTimer = 0f;
    private int _walkableLayer;

    // [최적화] 발소리 오디오 영구 캐싱
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

        // UI 블로킹 시 중앙 통제 (SRP 완벽 준수)
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            IsLeanHeld = false;
            IsMoving = false;
            ClearAllFocus();
            return;
        }

        // [최적화] 화면 중앙 좌표는 해상도 변경 시에만 참조하면 됨
        GazeScreenPosition = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        ProcessInputs();
        ProcessGazeAndInteraction();

        if (IsLeanHeld && !IsMovementLocked) MoveContinuously();
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

        // [최적화] ScreenPointToRay 수학 연산 폐기. 정중앙 기준 Ray 직접 생성
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
        if (mainCamera == null || CC == null || !CC.enabled) return;

        if (GameFlowManager.Instance != null)
        {
            GameStage stage = GameFlowManager.Instance.currentStage;
            if (stage == GameStage.Stage1_Elevator || stage == GameStage.Stage3_Anomaly || stage == GameStage.Stage4_Man)
            {
                StopMovement();
                return;
            }
        }

        Vector3 forwardDir = mainCamera.transform.forward;
        forwardDir.y = 0;
        forwardDir.Normalize();

        if (forwardDir.sqrMagnitude > 0.01f)
        {
            IsMoving = true;
            CC.Move(forwardDir * moveSpeed * Time.deltaTime);

            _footstepTimer += Time.deltaTime;
            if (_footstepTimer >= footstepInterval)
            {
                _footstepTimer = 0f;
                // [최적화] 캐싱된 발소리 직접 사용
                if (playerFootstepSource != null && _cachedFootstepClip != null)
                    playerFootstepSource.PlayOneShot(_cachedFootstepClip);
            }

            if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.05f * Time.deltaTime);
        }
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
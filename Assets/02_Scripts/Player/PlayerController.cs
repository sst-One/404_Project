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
    public float maxGazeDistance = 0.75f;
    public float gazeRadius = 0.18f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;
    public float readyTimeThreshold = 0.25f;
    [Tooltip("아이템 상호작용 보정 최대 거리")]
    public float interactionAssistDistance = 1.3f;
    [Tooltip("아이템 상호작용 보정 반경")]
    public float interactionAssistRadius = 0.18f;
    [Tooltip("거리 제한 예외가 지정된 엘리베이터 버튼을 시선으로 탐색할 최대 거리")]
    [Min(1f)] public float distanceIndependentScanDistance = 100f;

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
    private bool _wasFreezeActiveLastFrame;
    private float _freezeAudioCandidateTimer;
    private bool _breathAudioPlayedForCurrentFreeze;
    private AudioSource _breathInhaleSource;

    [Header("Breath Audio Settings")]
    [Tooltip("숨참기 소리 재생 전 오작동을 걸러내기 위해 유지해야 하는 시간")]
    [Min(0f)] public float breathAudioConfirmDelay = 0.2f;
    [Tooltip("숨참기 시작 소리의 음량")]
    [Range(0f, 1f)] public float breathInhaleVolume = 0.2f;

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
        IsFreezeActive = HidingSpotManager.Instance != null && HidingSpotManager.Instance.IsHiding;

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
            }
            else
            {
                IsLeanHeld = false;
                IsFreezeActive = HidingSpotManager.Instance != null && HidingSpotManager.Instance.IsHiding;
            }
            return;
        }

        VisionTrackingManager tracking = VisionTrackingManager.Instance;
        IsFreezeActive = HidingSpotManager.Instance != null && HidingSpotManager.Instance.IsHiding;
        bool isCurrentlyReaching = tracking.GetReachState();
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
        if (mainCamera == null)
        {
            ClearAllFocus();
            return;
        }

        IsFloorValid = false;
        Ray ray = new Ray(mainCamera.transform.position, mainCamera.transform.forward);
        // 일반 상호작용은 플레이어가 물체 가까이 접근해야 합니다.
        // 엘리베이터 버튼만 별도 플래그를 통해 거리 제한 없이 시선 탐색합니다.
        float normalInteractionDistance = Mathf.Min(maxGazeDistance, interactionAssistDistance);
        float castRadius = Mathf.Max(gazeRadius, interactionAssistRadius);
        float castDistance = Mathf.Max(normalInteractionDistance, distanceIndependentScanDistance);
        int queryMask = targetMask.value | obstacleMask.value;

        RaycastHit[] hits = Physics.SphereCastAll(
            ray, castRadius, castDistance, queryMask, QueryTriggerInteraction.Collide);

        if (hits == null || hits.Length == 0)
        {
            ClearAllFocus();
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;
            if (hitCollider == null || !hitCollider.enabled || !hitCollider.gameObject.activeInHierarchy)
                continue;

            int layerBit = 1 << hitCollider.gameObject.layer;
            if ((obstacleMask.value & layerBit) != 0)
            {
                ClearAllFocus();
                return;
            }

            if (_walkableLayer >= 0 && hitCollider.gameObject.layer == _walkableLayer)
            {
                // SphereCast 반경이 넓으면 낮은 위치의 버튼/물체보다 바닥을 먼저 감지할 수 있습니다.
                // 바닥은 시선 상호작용의 차단물이 아니므로 기록만 하고 다음 충돌을 계속 검사합니다.
                IsFloorValid = true;
                continue;
            }

            if ((targetMask.value & layerBit) == 0)
                continue;

            // 실제 Collider는 자식 메시 오브젝트에 있고 IInteractable은 부모에 붙는
            // 프리팹 구조를 지원합니다. 자식 콜라이더의 레이어도 그대로 검사합니다.
            InteractableItem item = hitCollider.GetComponentInParent<InteractableItem>();
            if (item == null || !item.isInteractable || !item.gameObject.activeInHierarchy)
                continue;

            // 거리 제한 예외는 엘리베이터 버튼 등 명시적으로 지정된 물체에만 적용합니다.
            // 일반 물체가 너무 멀리 있으면 그 뒤의 물체를 관통해 선택하지 않습니다.
            if (!item.ignorePlayerDistanceLimit && hits[i].distance > normalInteractionDistance)
            {
                ClearAllFocus();
                return;
            }

            ProcessTargetHover(item);
            return;
        }

        bool floorWasValid = IsFloorValid;
        ClearAllFocus();
        IsFloorValid = floorWasValid;
    }

    private void ProcessTargetHover(InteractableItem interactable)
    {
        if (interactable == null || !interactable.isInteractable)
        {
            ClearAllFocus();
            return;
        }

        Transform targetTransform = interactable.transform;
        if (CurrentHoverTarget == targetTransform)
        {
            if (!IsTargetReady)
            {
                _currentReadyTimer += Time.deltaTime;
                if (_currentReadyTimer >= readyTimeThreshold) IsTargetReady = true;
            }
        }
        else
        {
            ClearObjectFocus();
            CurrentHoverTarget = targetTransform;
            _currentReadyTimer = 0f;
            IsTargetReady = false;
        }

        if (_currentFocusedObject != interactable)
            ChangeObjectFocus(interactable);

        if (IsTargetReady)
        {
            if (!_isObjectReadyTriggered)
            {
                _isObjectReadyTriggered = true;
                _currentFocusedObject.OnReadyStateReached();
            }

            if (IsGripTriggered)
            {
                OnInteractTriggered?.Invoke(targetTransform);
                _currentFocusedObject.OnInteract();
                ClearObjectFocus();
                CurrentHoverTarget = null;
                _currentReadyTimer = 0f;
                IsTargetReady = false;
            }
        }
        else
        {
            _isObjectReadyTriggered = false;
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
using System;
using System.Collections;
using UnityEngine;

public enum PhoneState { Idle, Dropping, Dropped, Recovered, Calling, Success }

public class PhoneController : InteractableItem
{
    public static PhoneController Instance { get; private set; }

    [Header("상태 및 수치 설정")]
    public PhoneState currentState = PhoneState.Idle;
    public float callDuration = 10.0f;

    [Header("UI 참조")]
    public PhoneUIController phoneUI;

    [Header("낙하 연출 설정 (Drop)")]
    public float dropDuration = 0.8f;
    public Vector3 dropRotation = new Vector3(90f, 0f, 0f);

    [Header("카메라 연동 (왼손 위치)")]
    public Vector3 leftHandPosition = new Vector3(-1.75f, -0.7f, 2.3f);
    public Vector3 leftHandRotation = new Vector3(15f, 30f, 0f);
    [SerializeField] private bool showOnlyScreenInHand = true;

    [Header("오디오 설정")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string dropSlideClipName = "SND-049_PhoneSlide_OneShot";
    public string vibrationClipName = "SND-050_PhoneVibration_Loop";
    public string dialingClipName = "SND-053_PhoneDialTone_Loop";
    public string policeVoiceClipName = "SND-054_PoliceResponse_OneShot";
    public string phonePickupClipName = "SND-051_PhonePickup_OneShot";
    public string phoneFailClipName = "SND-052_PhoneFailBeep_OneShot";

    public event Action onCallSuccess;
    private bool _isInteracting = false;
    private Vector3 _phoneInitialLocalScale;
    private bool _hasCachedPhoneInitialScale;
    private Coroutine _phoneRaise;
    private Camera _handCamera;
    private RectTransform _heldScreen;
    private Quaternion _heldScreenRotation;
    private Vector3 _heldScreenCenter;
    private Vector2 _heldScreenSize;
    private float _raiseProgress;
    private readonly Vector3[] _screenCorners = new Vector3[4];
    private readonly System.Collections.Generic.List<Renderer> _hiddenPhoneRenderers =
        new System.Collections.Generic.List<Renderer>();

    private void Awake()
    {
        // 프리팹에 처음 설정된 휴대폰 로컬 스케일을 기준값으로 보존합니다.
        // 월드 스케일을 매번 환산하면 씬/카메라 부모 스케일 변화가 누적된 것처럼
        // 로컬 스케일이 달라질 수 있으므로, 원래 설정값을 그대로 복원합니다.
        CachePhoneInitialScale();

        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Item_Phone은 Player 프리팹 내부의 자식 오브젝트입니다.
        // DontDestroyOnLoad는 루트 오브젝트에 적용해야 하므로 플레이어 루트를 유지합니다.
        GameObject persistenceRoot = transform.root != null ? transform.root.gameObject : gameObject;
        if (persistenceRoot != null && persistenceRoot.scene.IsValid())
            DontDestroyOnLoad(persistenceRoot);

        // Phone은 처음에는 숨기되, ShowPhoneInHand로 다시 활성화된 뒤
        // Start에서 Idle 상태를 이유로 자신을 다시 비활성화하지 않도록 합니다.
        interactOnlyOnce = false;
        HidePhone();
    }

    public override void OnInteract()
    {
        // 휴대폰은 바닥에 떨어져 회수 가능한 상태일 때만 상호작용을 처리합니다.
        if (currentState != PhoneState.Dropped || !isInteractable)
            return;

        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
            return;

        base.OnInteract();
        OnPhoneReached();
    }

    private void CachePhoneInitialScale()
    {
        if (_hasCachedPhoneInitialScale) return;
        _phoneInitialLocalScale = transform.localScale;
        _hasCachedPhoneInitialScale = true;
    }

    private Camera GetItemCamera()
    {
        GameObject itemCamObj = GameObject.Find("Item_Camera");
        if (itemCamObj != null)
        {
            Camera cam = itemCamObj.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        return Camera.main;
    }

    public void ShowPhoneInHand()
    {
        StopPhoneRaise();
        RestorePhoneBody();
        gameObject.SetActive(true);
        isInteractable = false;

        Camera targetCam = GetItemCamera();
        if (targetCam != null)
        {
            transform.SetParent(null, true);
            DontDestroyOnLoad(gameObject);
            CachePhoneInitialScale();
            transform.localScale = _phoneInitialLocalScale * 0.5f;
            _handCamera = targetCam;
            Canvas phoneCanvas = phoneUI != null ? phoneUI.GetComponentInChildren<Canvas>(true) : null;
            _heldScreen = phoneCanvas != null ? phoneCanvas.transform as RectTransform : null;
            if (_heldScreen != null)
            {
                _heldScreen.GetWorldCorners(_screenCorners);
                _heldScreenSize = new Vector2(Vector3.Distance(_screenCorners[0], _screenCorners[3]),
                    Vector3.Distance(_screenCorners[0], _screenCorners[1]));
                _heldScreenCenter = transform.InverseTransformPoint((_screenCorners[0] + _screenCorners[2]) * 0.5f);
                _heldScreenRotation = Quaternion.Inverse(transform.rotation) * _heldScreen.rotation;
                if (showOnlyScreenInHand)
                    foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
                    {
                        if (renderer.forceRenderingOff || renderer.transform.IsChildOf(_heldScreen)) continue;
                        renderer.forceRenderingOff = true;
                        _hiddenPhoneRenderers.Add(renderer);
                    }
            }
            gameObject.layer = LayerMask.NameToLayer("FP_Item");
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = LayerMask.NameToLayer("FP_Item");
            }
            _phoneRaise = StartCoroutine(RaisePhone());
        }
    }

    private IEnumerator RaisePhone()
    {
        const float duration = 0.32f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            _raiseProgress = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            UpdateHeldPose();
            yield return null;
        }
        _raiseProgress = 1f;
        UpdateHeldPose();
        _phoneRaise = null;
    }

    private void LateUpdate()
    {
        UpdateHeldPose();
    }

    private void UpdateHeldPose()
    {
        if (_handCamera == null) return;
        Transform cameraTransform = _handCamera.transform;
        if (_heldScreen == null || _heldScreenSize.x < 0.0001f || _heldScreenSize.y < 0.0001f)
        {
            transform.SetPositionAndRotation(cameraTransform.position + cameraTransform.rotation *
                ((leftHandPosition + Vector3.down * (1f - _raiseProgress) * 0.65f) * 0.5f),
                cameraTransform.rotation * Quaternion.Euler(leftHandRotation.x, leftHandRotation.y, -180f));
            return;
        }

        float depth = Mathf.Max(_handCamera.nearClipPlane + 0.05f, leftHandPosition.z * 0.5f);
        Vector3 bottom = _handCamera.ViewportToWorldPoint(new Vector3(0f, 0f, depth));
        float viewportWidth = Vector3.Distance(bottom, _handCamera.ViewportToWorldPoint(new Vector3(1f, 0f, depth)));
        float viewportHeight = Vector3.Distance(bottom, _handCamera.ViewportToWorldPoint(new Vector3(0f, 1f, depth)));
        float fit = Mathf.Min(viewportWidth / (3f * _heldScreenSize.x), viewportHeight * 2f / (3f * _heldScreenSize.y));
        transform.localScale = _phoneInitialLocalScale * (0.5f * fit);
        Quaternion rotation = cameraTransform.rotation * Quaternion.Inverse(_heldScreenRotation);
        Vector3 center = _handCamera.ViewportToWorldPoint(new Vector3(0.205f, Mathf.Lerp(0.15f, 0.46f, _raiseProgress), depth));
        transform.SetPositionAndRotation(center - rotation * Vector3.Scale(_heldScreenCenter, transform.localScale), rotation);
    }

    private void StopPhoneRaise()
    {
        if (_phoneRaise == null) return;
        StopCoroutine(_phoneRaise);
        _phoneRaise = null;
    }

    public void HidePhone()
    {
        StopAllCoroutines();
        _phoneRaise = null;
        _handCamera = null;
        RestorePhoneBody();
        if (sfxSource != null)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
            sfxSource.clip = null;
        }
        if (phoneUI != null)
        {
            phoneUI.HideAllScreens();
        }
        gameObject.SetActive(false);
    }

    private void RestorePhoneBody()
    {
        foreach (Renderer renderer in _hiddenPhoneRenderers)
            if (renderer != null) renderer.forceRenderingOff = false;
        _hiddenPhoneRenderers.Clear();
    }

    public void TriggerDrop(Transform targetFloorPoint)
    {
        if (currentState != PhoneState.Idle) return;
        StopPhoneRaise();
        if (targetFloorPoint == null)
        {
            Debug.LogError("[PhoneController] 휴대폰 낙하 위치(phoneDropTarget)가 연결되지 않았습니다.", this);
            return;
        }

        gameObject.SetActive(true);
        _handCamera = null;
        RestorePhoneBody();
        StartCoroutine(DropRoutine(targetFloorPoint));
    }

    private IEnumerator DropRoutine(Transform targetFloorPoint)
    {
        currentState = PhoneState.Dropping;
        if (phoneUI != null) phoneUI.HideAllScreens();

        if (sfxSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(dropSlideClipName);
            if (clip != null) sfxSource.PlayOneShot(clip);
        }

        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(dropRotation);
        float timer = 0f;

        while (timer < dropDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / dropDuration);
            transform.position = Vector3.Lerp(startPos, targetFloorPoint.position, t);
            transform.rotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }

        transform.position = targetFloorPoint.position;
        transform.rotation = endRot;
        currentState = PhoneState.Dropped;

        int interactableLayer = LayerMask.NameToLayer("Interactable");
        if (interactableLayer >= 0)
        {
            gameObject.layer = interactableLayer;
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
                child.gameObject.layer = interactableLayer;
        }
        else
        {
            Debug.LogError("[PhoneController] 'Interactable' 레이어를 찾을 수 없습니다. PlayerController의 전용 휴대폰 탐색은 계속 사용할 수 있습니다.", this);
        }

        // 휴대폰의 루트/자식 Collider가 모두 탐색 대상이 되도록 복구합니다.
        Collider[] phoneColliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider phoneCollider in phoneColliders)
        {
            if (phoneCollider != null)
                phoneCollider.enabled = true;
        }

        Physics.SyncTransforms();

        HidingSpotManager hiding = HidingSpotManager.Instance;
        if (hiding == null || hiding.HasHiddenOnce)
            EnableDroppedPhoneRecovery();
        if (phoneUI != null) phoneUI.ShowLockDay4();
    }

    public void EnableDroppedPhoneRecovery()
    {
        if (currentState != PhoneState.Dropped) return;
        EnableInteractionWithLight();
        StartVibration();
    }

    private void StartVibration()
    {
        if (sfxSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(vibrationClipName);
            if (clip != null)
            {
                sfxSource.clip = clip;
                sfxSource.loop = true;
                if (!sfxSource.isPlaying) sfxSource.Play();
            }
        }
    }

    private void OnPhoneReached()
    {
        if (GameFlowManager.Instance != null)
        {
            GameStage current = GameFlowManager.Instance.currentStage;
            if (current < GameStage.Stage8_Intruder || current > GameStage.Stage13_Ending) return;
        }

        if (_isInteracting || currentState != PhoneState.Dropped) return;
        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        _isInteracting = true;
        isInteractable = false;

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.2f, transform.position);
        yield return new WaitForSeconds(0.1f);

        currentState = PhoneState.Recovered;

        if (sfxSource != null)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
            if (AudioManager.Instance != null)
            {
                AudioClip pickupClip = AudioManager.Instance.GetClip(phonePickupClipName);
                if (pickupClip != null) sfxSource.PlayOneShot(pickupClip);
            }
        }

        ShowPhoneInHand();
        _isInteracting = false;
        if (UIManager.Instance != null)
            StartCoroutine(UIManager.Instance.ShowGameplayHint("휴대폰을 찾았어. 다시 안전한 곳에 숨어서 신고하자.", 3.5f));
        StartCoroutine(WaitToCallRoutine());
    }

    private IEnumerator WaitToCallRoutine()
    {
        if (phoneUI != null) phoneUI.ShowDial112Screen();

        // [결함 3 픽스] 다이얼 화면이 스킵되지 않도록 플레이어에게 최소 2초의 시각적 대기 시간을 강제 보장함
        yield return new WaitForSeconds(2.0f);

        while (currentState == PhoneState.Recovered)
        {
            // 단독 씬 테스트 시 HidingSpotManager가 없어도 무한 대기하지 않도록 예외(null 체크) 강화
            bool isHiding = HidingSpotManager.Instance == null || HidingSpotManager.Instance.IsHiding;
            bool isFreeze = PlayerController.Instance == null || PlayerController.Instance.IsFreezeActive;
            if (isHiding && isFreeze) yield return StartCoroutine(CallRoutine());
            yield return null;
        }
    }

    private IEnumerator CallRoutine()
    {
        currentState = PhoneState.Calling;
        if (phoneUI != null) phoneUI.ShowCallingScreen();

            if (sfxSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(dialingClipName);
            if (clip != null) { sfxSource.clip = clip; sfxSource.loop = true; sfxSource.Play(); }
        }

        float timer = 0f;
        float failGraceTimer = 0f;

        while (timer < callDuration)
        {
            timer += Time.deltaTime;

            if (PlayerController.Instance != null && !PlayerController.Instance.IsFreezeActive)
            {
                failGraceTimer += Time.deltaTime;
                if (failGraceTimer > 0.5f)
                {
                    if (sfxSource != null)
                    {
                        sfxSource.Stop();
                        sfxSource.loop = false;
                        if (AudioManager.Instance != null)
                        {
                            AudioClip failClip = AudioManager.Instance.GetClip(phoneFailClipName);
                            if (failClip != null) sfxSource.PlayOneShot(failClip);
                        }
                    }

                    currentState = PhoneState.Recovered;
                    if (phoneUI != null) phoneUI.ShowDial112Screen();
                    if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.4f, transform.position);

                    yield return new WaitForSeconds(1.0f);
                    StartCoroutine(WaitToCallRoutine());
                    yield break;
                }
            }
            else
            {
                failGraceTimer = 0f;
            }
            yield return null;
        }

        currentState = PhoneState.Success;
        if (phoneUI != null) phoneUI.ShowPoliceCallScreen();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();

        AudioClip breath = AudioManager.Instance != null
            ? AudioManager.Instance.GetClip("SND-019_NervousBreath_Loop_Layer") : null;
        if (sfxSource != null && breath != null)
        {
            sfxSource.clip = breath;
            sfxSource.loop = true;
            sfxSource.Play();
        }
        yield return StartCoroutine(ShowCallDialogueLine(NarrativeData.Call112_Opening));
        yield return StartCoroutine(ShowCallDialogueLine(NarrativeData.Call112_ChoiceA_Line));
        yield return StartCoroutine(ShowCallDialogueLine(NarrativeData.Call112_ChoiceA_Reply));
        if (sfxSource != null)
        {
            sfxSource.Stop();
            sfxSource.loop = false;
        }
        onCallSuccess?.Invoke();
        yield return new WaitForSeconds(0.5f);
        HidePhone();
    }

    private IEnumerator ShowCallDialogueLine(string message)
    {
        float duration = Mathf.Clamp(message.Length * 0.08f, 2.0f, 5.5f);
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(message, duration));
        else
            yield return new WaitForSecondsRealtime(duration);
    }

}

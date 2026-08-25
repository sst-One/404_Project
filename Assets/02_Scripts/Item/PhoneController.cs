using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PhoneState { Idle, Dropping, Dropped, Recovered, Calling, Success }

[RequireComponent(typeof(InteractableItem))]
public class PhoneController : MonoBehaviour
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

    [Header("최적화된 오디오 설정 (단 2개)")]
    public AudioSource sfxSource;
    public AudioSource voiceSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string dropSlideClipName = "SND-049_PhoneSlide_OneShot";
    public string vibrationClipName = "SND-050_PhoneVibration_Loop";
    public string dialingClipName = "SND-053_PhoneDialTone_Loop";
    public string policeVoiceClipName = "SND-054_PoliceResponse_OneShot";
    public string phonePickupClipName = "SND-051_PhonePickup_OneShot";
    public string phoneFailClipName = "SND-052_PhoneFailBeep_OneShot";

    [Header("성공 이벤트")]
    public UnityEvent onCallSuccess;

    private bool _isInteracting = false;
    private InteractableItem _interactable;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _interactable = GetComponent<InteractableItem>();

        if (_interactable != null)
        {
            _interactable.interactOnlyOnce = false;
            _interactable.isInteractable = (currentState == PhoneState.Dropped);
            _interactable.onInteractEvent.RemoveAllListeners();
            _interactable.onInteractEvent.AddListener(OnPhoneReached);
        }

        if (currentState == PhoneState.Dropped)
        {
            StartVibration();
            if (phoneUI != null) phoneUI.ShowDefaultScreen();
        }
        else
        {
            HidePhone();
        }
    }

    // [핫픽스] 1인칭 오버레이 카메라(Item_Camera)를 직접 탐색
    private Camera GetItemCamera()
    {
        GameObject itemCamObj = GameObject.Find("Item_Camera");
        if (itemCamObj != null)
        {
            Camera cam = itemCamObj.GetComponent<Camera>();
            if (cam != null) return cam;
        }
        return Camera.main; // 최후의 보루: 못 찾으면 메인 카메라 반환
    }

    public void ShowPhoneInHand()
    {
        gameObject.SetActive(true);
        if (_interactable != null) _interactable.isInteractable = false;

        Camera targetCam = GetItemCamera();
        if (targetCam != null)
        {
            // 직접 1인칭 카메라의 자식으로 들어감
            transform.SetParent(targetCam.transform);

            gameObject.layer = LayerMask.NameToLayer("FP_Item");
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = LayerMask.NameToLayer("FP_Item");
            }

            // 인스펙터에 지정된 고유 로컬 좌표/회전값으로 렌더링 위치 복원
            transform.localPosition = leftHandPosition;
            transform.localRotation = Quaternion.Euler(leftHandRotation);
        }
    }

    public void HidePhone()
    {
        gameObject.SetActive(false);
    }

    public void TriggerDrop(Transform targetFloorPoint)
    {
        if (currentState != PhoneState.Idle) return;
        gameObject.SetActive(true);
        StartCoroutine(DropRoutine(targetFloorPoint));
    }

    private IEnumerator DropRoutine(Transform targetFloorPoint)
    {
        currentState = PhoneState.Dropping;
        if (phoneUI != null) phoneUI.TurnOffScreen();

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

        gameObject.layer = LayerMask.NameToLayer("Interactable");
        foreach (Transform child in GetComponentsInChildren<Transform>(true))
        {
            child.gameObject.layer = LayerMask.NameToLayer("Interactable");
        }

        if (_interactable != null) _interactable.isInteractable = true;

        StartVibration();
        if (phoneUI != null) phoneUI.ShowDefaultScreen();
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

    public void OnPhoneReached()
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
        if (_interactable != null) _interactable.isInteractable = false;

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
        StartCoroutine(WaitToCallRoutine());
    }

    private IEnumerator WaitToCallRoutine()
    {
        if (phoneUI != null) phoneUI.ShowDial112();

        while (currentState == PhoneState.Recovered)
        {
            bool isHiding = HidingSpotManager.Instance != null && HidingSpotManager.Instance.IsHiding;
            bool isFreeze = PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive;

            if (isHiding && isFreeze) yield return StartCoroutine(CallRoutine());
            yield return null;
        }
    }

    private IEnumerator CallRoutine()
    {
        currentState = PhoneState.Calling;
        if (phoneUI != null) phoneUI.ShowCalling();

        if (sfxSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(dialingClipName);
            if (clip != null) { sfxSource.clip = clip; sfxSource.loop = true; sfxSource.Play(); }
        }

        float timer = 0f;
        while (timer < callDuration)
        {
            timer += Time.deltaTime;

            if (PlayerController.Instance == null || !PlayerController.Instance.IsFreezeActive)
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
                if (phoneUI != null) phoneUI.ShowDial112();
                if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.4f, transform.position);

                yield return new WaitForSeconds(1.0f);
                StartCoroutine(WaitToCallRoutine());
                yield break;
            }
            yield return null;
        }

        currentState = PhoneState.Success;
        if (phoneUI != null) phoneUI.ShowPoliceCall();
        if (sfxSource != null && sfxSource.isPlaying) sfxSource.Stop();

        float voiceLength = 4.0f;
        if (voiceSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(policeVoiceClipName);
            if (clip != null) { voiceSource.clip = clip; voiceSource.Play(); voiceLength = clip.length; }
        }

        onCallSuccess?.Invoke();
        yield return new WaitForSeconds(voiceLength);
        if (phoneUI != null) phoneUI.TurnOffScreen();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PhoneState { Idle, Dropping, Dropped, Recovered, Calling, Success }

[RequireComponent(typeof(InteractableItem))]
public class PhoneController : MonoBehaviour
{
    [Header("상태 및 수치 설정")]
    public PhoneState currentState = PhoneState.Idle; // 낙하 전 대기 상태 추가
    public float recoverHoldTime = 1.0f;
    public float callDuration = 10.0f;

    [Header("낙하 연출 설정 (Drop)")]
    public float dropDuration = 0.8f;
    public Vector3 dropRotation = new Vector3(90f, 0f, 0f);

    [Header("카메라 연동 (왼손 위치)")]
    public Vector3 leftHandPosition = new Vector3(-1.75f, -0.7f, 2.3f);
    public Vector3 leftHandRotation = new Vector3(15f, 30f, 0f);

    [Header("오디오 설정 (AudioSources)")]
    public AudioSource vibrationSource;
    public AudioSource dialingSource;
    public AudioSource policeVoiceSource;
    public AudioSource phonePickupSource;
    public AudioSource phoneFailSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string dropSlideClipName = "SND-049_PhoneSlide_OneShot"; // 미끄러지는 소리
    public string vibrationClipName = "SND-050_PhoneVibration_Loop";
    public string dialingClipName = "SND-053_PhoneDialTone_Loop";
    public string policeVoiceClipName = "SND-054_PoliceResponse_OneShot";
    public string phonePickupClipName = "SND-051_PhonePickup_OneShot";
    public string phoneFailClipName = "SND-052_PhoneFailBeep_OneShot";

    [Header("성공 이벤트")]
    public UnityEvent onCallSuccess;

    private Coroutine _actionCoroutine;
    private bool _isInteracting = false;
    private InteractableItem _interactable;
    private Camera _mainCamera;

    private void Start()
    {
        _interactable = GetComponent<InteractableItem>();

        if (_interactable != null)
        {
            _interactable.interactOnlyOnce = false;
            // 낙하하기 전까지는 상호작용 불가
            _interactable.isInteractable = (currentState == PhoneState.Dropped);
        }

        // 씬 시작 시 이미 떨어져 있는 상태라면 진동 시작
        if (currentState == PhoneState.Dropped)
        {
            StartVibration();
        }
    }

    private Camera GetPlayerCamera()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Camera playerCam = playerObj.GetComponentInChildren<Camera>();
            if (playerCam != null) return playerCam;
        }
        return Camera.main;
    }

    // [신규 로직] 외부 컨트롤러(Stage8_11)에서 호출하여 핸드폰을 바닥으로 떨어뜨리는 함수
    public void TriggerDrop(Transform targetFloorPoint)
    {
        if (currentState != PhoneState.Idle) return;
        StartCoroutine(DropRoutine(targetFloorPoint));
    }

    private IEnumerator DropRoutine(Transform targetFloorPoint)
    {
        currentState = PhoneState.Dropping;

        // 떨어지는 사운드 재생
        if (vibrationSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(dropSlideClipName);
            if (clip != null) vibrationSource.PlayOneShot(clip);
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

        // 바닥에 안착 후 Dropped 상태 전환 및 진동 시작
        currentState = PhoneState.Dropped;
        if (_interactable != null) _interactable.isInteractable = true;

        StartVibration();
    }

    private void StartVibration()
    {
        if (vibrationSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(vibrationClipName);
            if (clip != null)
            {
                vibrationSource.clip = clip;
                vibrationSource.loop = true;
                if (!vibrationSource.isPlaying) vibrationSource.Play();
            }
        }
    }

    public void OnPhoneReached()
    {
        if (_isInteracting || currentState != PhoneState.Dropped) return;
        _actionCoroutine = StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        _isInteracting = true;

        float timer = 0f;
        while (timer < recoverHoldTime)
        {
            if (PlayerController.Instance == null || !PlayerController.Instance.IsGripHeld)
            {
                _isInteracting = false;
                if (_interactable != null) _interactable.isInteractable = true;
                yield break;
            }
            timer += Time.deltaTime;

            if (StateManager.Instance != null)
                StateManager.Instance.AddNoise(0.15f * Time.deltaTime, transform.position);

            yield return null;
        }

        currentState = PhoneState.Recovered;

        if (vibrationSource != null && vibrationSource.isPlaying) vibrationSource.Stop();

        if (phonePickupSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phonePickupClipName);
            if (clip != null) phonePickupSource.PlayOneShot(clip);
        }

        if (_interactable != null) _interactable.isInteractable = false;

        _mainCamera = GetPlayerCamera();
        if (_mainCamera != null)
        {
            transform.SetParent(_mainCamera.transform);
            transform.localPosition = leftHandPosition;
            transform.localRotation = Quaternion.Euler(leftHandRotation);
        }

        _isInteracting = false;
        StartCoroutine(WaitToCallRoutine());
    }

    // [핵심 수정] 시선 고정(Gaze) 조건 삭제. 휴대폰을 주운 상태면 손뻗기(Reach/Click) 입력만으로 바로 112 신고 시작
    private IEnumerator WaitToCallRoutine()
    {
        Debug.Log("[PhoneController] 112 신고 대기 상태. 손 뻗기(Reach/Click) 입력 시 전화 연결 시도.");

        while (currentState == PhoneState.Recovered)
        {
            // IsFocused 조건 제거. PlayerController의 IsGripTriggered(손 뻗기/클릭)만 감지
            if (PlayerController.Instance != null && PlayerController.Instance.IsGripTriggered)
            {
                yield return StartCoroutine(CallRoutine());
            }
            yield return null;
        }
    }

    private IEnumerator CallRoutine()
    {
        currentState = PhoneState.Calling;

        if (dialingSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(dialingClipName);
            if (clip != null)
            {
                dialingSource.clip = clip;
                dialingSource.Play();
            }
        }

        float timer = 0f;
        while (timer < callDuration)
        {
            timer += Time.deltaTime;

            // 통화 중에는 멈추기(Origin Freeze) 상태를 엄격히 유지해야 함
            if (PlayerController.Instance == null || !PlayerController.Instance.IsFreezeActive)
            {
                if (dialingSource != null && dialingSource.isPlaying) dialingSource.Stop();

                if (phoneFailSource != null && AudioManager.Instance != null)
                {
                    AudioClip clip = AudioManager.Instance.GetClip(phoneFailClipName);
                    if (clip != null) phoneFailSource.PlayOneShot(clip);
                }

                currentState = PhoneState.Recovered;
                if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.4f, transform.position);

                StartCoroutine(WaitToCallRoutine());
                yield break;
            }
            yield return null;
        }

        currentState = PhoneState.Success;
        if (dialingSource != null && dialingSource.isPlaying) dialingSource.Stop();

        float voiceLength = 4.0f;
        if (policeVoiceSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(policeVoiceClipName);
            if (clip != null)
            {
                policeVoiceSource.clip = clip;
                policeVoiceSource.Play();
                voiceLength = clip.length;
            }
        }

        onCallSuccess?.Invoke();
        StartCoroutine(SelfDestructRoutine(voiceLength));
    }

    private IEnumerator SelfDestructRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("[PhoneController] 통화 종료. 휴대폰 오브젝트 파괴.");
        transform.SetParent(null);
        Destroy(gameObject);
    }
}
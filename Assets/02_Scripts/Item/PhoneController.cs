using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PhoneState { Idle, Dropping, Dropped, Recovered, Calling, Success }

[RequireComponent(typeof(InteractableItem))]
public class PhoneController : MonoBehaviour
{
    [Header("상태 및 수치 설정")]
    public PhoneState currentState = PhoneState.Idle;
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
    private Camera _mainCamera;

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

    public void TriggerDrop(Transform targetFloorPoint)
    {
        if (currentState != PhoneState.Idle) return;
        StartCoroutine(DropRoutine(targetFloorPoint));
    }

    private IEnumerator DropRoutine(Transform targetFloorPoint)
    {
        currentState = PhoneState.Dropping;

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
        StartCoroutine(RecoverRoutine());
    }

    private IEnumerator RecoverRoutine()
    {
        _isInteracting = true;
        if (_interactable != null) _interactable.isInteractable = false;

        if (StateManager.Instance != null)
            StateManager.Instance.AddNoise(0.2f, transform.position);

        yield return new WaitForSeconds(0.1f);

        currentState = PhoneState.Recovered;

        if (vibrationSource != null && vibrationSource.isPlaying) vibrationSource.Stop();

        if (phonePickupSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(phonePickupClipName);
            if (clip != null) phonePickupSource.PlayOneShot(clip);
        }

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

    private IEnumerator WaitToCallRoutine()
    {
        Debug.Log("[PhoneController] 휴대폰 획득. 1.5초 후 112 자동 연결 시도 (Origin Freeze 유지 요망)");

        // [핵심 핫픽스] Reach 입력 락(Lock)을 제거하고, 1.5초의 숨 고르기 버퍼 후 즉각 연결 시도
        yield return new WaitForSeconds(1.5f);

        if (currentState == PhoneState.Recovered)
        {
            yield return StartCoroutine(CallRoutine());
        }
    }

    private IEnumerator CallRoutine()
    {
        currentState = PhoneState.Calling;
        Debug.Log("[PhoneController] 112 발신 중... 움직임이 감지되면 통화가 끊어집니다.");

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

            // [판정] 움직이거나 숨을 참지 못해 Origin Freeze가 풀린 경우
            if (PlayerController.Instance == null || !PlayerController.Instance.IsFreezeActive)
            {
                Debug.LogWarning("[PhoneController] 미세 움직임 감지! 112 연결 실패. 재발신 대기.");
                if (dialingSource != null && dialingSource.isPlaying) dialingSource.Stop();

                if (phoneFailSource != null && AudioManager.Instance != null)
                {
                    AudioClip clip = AudioManager.Instance.GetClip(phoneFailClipName);
                    if (clip != null) phoneFailSource.PlayOneShot(clip);
                }

                currentState = PhoneState.Recovered;
                if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.4f, transform.position); // 실패 시 페널티 소음 발생

                // 1초 패널티 후 1.5초 뒤 다시 자동 발신 무한 루프
                yield return new WaitForSeconds(1.0f);
                StartCoroutine(WaitToCallRoutine());
                yield break;
            }
            yield return null;
        }

        currentState = PhoneState.Success;
        Debug.Log("[PhoneController] 112 통화 10초 유지 성공. 경찰 응답 수신 중.");

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
        transform.SetParent(null);
        Destroy(gameObject);
    }
}
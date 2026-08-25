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
            // 바닥에 떨어져 진동 중일 때 화면을 켜서 시각적 단서 제공
            if (phoneUI != null) phoneUI.ShowDefaultScreen();
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

        // 떨어지는 도중에는 화면이 꺼짐
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
        if (_interactable != null) _interactable.isInteractable = true;

        StartVibration();

        // 바닥에 닿은 후 화면이 켜짐
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
            if (current < GameStage.Stage8_Intruder || current > GameStage.Stage13_Ending)
            {
                return;
            }
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

        Camera cam = GetPlayerCamera();
        if (cam != null)
        {
            transform.SetParent(cam.transform);
            transform.localPosition = leftHandPosition;
            transform.localRotation = Quaternion.Euler(leftHandRotation);
        }

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

            if (isHiding && isFreeze)
            {
                yield return StartCoroutine(CallRoutine());
            }
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
            if (clip != null)
            {
                sfxSource.clip = clip;
                sfxSource.loop = true;
                sfxSource.Play();
            }
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
            if (clip != null)
            {
                voiceSource.clip = clip;
                voiceSource.Play();
                voiceLength = clip.length;
            }
        }

        onCallSuccess?.Invoke();
        StartCoroutine(SelfDestructRoutine(voiceLength));
    }

    private IEnumerator SelfDestructRoutine(float delay)
    {
        // 종료 연출 후 화면 끄기
        yield return new WaitForSeconds(delay);
        if (phoneUI != null) phoneUI.TurnOffScreen();
        transform.SetParent(null);
        Destroy(gameObject);
    }
}
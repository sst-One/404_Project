// 3. PhoneController.cs
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public enum PhoneState { Dropped, Recovered, Calling, Success }

public class PhoneController : MonoBehaviour
{
    [Header("상태 및 수치 설정")]
    public PhoneState currentState = PhoneState.Dropped;
    public float recoverHoldTime = 1.0f;
    public float callDuration = 10.0f;

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
    public string vibrationClipName = "SND-049_PhoneSlide_OneShot";
    public string dialingClipName = "";
    public string policeVoiceClipName = "";
    public string phonePickupClipName = "";
    public string phoneFailClipName = "";

    [Header("성공 이벤트")]
    public UnityEvent onCallSuccess;

    private Coroutine _actionCoroutine;
    private bool _isInteracting = false;
    private PlayerInputProvider _inputProvider;
    private InteractableItem _interactable;
    private Camera _mainCamera;

    private void Start()
    {
        _inputProvider = FindObjectOfType<PlayerInputProvider>();
        _interactable = GetComponent<InteractableItem>();

        if (_interactable != null)
        {
            _interactable.interactOnlyOnce = false;
        }

        if (vibrationSource != null && currentState == PhoneState.Dropped && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(vibrationClipName);
            if (clip != null)
            {
                vibrationSource.clip = clip;
                vibrationSource.Play();
            }
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
            if (_inputProvider == null || !_inputProvider.IsGripHeld)
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

            if (_inputProvider == null || !_inputProvider.IsFreezeActive)
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

    private IEnumerator WaitToCallRoutine()
    {
        Debug.Log("[PhoneController] 신고 대기 상태. 휴대폰을 바라보고 스페이스바(Freeze)를 누르십시오.");

        while (currentState == PhoneState.Recovered)
        {
            if (_interactable != null && _interactable.IsFocused && _inputProvider != null && _inputProvider.IsFreezeActive)
            {
                yield return StartCoroutine(CallRoutine());
            }
            yield return null;
        }
    }

    private IEnumerator SelfDestructRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        Debug.Log("[PhoneController] 통화 종료. 휴대폰 오브젝트를 파괴합니다.");
        transform.SetParent(null);
        Destroy(gameObject);
    }
}
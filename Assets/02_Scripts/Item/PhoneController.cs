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

    [Header("오디오 설정")]
    public AudioSource vibrationSource;
    public AudioSource dialingSource;
    public AudioSource policeVoiceSource;

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

        // [핵심 픽스] 회수 실패 시 재시도가 가능하도록 1회용 속성을 스크립트에서 강제 해제
        if (_interactable != null)
        {
            _interactable.interactOnlyOnce = false;
        }

        if (vibrationSource != null && currentState == PhoneState.Dropped) vibrationSource.Play();
    }

    // [신규 추가] 플레이어 객체를 찾아 메인 카메라를 동적으로 반환하는 안전장치
    private Camera GetPlayerCamera()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Camera playerCam = playerObj.GetComponentInChildren<Camera>();
            if (playerCam != null) return playerCam;
        }

        // Player 태그를 찾지 못했을 경우의 최후 방어선
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
        Debug.Log("[PhoneController] 휴대폰 회수 시도 중... (Reach 유지 필요)");

        float timer = 0f;
        while (timer < recoverHoldTime)
        {
            if (_inputProvider == null || !_inputProvider.IsGripHeld)
            {
                Debug.LogWarning("[PhoneController] 회수 실패: 손 뻗기(Reach) 유지가 끊겼습니다.");
                _isInteracting = false;

                // 회수에 실패했으므로 다시 상호작용할 수 있도록 풀어줌
                if (_interactable != null) _interactable.isInteractable = true;
                yield break;
            }
            timer += Time.deltaTime;

            if (StateManager.Instance != null)
                StateManager.Instance.AddNoise(0.15f * Time.deltaTime, transform.position);

            yield return null;
        }

        Debug.Log("[PhoneController] 휴대폰 회수 성공!");
        currentState = PhoneState.Recovered;
        if (vibrationSource != null) vibrationSource.Stop();

        // [핵심 추가] 회수 성공 즉시 향후 마우스 클릭(Reach)을 통한 중복 상호작용 영구 차단
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

    private IEnumerator CallRoutine()
    {
        currentState = PhoneState.Calling;
        Debug.Log($"[PhoneController] 112 신고 연결 중... ({callDuration}초간 스페이스바 유지 필요)");

        if (dialingSource != null) dialingSource.Play();

        float timer = 0f;
        while (timer < callDuration)
        {
            timer += Time.deltaTime;

            if (_inputProvider == null || !_inputProvider.IsFreezeActive)
            {
                Debug.LogWarning("[PhoneController] 신고 실패: 스페이스바(Freeze) 입력을 놓쳤습니다!");
                if (dialingSource != null) dialingSource.Stop();

                currentState = PhoneState.Recovered;
                if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.4f, transform.position);

                StartCoroutine(WaitToCallRoutine());
                yield break;
            }
            yield return null;
        }

        Debug.Log("[PhoneController] 112 신고 성공! 경찰 연결 완료.");
        currentState = PhoneState.Success;
        if (dialingSource != null) dialingSource.Stop();
        if (policeVoiceSource != null) policeVoiceSource.Play();

        onCallSuccess?.Invoke();

        float voiceLength = (policeVoiceSource != null && policeVoiceSource.clip != null) ? policeVoiceSource.clip.length : 4.0f;
        StartCoroutine(SelfDestructRoutine(voiceLength));
    }

    private IEnumerator SelfDestructRoutine(float delay)
    {
        // 경찰 음성이 끝날 때까지 대기
        yield return new WaitForSeconds(delay);

        Debug.Log("[PhoneController] 통화 종료. 휴대폰 오브젝트를 파괴합니다.");

        // 카메라 종속 해제 후 오브젝트 완전 파괴
        transform.SetParent(null);
        Destroy(gameObject);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class PhoneController : MonoBehaviour, IInteractable
{
    public enum PhoneState { Dropped, Located, Recovered, Calling, Result }

    [Header("휴대폰 상태 (SYS-008)")]
    public PhoneState currentState = PhoneState.Dropped;

    [Header("신고 파라미터 (PARAM-035)")]
    public float callConnectTime = 10.0f;
    public float freezeGracePeriod = 1.5f; // 인간 반응 속도를 고려한 유예 시간

    [Header("이벤트 연결")]
    public UnityEvent onPhoneRecovered;
    public UnityEvent onCallSuccess;
    public UnityEvent onCallFailed;

    public void OnFocusEnter()
    {
        if (currentState == PhoneState.Dropped || currentState == PhoneState.Located)
        {
            Debug.Log("[PhoneController] 휴대폰 발견 (Located).");
            currentState = PhoneState.Located;
        }
    }

    public void OnFocusExit() { }
    public void OnReadyStateReached() { }

    public void OnInteract()
    {
        if (currentState == PhoneState.Located)
        {
            RecoverPhone();
        }
        else if (currentState == PhoneState.Recovered)
        {
            StartCallSequence();
        }
    }

    public void OnLean() { }

    // PhoneController.cs 내부의 RecoverPhone 메서드 수정
    private void RecoverPhone()
    {
        currentState = PhoneState.Recovered;
        Debug.Log("[PhoneController] 휴대폰 회수. UI 없이 즉시 신고 모드 진입. (스페이스바 유지 필요)");

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.5f);

        // UI 호출 로직을 건너뛰고 바로 신고 시퀀스(통화) 시작
        StartCallSequence();

        onPhoneRecovered?.Invoke();

        // 휴대폰 외형 감추기
        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null) renderer.enabled = false;

        // 콜라이더를 꺼서 중복 상호작용 방지
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    private void StartCallSequence()
    {
        currentState = PhoneState.Calling;
        Debug.Log($"[PhoneController] 112 신고 연결 중... {freezeGracePeriod}초 안에 스페이스바(Origin Freeze)를 누르고 유지하십시오.");
        StartCoroutine(CallRoutine());
    }

    private IEnumerator CallRoutine()
    {
        float timer = 0f;

        while (timer < callConnectTime)
        {
            timer += Time.deltaTime;

            // 유예 시간이 지난 이후부터만 실패 조건을 검사합니다.
            if (timer > freezeGracePeriod && InputManager.Instance != null)
            {
                bool isFreezing = InputManager.Instance.GetInput().IsFreezing();
                if (!isFreezing)
                {
                    Debug.LogWarning("[PhoneController] 통화 중 움직임 감지! 신고 실패.");
                    currentState = PhoneState.Recovered; // 다시 Reach하여 재시도 가능
                    onCallFailed?.Invoke();
                    yield break;
                }
            }
            yield return null;
        }

        currentState = PhoneState.Result;
        Debug.Log("[PhoneController] 112 신고 접수 완료! (경찰 출동 대기)");
        onCallSuccess?.Invoke();
    }
}
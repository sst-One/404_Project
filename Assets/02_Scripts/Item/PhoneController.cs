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

    private void RecoverPhone()
    {
        currentState = PhoneState.Recovered;
        Debug.Log("[PhoneController] 휴대폰 회수 성공. 다시 좌클릭(Reach)하여 112 신고를 시작하십시오.");

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.5f);

        onPhoneRecovered?.Invoke();
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
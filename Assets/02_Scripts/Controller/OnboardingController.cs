using System;
using System.Collections;
using UnityEngine;

public class OnboardingController : MonoBehaviour
{
    public static OnboardingController Instance { get; private set; }

    private enum OnboardingState { None, PermissionRequest, Calibration, Tutorial, Complete }
    private OnboardingState _currentState = OnboardingState.None;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // MainMenuController에서 게임 시작 버튼을 누를 때 호출됨
    public void StartOnboarding(Action onComplete)
    {
        StartCoroutine(ProcessOnboardingFlow(onComplete));
    }

    private IEnumerator ProcessOnboardingFlow(Action onComplete)
    {
        // 1. 권한 요청 대기 (SCN-001 / FEAT-027)
        _currentState = OnboardingState.PermissionRequest;
        Debug.Log("[Onboarding] 1단계: 카메라/마이크 권한을 확인합니다... (2초 대기)");
        yield return new WaitForSecondsRealtime(2.0f);

        // 2. 캘리브레이션 진입 (FEAT-033)
        _currentState = OnboardingState.Calibration;
        Debug.Log("[Onboarding] 2단계: 캘리브레이션 단계 진입.");

        if (CalibrationManager.Instance != null)
        {
            bool isCalibrated = false;

            // 메모리 누수 방지를 위한 로컬 액션 정의
            Action onCalibSuccess = () => isCalibrated = true;

            CalibrationManager.Instance.OnCalibrationSuccess += onCalibSuccess;

            // 통제권 획득 후 명시적 실행
            CalibrationManager.Instance.StartCalibration();

            yield return new WaitUntil(() => isCalibrated);

            // 이벤트 구독 안전 해제
            CalibrationManager.Instance.OnCalibrationSuccess -= onCalibSuccess;
        }
        else
        {
            Debug.LogError("[Onboarding] CalibrationManager가 감지되지 않아 1초 후 강제 진행합니다.");
            yield return new WaitForSecondsRealtime(1.0f);
        }

        // 3. 튜토리얼 텍스트 연출 (FEAT-037)
        _currentState = OnboardingState.Tutorial;
        Debug.Log("[Onboarding] 3단계: 기본 인터랙션(Gaze, Reach, Lean, Freeze) 튜토리얼 가이드 대기.");

        // TODO: 향후 UIManager와 연동하여 4가지 실입력을 검증하는 무한 대기 루프가 삽입될 구간입니다.
        // 현재는 E2E 통합 테스트를 위해 3초 대기만 수행하고 넘어갑니다.
        yield return new WaitForSecondsRealtime(3.0f);

        // 4. 완료 후 콜백 실행 (SCN-002 페이드 연출 및 씬 로드)
        _currentState = OnboardingState.Complete;
        Debug.Log("[Onboarding] 온보딩 완료. 씬 전환 시퀀스로 넘어갑니다.");

        onComplete?.Invoke();
    }
}
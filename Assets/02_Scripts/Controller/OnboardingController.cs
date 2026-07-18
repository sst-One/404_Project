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
        // 1. 권한 요청 대기 (SCN-001)
        _currentState = OnboardingState.PermissionRequest;
        Debug.Log("[Onboarding] 1단계: 카메라/마이크 권한을 확인합니다...");
        yield return new WaitForSecondsRealtime(2.0f);

        // 2. 캘리브레이션 진입
        _currentState = OnboardingState.Calibration;
        Debug.Log("[Onboarding] 2단계: 캘리브레이션 단계 진입.");

        bool isCalibrated = false;
        if (CalibrationManager.Instance != null)
        {
            CalibrationManager.Instance.OnCalibrationSuccess += () => isCalibrated = true;
            CalibrationManager.Instance.StartCalibration();
            yield return new WaitUntil(() => isCalibrated);
        }
        else
        {
            Debug.LogWarning("[Onboarding] CalibrationManager가 감지되지 않아 임시 대기합니다.");
            yield return new WaitForSecondsRealtime(1.0f);
        }

        // 3. 튜토리얼 텍스트 연출
        _currentState = OnboardingState.Tutorial;
        Debug.Log("[Onboarding] 3단계: 기본 인터랙션(Gaze, Reach, Lean, Freeze) 튜토리얼 가이드 출력.");
        yield return new WaitForSecondsRealtime(3.0f);

        // 4. 완료 후 콜백 실행 (페이드 연출 및 씬 로드)
        _currentState = OnboardingState.Complete;
        Debug.Log("[Onboarding] 온보딩 완료. 씬 전환 시퀀스로 넘어갑니다.");

        onComplete?.Invoke();
    }
}
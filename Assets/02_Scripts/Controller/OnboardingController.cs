using System.Collections;
using UnityEngine;

public class OnboardingController : MonoBehaviour
{
    private enum OnboardingState { PermissionRequest, Calibration, Tutorial, Complete }
    private OnboardingState _currentState = OnboardingState.PermissionRequest;

    private void Start()
    {
        // 게임 시작 시 GameFlowManager의 상태를 Title로 고정
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameStage.Title)
        {
            return;
        }

        StartCoroutine(ProcessOnboardingFlow());
    }

    private IEnumerator ProcessOnboardingFlow()
    {
        // 1. 권한 요청 가상 대기 (SCN-001)
        Debug.Log("[Onboarding] 1단계: 카메라/마이크 권한을 확인합니다...");
        yield return new WaitForSeconds(2.0f);

        // 2. 캘리브레이션 진입
        _currentState = OnboardingState.Calibration;
        Debug.Log("[Onboarding] 2단계: 캘리브레이션 단계 진입.");

        bool isCalibrated = false;
        CalibrationManager.Instance.OnCalibrationSuccess += () => isCalibrated = true;
        CalibrationManager.Instance.StartCalibration();

        yield return new WaitUntil(() => isCalibrated);

        // 3. 튜토리얼 텍스트 연출 (추후 UI 캔버스와 연동)
        _currentState = OnboardingState.Tutorial;
        Debug.Log("[Onboarding] 3단계: 기본 인터랙션(Gaze, Reach, Lean, Freeze) 튜토리얼 가이드 출력.");
        yield return new WaitForSeconds(3.0f); // 튜토리얼 UI 확인 시간

        // 4. 본 게임 진입
        _currentState = OnboardingState.Complete;
        Debug.Log("[Onboarding] 온보딩 완료. Stage 1 엘리베이터로 진입합니다.");

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage1_Elevator);
        }
    }
}
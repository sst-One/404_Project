using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("회전 대상 설정")]
    public Transform playerBody;

    [Header("Vision AI 360도 회전 설정 (조이스틱 방식)")]
    [Tooltip("화면 회전 속도 (기존 200 -> 80으로 대폭 하향하여 확확 돌아가는 현상 차단)")]
    public float headRotationSpeed = 80f;

    [Tooltip("상하 회전 최대 제한 (목 꺾임 방지)")]
    public float maxPitchAngle = 60f;

    [Tooltip("데드존 반경 (기존 0.04 -> 0.08로 두 배 상향하여 웹캠 미세 떨림 100% 흡수)")]
    public float deadzoneRadius = 0.08f;

    [Tooltip("멈출 때의 부드러움 (기존 0.05 -> 0.15로 상향하여 기계적인 꺾임 제거)")]
    public float rotationSmoothTime = 0.15f;

    private float _targetYaw = 0f;
    private float _targetPitch = 0f;
    private float _currentYaw = 0f;
    private float _currentPitch = 0f;

    private float _yawVelocity;
    private float _pitchVelocity;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerBody != null)
        {
            _targetYaw = playerBody.localEulerAngles.y;
            _currentYaw = _targetYaw;
        }
    }

    private void Update()
    {
        // 멈춤(Freeze) 상태일 때 회전 속도를 극감시켜 공포감 조성
        float speedMultiplier = 1f;
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            speedMultiplier = 0.1f;
        }

        if (VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.isTracking)
        {
            Vector3 headDelta = VisionTrackingManager.Instance.currentHeadPosition - VisionTrackingManager.Instance.baselineHeadPosition;

            // X축 이동량 검사 (좌우 회전 누적)
            if (Mathf.Abs(headDelta.x) > deadzoneRadius)
            {
                // 데드존을 초과한 만큼만 순수하게 가속 (급가속 방지)
                float activeX = headDelta.x > 0 ? headDelta.x - deadzoneRadius : headDelta.x + deadzoneRadius;
                _targetYaw += activeX * headRotationSpeed * speedMultiplier * Time.deltaTime;
            }

            // Y축 이동량 검사 (상하 회전 누적 - 미세 떨림 완벽 차단)
            if (Mathf.Abs(headDelta.y) > deadzoneRadius)
            {
                float activeY = headDelta.y > 0 ? headDelta.y - deadzoneRadius : headDelta.y + deadzoneRadius;
                _targetPitch -= activeY * headRotationSpeed * speedMultiplier * Time.deltaTime; // 상하 반전
            }

            // 상하 제한 (Gimbal Lock 방지)
            _targetPitch = Mathf.Clamp(_targetPitch, -maxPitchAngle, maxPitchAngle);
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _targetYaw += mouseDelta.x * 0.1f * speedMultiplier;
            _targetPitch -= mouseDelta.y * 0.1f * speedMultiplier;
            _targetPitch = Mathf.Clamp(_targetPitch, -maxPitchAngle, maxPitchAngle);
        }

        // 스무딩 연산 (기존보다 묵직하게 정지)
        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);

        // 상하 회전 적용 (카메라 자체)
        transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);

        // 좌우 회전 적용 (플레이어 몸통 전체 - 360도 회전)
        if (playerBody != null)
        {
            playerBody.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}
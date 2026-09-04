using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("회전 대상 설정")]
    public Transform playerBody;

    [Header("Vision AI 얼굴 회전 설정")]
    public float headRotationSpeed = 2f;
    public float maxPitchAngle = 60f;
    public float deadzoneRadius = 2.0f;
    public float rotationSmoothTime = 0.1f;

    [Header("회전 방향 반전 설정")]
    public bool invertYaw = false;
    public bool invertPitch = true;

    private float _targetYaw = 0f;
    private float _targetPitch = 0f;
    private float _currentYaw = 0f;
    private float _currentPitch = 0f;
    private float _yawVelocity;
    private float _pitchVelocity;

    private float _baseYaw = 0f;
    private float _basePitch = 0f;

    private void Start()
    {
        if (playerBody != null)
        {
            _baseYaw = playerBody.localEulerAngles.y;
            _targetYaw = _baseYaw;
            _currentYaw = _baseYaw;
        }

        headRotationSpeed = PlayerPrefs.GetFloat("RotationSpeed", 2f);
        deadzoneRadius = PlayerPrefs.GetFloat("DeadzoneRadius", 2.0f);
        invertYaw = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
    }

    public void SyncToCurrentLocalRotation()
    {
        _baseYaw = playerBody != null ? playerBody.localEulerAngles.y : 0f;
        _targetYaw = _baseYaw;
        _currentYaw = _baseYaw;

        float pitch = transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        _basePitch = pitch;
        _targetPitch = pitch;
        _currentPitch = pitch;
    }

    public void SetInvertYaw(bool value)
    {
        invertYaw = value;
        PlayerPrefs.SetInt("InvertYaw", value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void ResetView()
    {
        _targetYaw = _baseYaw;
        _targetPitch = _basePitch;
        _currentYaw = _baseYaw;
        _currentPitch = _basePitch;
        transform.localRotation = Quaternion.Euler(_basePitch, 0f, 0f);
        if (playerBody != null) playerBody.localRotation = Quaternion.Euler(0f, _baseYaw, 0f);
    }

    private void Update()
    {
        bool isPaused = UIManager.Instance != null && UIManager.Instance.IsPaused;
        bool isUIBlocking = UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking();

        if (isPaused || isUIBlocking) return;

        // 중앙 통제소인 PlayerController의 락 상태만 확인하도록 구조 개선
        if (PlayerController.Instance != null && PlayerController.Instance.IsCameraLocked) return;

        float speedMultiplier = (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive) ? 0.2f : 1f;

        if (VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.isTracking)
        {
            float deltaYaw = Mathf.DeltaAngle(VisionTrackingManager.Instance.baselineHeadRotation.y, VisionTrackingManager.Instance.currentHeadRotation.y);
            float deltaPitch = Mathf.DeltaAngle(VisionTrackingManager.Instance.baselineHeadRotation.x, VisionTrackingManager.Instance.currentHeadRotation.x);

            if (Mathf.Abs(deltaYaw) > deadzoneRadius)
            {
                float activeYaw = deltaYaw > 0 ? deltaYaw - deadzoneRadius : deltaYaw + deadzoneRadius;
                if (invertYaw) activeYaw = -activeYaw;
                _baseYaw += activeYaw * headRotationSpeed * speedMultiplier * Time.deltaTime;
            }

            if (Mathf.Abs(deltaPitch) > deadzoneRadius)
            {
                float activePitch = deltaPitch > 0 ? deltaPitch - deadzoneRadius : deltaPitch + deadzoneRadius;
                if (invertPitch) activePitch = -activePitch;
                _basePitch += activePitch * headRotationSpeed * speedMultiplier * Time.deltaTime;
            }

            _basePitch = Mathf.Clamp(_basePitch, -maxPitchAngle, maxPitchAngle);

            _targetYaw = _baseYaw;
            _targetPitch = _basePitch;
        }
        else if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _targetYaw += mouseDelta.x * 0.1f * speedMultiplier;
            _targetPitch -= mouseDelta.y * 0.1f * speedMultiplier;
            _targetPitch = Mathf.Clamp(_targetPitch, -maxPitchAngle, maxPitchAngle);
            _baseYaw = _targetYaw;
            _basePitch = _targetPitch;
        }

        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);

        transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        if (playerBody != null) playerBody.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
    }
}
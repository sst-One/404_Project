using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("회전 대상 설정")]
    public Transform playerBody;

    [Header("Vision AI 얼굴 회전 설정")]
    [Tooltip("구버전 호환용 감도. 새 설정은 가로/세로 감도를 사용합니다.")]
    public float headRotationSpeed = 2f;
    [Min(0.1f)] public float horizontalSensitivity = 2f;
    [Min(0.1f)] public float verticalSensitivity = 2f;
    public float maxPitchAngle = 60f;
    public float deadzoneRadius = 2.0f;
    public float rotationSmoothTime = 0.1f;

    [Header("회전 방향 반전 설정")]
    public bool invertYaw = false;
    public bool invertPitch;
    internal const string PitchPreferenceKey = "CameraPitchInvertedV2";

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
        horizontalSensitivity = PlayerPrefs.GetFloat("HorizontalSensitivity", headRotationSpeed);
        verticalSensitivity = PlayerPrefs.GetFloat("VerticalSensitivity", headRotationSpeed);
        deadzoneRadius = PlayerPrefs.GetFloat("DeadzoneRadius", 2.0f);
        invertYaw = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
        invertPitch = PlayerPrefs.GetInt(PitchPreferenceKey, invertPitch ? 1 : 0) == 1;
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

    public void SetInvertPitch(bool value)
    {
        invertPitch = value;
        PlayerPrefs.SetInt(PitchPreferenceKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetHorizontalSensitivity(float value)
    {
        horizontalSensitivity = Mathf.Max(0.1f, value);
        PlayerPrefs.SetFloat("HorizontalSensitivity", horizontalSensitivity);
        PlayerPrefs.Save();
    }

    public void SetVerticalSensitivity(float value)
    {
        verticalSensitivity = Mathf.Max(0.1f, value);
        PlayerPrefs.SetFloat("VerticalSensitivity", verticalSensitivity);
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
        bool isUIBlocking = UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking() && !UIManager.Instance.IsDialogueActive;

        // 대사/선택 중에는 이동·상호작용은 잠그되, 환각을 바라보거나 시선을 돌리는 카메라 입력은 허용합니다.
        if (isPaused || isUIBlocking) return;

        // 중앙 통제소인 PlayerController의 락 상태만 확인하도록 구조 개선
        PlayerController player = PlayerController.Instance;
        if (player != null && (!player.IsInputSetupComplete || player.IsCameraLocked)) return;
        if (TutorialController.Instance != null && TutorialController.Instance.IsCameraLocked) return;

        float speedMultiplier = (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive) ? 0.2f : 1f;

        bool allowTracking = PlayerController.Instance == null ||
            (PlayerController.Instance.useWebcam && !PlayerController.Instance.forceFallbackMode);
        if (allowTracking && VisionTrackingManager.Instance != null &&
            VisionTrackingManager.Instance.IsCalibrated && VisionTrackingManager.Instance.isTracking)
        {
            float deltaYaw = Mathf.DeltaAngle(VisionTrackingManager.Instance.baselineHeadRotation.y, VisionTrackingManager.Instance.currentHeadRotation.y);
            float deltaPitch = Mathf.DeltaAngle(VisionTrackingManager.Instance.baselineHeadRotation.x, VisionTrackingManager.Instance.currentHeadRotation.x);

            if (Mathf.Abs(deltaYaw) > deadzoneRadius)
            {
                float activeYaw = deltaYaw > 0 ? deltaYaw - deadzoneRadius : deltaYaw + deadzoneRadius;
                if (invertYaw) activeYaw = -activeYaw;
                _baseYaw += activeYaw * horizontalSensitivity * speedMultiplier * Time.deltaTime;
            }

            if (Mathf.Abs(deltaPitch) > deadzoneRadius)
            {
                float activePitch = deltaPitch > 0 ? deltaPitch - deadzoneRadius : deltaPitch + deadzoneRadius;
                if (invertPitch) activePitch = -activePitch;
                _basePitch += activePitch * verticalSensitivity * speedMultiplier * Time.deltaTime;
            }

            _basePitch = Mathf.Clamp(_basePitch, -maxPitchAngle, maxPitchAngle);

            _targetYaw = _baseYaw;
            _targetPitch = _basePitch;
        }
        else if (player != null && player.AllowsKeyboardMouse && Mouse.current != null &&
            Cursor.lockState == CursorLockMode.Locked)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            _targetYaw += mouseDelta.x * horizontalSensitivity * 0.05f * speedMultiplier;
            _targetPitch -= mouseDelta.y * verticalSensitivity * 0.05f * speedMultiplier;
            _targetPitch = Mathf.Clamp(_targetPitch, -maxPitchAngle, maxPitchAngle);
            _baseYaw = _targetYaw;
            _basePitch = _targetPitch;
        }
        else
        {
            return;
        }

        _currentYaw = Mathf.SmoothDamp(_currentYaw, _targetYaw, ref _yawVelocity, rotationSmoothTime);
        _currentPitch = Mathf.SmoothDamp(_currentPitch, _targetPitch, ref _pitchVelocity, rotationSmoothTime);

        transform.localRotation = Quaternion.Euler(_currentPitch, 0f, 0f);
        if (playerBody != null) playerBody.localRotation = Quaternion.Euler(0f, _currentYaw, 0f);
    }
}

using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputProvider : MonoBehaviour
{
    [Header("Input Data (Read Only)")]
    public Vector2 GazeScreenPosition;
    public bool IsGripTriggered;
    public bool IsGripHeld;
    public bool IsLeanTriggered;
    public bool IsFreezeActive;

    [Header("Input Mode")]
    public bool useWebcam = true;

    private bool _wasReachHeldLastFrame = false;
    private bool _wasLeanHeldLastFrame = false;

    private void Update()
    {
        IsGripTriggered = false;
        IsLeanTriggered = false;

        // 마우스/키보드 강제 사용 설정이거나, 웹캠 추적이 5초 이상 끊겨 Fallback 상태가 된 경우
        if (!useWebcam || (VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.IsInFallbackMode))
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            ProcessPCInput();
        }
        // 웹캠 사용 중인 경우
        else if (VisionTrackingManager.Instance != null)
        {
            if (VisionTrackingManager.Instance.isTracking)
            {
                IsFreezeActive = false;
                IsGripHeld = false;
                ProcessWebcamInput();
            }
            else
            {
                // POL-012: 인식이 끊긴 지 0~5초 사이. 단발성 트리거(Triggered)는 끄되, 
                // 유지 상태(Held, Freeze)는 이전 프레임 상태를 그대로 유지하여 게임이 끊기지 않게 방어.
            }
        }
    }

    private void ProcessWebcamInput()
    {
        bool isUIBlocking = false;
        if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive) isUIBlocking = true;
        //if (PhoneUIController.Instance != null && PhoneUIController.Instance.IsPhoneUIActive) isUIBlocking = true;

        GazeScreenPosition = VisionTrackingManager.Instance.GetGazeScreenPosition();

        bool isCurrentlyReaching = VisionTrackingManager.Instance.GetReachState();
        if (!isUIBlocking)
        {
            IsGripHeld = isCurrentlyReaching;
            if (isCurrentlyReaching && !_wasReachHeldLastFrame) IsGripTriggered = true;
        }
        _wasReachHeldLastFrame = isCurrentlyReaching;

        bool isCurrentlyLeaning = VisionTrackingManager.Instance.GetLeanState();
        if (!isUIBlocking)
        {
            if (isCurrentlyLeaning && !_wasLeanHeldLastFrame) IsLeanTriggered = true;
        }
        _wasLeanHeldLastFrame = isCurrentlyLeaning;

        IsFreezeActive = VisionTrackingManager.Instance.GetOriginFreezeState();
    }

    private void ProcessPCInput()
    {
        bool isUIBlocking = false;
        if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive) isUIBlocking = true;
        //if (PhoneUIController.Instance != null && PhoneUIController.Instance.IsPhoneUIActive) isUIBlocking = true;

        if (Mouse.current != null)
        {
            GazeScreenPosition = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (!isUIBlocking) IsGripTriggered = true;
            }
            if (Mouse.current.leftButton.isPressed)
            {
                if (!isUIBlocking) IsGripHeld = true;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (!isUIBlocking) IsLeanTriggered = true;
            }
            if (Keyboard.current.spaceKey.isPressed)
            {
                IsFreezeActive = true;
            }
        }
    }
}
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

        // [핵심 핫픽스] UI 메뉴(ESC)나 자막이 떠 있을 경우, 입력을 뿌리 단계에서 원천 차단
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            return; // 아래의 웹캠 및 PC 입력 로직 진입을 완전히 막음
        }

        if (!useWebcam || (VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.IsInFallbackMode))
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            ProcessPCInput();
        }
        else if (VisionTrackingManager.Instance != null)
        {
            if (VisionTrackingManager.Instance.isTracking)
            {
                IsFreezeActive = false;
                IsGripHeld = false;
                ProcessWebcamInput();
            }
        }
    }

    private void ProcessWebcamInput()
    {
        GazeScreenPosition = VisionTrackingManager.Instance.GetGazeScreenPosition();

        bool isCurrentlyReaching = VisionTrackingManager.Instance.GetReachState();
        IsGripHeld = isCurrentlyReaching;
        if (isCurrentlyReaching && !_wasReachHeldLastFrame) IsGripTriggered = true;
        _wasReachHeldLastFrame = isCurrentlyReaching;

        bool isCurrentlyLeaning = VisionTrackingManager.Instance.GetLeanState();
        if (isCurrentlyLeaning && !_wasLeanHeldLastFrame) IsLeanTriggered = true;
        _wasLeanHeldLastFrame = isCurrentlyLeaning;

        IsFreezeActive = VisionTrackingManager.Instance.GetOriginFreezeState();
    }

    private void ProcessPCInput()
    {
        if (Mouse.current != null)
        {
            GazeScreenPosition = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame) IsGripTriggered = true;
            if (Mouse.current.leftButton.isPressed) IsGripHeld = true;
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame) IsLeanTriggered = true;
            if (Keyboard.current.spaceKey.isPressed) IsFreezeActive = true;
        }
    }
}
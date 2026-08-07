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

        bool isSystemMenuOpen = false;
        if (SystemMenuController.Instance != null && SystemMenuController.Instance.IsPaused)
        {
            isSystemMenuOpen = true;
        }

        if (isSystemMenuOpen || (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()))
        {
            IsFreezeActive = false;
            IsGripHeld = false;
            return;
        }

        // [핵심 핫픽스 1] 시선(커서)을 화면 정중앙에 완벽히 고정 (FPS 스타일 조준)
        GazeScreenPosition = new Vector2(Screen.width / 2f, Screen.height / 2f);

        if (!useWebcam || VisionTrackingManager.Instance == null || !VisionTrackingManager.Instance.isTracking)
        {
            ProcessPCInput();
        }
        else
        {
            ProcessWebcamInput();
        }
    }

    private void ProcessWebcamInput()
    {
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
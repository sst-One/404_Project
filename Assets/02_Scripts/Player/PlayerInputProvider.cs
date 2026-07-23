using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 역할: 플레이어의 모든 입력을 수집하여 순수 데이터로만 제공하는 모듈.
/// 물리 연산(Raycast)이나 UI 렌더링은 절대 수행하지 않음.
/// </summary>
public class PlayerInputProvider : MonoBehaviour
{
    [Header("Input Data (Read Only)")]
    [Tooltip("현재 시선(마우스)의 화면 스크린 좌표")]
    public Vector2 GazeScreenPosition;

    [Tooltip("상호작용(Grip/좌클릭) 입력 여부")]
    public bool IsGripTriggered;
    public bool IsGripHeld;

    [Tooltip("이동(Lean/W키) 단발성 입력 여부")]
    public bool IsLeanTriggered;

    [Tooltip("정지(Freeze/스페이스바) 유지 여부")]
    public bool IsFreezeActive;

    // 추후 MediaPipe(웹캠) 데이터가 들어오면 이 bool 값을 true로 바꾸어 분기처리 가능
    private bool useWebcam = false;

    private void Update()
    {
        IsGripTriggered = false;
        IsLeanTriggered = false;
        IsFreezeActive = false;
        IsGripHeld = false; // 매 프레임 초기화

        if (!useWebcam) ProcessPCInput();
    }

    private void ProcessPCInput()
    {
        if (Mouse.current != null)
        {
            GazeScreenPosition = Mouse.current.position.ReadValue();

            // 자막 출력 중일 때는 Reach(클릭 및 유지) 신호 차단
            if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive)
            {
                // 차단됨
            }
            else
            {
                if (Mouse.current.leftButton.wasPressedThisFrame) IsGripTriggered = true;
                if (Mouse.current.leftButton.isPressed) IsGripHeld = true; // [신규 추가]
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive) { /* 무시 */ }
                else IsLeanTriggered = true;
            }
            if (Keyboard.current.spaceKey.isPressed) IsFreezeActive = true;
        }
    }
}
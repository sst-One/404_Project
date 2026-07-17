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

    [Tooltip("상호작용(Grip/좌클릭) 단발성 입력 여부")]
    public bool IsGripTriggered;

    [Tooltip("이동(Lean/W키) 단발성 입력 여부")]
    public bool IsLeanTriggered;

    [Tooltip("정지(Freeze/스페이스바) 유지 여부")]
    public bool IsFreezeActive;

    // 추후 MediaPipe(웹캠) 데이터가 들어오면 이 bool 값을 true로 바꾸어 분기처리 가능
    private bool useWebcam = false;

    private void Update()
    {
        // 1. 매 프레임 입력 데이터를 초기화 (단발성 입력을 위해)
        IsGripTriggered = false;
        IsLeanTriggered = false;
        IsFreezeActive = false;

        // 2. PC 폴백(키보드/마우스) 입력 처리
        if (!useWebcam)
        {
            ProcessPCInput();
        }
        else
        {
            // 추후 MediaPipe 수신 로직이 여기에 들어갑니다.
            // 일단은 구조를 비워둡니다.
        }
    }

    private void ProcessPCInput()
    {
        if (Mouse.current != null)
        {
            // [수정 1] 정중앙 강제 고정 해제 -> 실제 마우스 포인터 좌표 추적
            GazeScreenPosition = Mouse.current.position.ReadValue();

            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                IsGripTriggered = true;
            }
        }

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.wasPressedThisFrame) IsLeanTriggered = true;
            if (Keyboard.current.spaceKey.isPressed) IsFreezeActive = true;
        }
    }
}
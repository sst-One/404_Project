using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("디버그 및 테스트 환경 (SYS-009)")]
    [Tooltip("체크 시 웹캠(비전 AI)을 무시하고 키보드/마우스 입력 모드를 강제합니다.")]
    public bool forceFallbackMode = false; // PC 테스트를 위해 기본값을 true로 설정

    private IPlayerInput _currentInputProcessor;
    private IPlayerInput _fallbackProcessor;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeProcessors();
    }

    private void InitializeProcessors()
    {
        UpdateInputProcessor();
    }

    private void Update()
    {
        // [F1] 키를 통한 런타임 강제 입력 모드 스위칭
        if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
        {
            forceFallbackMode = !forceFallbackMode;
            UpdateInputProcessor();
            Debug.Log($"[InputManager] 입력 모드 실시간 변경: {(forceFallbackMode ? "키보드/마우스 (Fallback)" : "웹캠 (Vision AI)")}");
        }
    }

    private void UpdateInputProcessor()
    {
        if (forceFallbackMode)
        {
            _currentInputProcessor = _fallbackProcessor;
        }
    }

    public IPlayerInput GetInput()
    {
        // 타 스크립트(InteractionManager 등)에서 GetInput 호출 시 null 방어
        if (_currentInputProcessor == null)
        {
            UpdateInputProcessor();
        }
        return _currentInputProcessor;
    }
}
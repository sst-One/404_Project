using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private IPlayerInput _currentInputProcessor;
    private IPlayerInput _fallbackProcessor; // 예외 상황용 키보드 입력

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
        _fallbackProcessor = new KeyboardInputProcessor();

        // 씬 내에 배치된 PlayerInputMapper(비전 AI)를 최우선으로 찾음
        var mapper = FindObjectOfType<PlayerInputMapper>();
        if (mapper != null)
        {
            _currentInputProcessor = mapper;
            Debug.Log("[InputManager] PlayerInputMapper (Vision AI) 연동 완료.");
        }
        else
        {
            _currentInputProcessor = _fallbackProcessor;
            Debug.LogWarning("[InputManager] PlayerInputMapper를 찾을 수 없어 Fallback(Keyboard) 모드로 전환합니다.");
        }
    }

    public IPlayerInput GetInput()
    {
        return _currentInputProcessor;
    }
}
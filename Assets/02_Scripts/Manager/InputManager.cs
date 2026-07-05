using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    private IPlayerInput _currentInputProcessor;
    private KeyboardInputProcessor _keyboardProcessor;

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
        _keyboardProcessor = new KeyboardInputProcessor();
        _currentInputProcessor = _keyboardProcessor;
    }

    public IPlayerInput GetInput()
    {
        return _currentInputProcessor;
    }
}
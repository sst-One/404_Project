using System.Collections;
using UnityEngine;

public class FreezeManager : MonoBehaviour
{
    public static FreezeManager Instance { get; private set; }

    [Header("Settings")]
    public float heartbeatIncreaseInterval = 1.0f;
    public int heartbeatAmountPerTick = 1;
    public float overloadNoisePenalty = 0.5f;

    public bool IsFreezing { get; private set; }

    private Coroutine _heartbeatCoroutine;
    private bool _isOverloaded;
    private bool _requireInputReset;

    // 최신 입력 모듈 참조용 캐시 변수
    private PlayerInputProvider _playerInput;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnHeartbeatLevelChanged += HandleHeartbeatChanged;
        }
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnHeartbeatLevelChanged -= HandleHeartbeatChanged;
        }
    }

    private void Update()
    {
        if (_isOverloaded) return;

        // PlayerInputProvider 동적 캐싱 (씬 전환 시 플레이어 객체 로드 대기)
        if (_playerInput == null)
        {
            _playerInput = FindObjectOfType<PlayerInputProvider>();
            if (_playerInput == null) return; // 플레이어가 아직 로드되지 않았다면 대기
        }

        // 최신 입력 체계(PlayerInputProvider)에서 Origin Freeze 상태를 가져옴
        bool isFreezeInput = _playerInput.IsFreezeActive;

        if (_requireInputReset)
        {
            if (!isFreezeInput)
            {
                _requireInputReset = false;
            }
            return;
        }

        if (isFreezeInput && !IsFreezing)
        {
            StartFreeze();
        }
        else if (!isFreezeInput && IsFreezing)
        {
            StopFreeze();
        }
    }

    private void StartFreeze()
    {
        IsFreezing = true;
        Debug.Log("Freeze 상태 돌입: 호흡을 참습니다.");
        _heartbeatCoroutine = StartCoroutine(HeartbeatIncreaseRoutine());
    }

    private void StopFreeze()
    {
        IsFreezing = false;
        Debug.Log("Freeze 상태 해제: 호흡을 재개합니다.");
        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }
    }

    private IEnumerator HeartbeatIncreaseRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(heartbeatIncreaseInterval);
        while (IsFreezing)
        {
            yield return wait;
            if (StateManager.Instance != null)
            {
                StateManager.Instance.AddHeartbeat(heartbeatAmountPerTick);
            }
        }
    }

    private void HandleHeartbeatChanged(HeartbeatLevel level)
    {
        if (level == HeartbeatLevel.Overload)
        {
            TriggerOverloadPenalty();
        }
    }

    private void TriggerOverloadPenalty()
    {
        _isOverloaded = true;
        IsFreezing = false;

        if (_heartbeatCoroutine != null)
        {
            StopCoroutine(_heartbeatCoroutine);
            _heartbeatCoroutine = null;
        }

        Debug.LogWarning("페널티 발생: 심장박동 한계 초과! 강제로 숨을 내쉽니다.");

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(overloadNoisePenalty);
        }

        StartCoroutine(OverloadRecoveryRoutine());
    }

    private IEnumerator OverloadRecoveryRoutine()
    {
        yield return new WaitForSeconds(2.0f);

        _isOverloaded = false;
        _requireInputReset = true;
        Debug.Log("오버로드 상태 회복됨. 다시 정지 동작(Origin Freeze)을 수행할 수 있습니다.");
    }
}
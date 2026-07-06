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
        if (InputManager.Instance == null || _isOverloaded) return;

        bool isFreezeInput = InputManager.Instance.GetInput().IsFreezing();

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

        // 기획 변경: 즉시 초기화(ResetHeartbeat) 로직 삭제 완료
        // 심장박동은 StateManager의 자연 회복 코루틴에 의해서만 서서히 감소합니다.
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

        // 기획 변경: 오버로드 회복 시 즉시 초기화(ResetHeartbeat) 로직 삭제 완료

        _isOverloaded = false;
        _requireInputReset = true;
        Debug.Log("오버로드 상태 회복됨. 스페이스바를 떼었다가 다시 눌러야 정지 가능합니다.");
    }
}
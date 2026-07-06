using System;
using System.Collections;
using UnityEngine;

public enum NoiseLevel { Low, Mid, High, Critical }
public enum HeartbeatLevel { Stable, Rise, High, Overload }

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    public event Action<NoiseLevel> OnNoiseLevelChanged;
    public event Action<HeartbeatLevel> OnHeartbeatLevelChanged;

    private float _currentNoiseValue = 0f;
    private int _currentHeartbeatValue = 0;

    private NoiseLevel _currentNoiseLevel = NoiseLevel.Low;
    private HeartbeatLevel _currentHeartbeatLevel = HeartbeatLevel.Stable;

    private int _threatCount = 0;
    private Coroutine _threatCoroutine;

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
        // 신규 추가: 자연 회복 코루틴 상시 가동
        StartCoroutine(HeartbeatCooldownRoutine());
    }

    public void AddNoise(float amount)
    {
        _currentNoiseValue = Mathf.Clamp(_currentNoiseValue + amount, 0f, 1f);
        UpdateNoiseLevel();
    }

    private void UpdateNoiseLevel()
    {
        NoiseLevel newLevel = NoiseLevel.Low;
        if (_currentNoiseValue > 0.75f) newLevel = NoiseLevel.Critical;
        else if (_currentNoiseValue > 0.5f) newLevel = NoiseLevel.High;
        else if (_currentNoiseValue > 0.25f) newLevel = NoiseLevel.Mid;

        if (newLevel != _currentNoiseLevel)
        {
            _currentNoiseLevel = newLevel;
            Debug.Log("[StateManager] 소음 레벨 변경: " + _currentNoiseLevel);
            OnNoiseLevelChanged?.Invoke(_currentNoiseLevel);
        }
    }

    public void AddHeartbeat(int amount)
    {
        _currentHeartbeatValue = Mathf.Clamp(_currentHeartbeatValue + amount, 0, 3);
        UpdateHeartbeatLevel();
    }

    public void ResetHeartbeat()
    {
        _currentHeartbeatValue = 0;
        UpdateHeartbeatLevel();
    }

    private void UpdateHeartbeatLevel()
    {
        HeartbeatLevel newLevel = (HeartbeatLevel)_currentHeartbeatValue;
        if (newLevel != _currentHeartbeatLevel)
        {
            _currentHeartbeatLevel = newLevel;
            Debug.Log("[StateManager] 심장박동 레벨 변경: " + _currentHeartbeatLevel);
            OnHeartbeatLevelChanged?.Invoke(_currentHeartbeatLevel);
        }
    }

    public void AddThreat()
    {
        _threatCount++;
        if (_threatCount == 1)
        {
            Debug.Log("[StateManager] 위협 감지됨! 심장박동 강제 상승 시작.");
            _threatCoroutine = StartCoroutine(ThreatHeartbeatRoutine());
        }
    }

    public void RemoveThreat()
    {
        _threatCount--;
        if (_threatCount <= 0)
        {
            _threatCount = 0;
            if (_threatCoroutine != null)
            {
                StopCoroutine(_threatCoroutine);
                _threatCoroutine = null;
                Debug.Log("[StateManager] 모든 위협 해제. 심장박동 강제 상승 중단.");
            }
        }
    }

    private IEnumerator ThreatHeartbeatRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.5f);
        while (_threatCount > 0)
        {
            yield return wait;
            AddHeartbeat(1);
        }
    }

    // 신규 추가: 자연 회복(Cooldown) 알고리즘
    private IEnumerator HeartbeatCooldownRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1.5f);
        while (true)
        {
            yield return wait;

            bool isFreezing = false;
            if (FreezeManager.Instance != null)
            {
                isFreezing = FreezeManager.Instance.IsFreezing;
            }

            // 위협이 없고, 숨을 참고 있지 않으며, 심장박동이 0보다 클 때만 감소
            if (_threatCount == 0 && !isFreezing && _currentHeartbeatValue > 0)
            {
                AddHeartbeat(-1);
                Debug.Log("[StateManager] 심장박동 자연 안정화 중...");
            }
        }
    }
}
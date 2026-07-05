using System;
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

    // 신규 추가: 심장박동 초기화 함수
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
            // 신규 추가: 상태 변경 시 콘솔 로그 출력
            Debug.Log("[StateManager] 심장박동 레벨 변경: " + _currentHeartbeatLevel);
            OnHeartbeatLevelChanged?.Invoke(_currentHeartbeatLevel);
        }
    }
}
using System;
using System.Collections;
using UnityEngine;

public enum NoiseLevel { Low, Mid, High, Critical }
public enum HeartbeatLevel { Stable, Rise, High, Overload }

public class StateManager : MonoBehaviour
{
    public static StateManager Instance { get; private set; }

    public event Action<NoiseLevel, Vector3> OnNoiseLevelChanged;
    public event Action<HeartbeatLevel> OnHeartbeatLevelChanged;

    private float _currentNoiseValue = 0f;
    private int _currentHeartbeatValue = 0;

    private NoiseLevel _currentNoiseLevel = NoiseLevel.Low;
    private HeartbeatLevel _currentHeartbeatLevel = HeartbeatLevel.Stable;

    private int _threatCount = 0;
    private Coroutine _threatCoroutine;
    private Vector3 _lastNoisePosition;

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
        StartCoroutine(HeartbeatCooldownRoutine());
        StartCoroutine(NoiseCooldownRoutine()); // [TPM 핫픽스] 소음 자연 감소 코루틴 상시 가동
    }

    // =========================================================
    // 소음 (Noise) 시스템
    // =========================================================

    public void AddNoise(float amount, Vector3 noiseSourcePosition = default)
    {
        _currentNoiseValue = Mathf.Clamp(_currentNoiseValue + amount, 0f, 1f);
        _lastNoisePosition = noiseSourcePosition == default ? Camera.main.transform.position : noiseSourcePosition;

        NoiseLevel newLevel = NoiseLevel.Low;
        if (_currentNoiseValue > 0.75f) newLevel = NoiseLevel.Critical;
        else if (_currentNoiseValue > 0.5f) newLevel = NoiseLevel.High;
        else if (_currentNoiseValue > 0.25f) newLevel = NoiseLevel.Mid;

        _currentNoiseLevel = newLevel;

        // [TPM 핫픽스] 단계 변경 여부와 상관없이, Mid(0.25) 이상부터는 행동할 때마다 AI에게 핑(Ping)을 찍어 호출합니다.
        if (_currentNoiseLevel != NoiseLevel.Low)
        {
            Debug.Log($"[StateManager] 소음 발생: {_currentNoiseLevel} (누적치: {_currentNoiseValue:F2} / 위치: {_lastNoisePosition})");
            OnNoiseLevelChanged?.Invoke(_currentNoiseLevel, _lastNoisePosition);
        }
    }

    private IEnumerator NoiseCooldownRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(1.0f);
        while (true)
        {
            yield return wait;
            // 1초마다 소음 누적치를 0.05씩 깎아줍니다. (가만히 있으면 조용해짐)
            if (_currentNoiseValue > 0f)
            {
                _currentNoiseValue = Mathf.Clamp(_currentNoiseValue - 0.05f, 0f, 1f);

                // AI를 자극하지 않고 내부적으로만 단계(Tier)를 낮춥니다.
                NoiseLevel newLevel = NoiseLevel.Low;
                if (_currentNoiseValue > 0.75f) newLevel = NoiseLevel.Critical;
                else if (_currentNoiseValue > 0.5f) newLevel = NoiseLevel.High;
                else if (_currentNoiseValue > 0.25f) newLevel = NoiseLevel.Mid;

                _currentNoiseLevel = newLevel;
            }
        }
    }

    // =========================================================
    // 심장박동 (Heartbeat) 및 위협(Threat) 시스템
    // =========================================================

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
            Debug.Log($"[StateManager] 심장박동 레벨 변경: {_currentHeartbeatLevel}");
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

            if (_threatCount == 0 && !isFreezing && _currentHeartbeatValue > 0)
            {
                AddHeartbeat(-1);
            }
        }
    }
}
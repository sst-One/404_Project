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

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string normalBreathClipName = "";
    public string nervousBreathClipName = "";
    public string nervousHeartbeatClipName = "";

    [Header("Heartbeat Overload")]
    public float overloadNoisePenalty = 0.5f;

    // 은신 중에는 플레이어 호흡음을 중지해 자동으로 숨을 참는 연출을 적용합니다.
    private bool _isBreathSuppressedByHiding;

    // [최적화] 코루틴 캐싱 객체
    private WaitForSeconds _noiseWait;
    private WaitForSeconds _threatWait;
    private WaitForSeconds _cooldownWait;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [최적화] Awake 할당으로 런타임 GC 발생 억제
        _noiseWait = new WaitForSeconds(1.0f);
        _threatWait = new WaitForSeconds(0.5f);
        _cooldownWait = new WaitForSeconds(1.5f);
    }

    private void Start()
    {
        StartCoroutine(HeartbeatCooldownRoutine());
        StartCoroutine(NoiseCooldownRoutine());
    }

    public void AddNoise(float amount, Vector3 noiseSourcePosition = default)
    {
        _currentNoiseValue = Mathf.Clamp(_currentNoiseValue + amount, 0f, 1f);
        _lastNoisePosition = noiseSourcePosition == default ? Camera.main.transform.position : noiseSourcePosition;

        NoiseLevel newLevel = NoiseLevel.Low;
        if (_currentNoiseValue > 0.75f) newLevel = NoiseLevel.Critical;
        else if (_currentNoiseValue > 0.5f) newLevel = NoiseLevel.High;
        else if (_currentNoiseValue > 0.25f) newLevel = NoiseLevel.Mid;

        _currentNoiseLevel = newLevel;
        if (_currentNoiseLevel != NoiseLevel.Low) OnNoiseLevelChanged?.Invoke(_currentNoiseLevel, _lastNoisePosition);
    }

    private IEnumerator NoiseCooldownRoutine()
    {
        while (true)
        {
            yield return _noiseWait;
            if (_currentNoiseValue > 0f)
            {
                _currentNoiseValue = Mathf.Clamp(_currentNoiseValue - 0.15f, 0f, 1f);
                NoiseLevel newLevel = NoiseLevel.Low;
                if (_currentNoiseValue > 0.75f) newLevel = NoiseLevel.Critical;
                else if (_currentNoiseValue > 0.5f) newLevel = NoiseLevel.High;
                else if (_currentNoiseValue > 0.25f) newLevel = NoiseLevel.Mid;
                _currentNoiseLevel = newLevel;
            }
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
            OnHeartbeatLevelChanged?.Invoke(_currentHeartbeatLevel);
            UpdateHeartbeatEffects(_currentHeartbeatValue);

            // 기존 심박수 과부하의 소음 패널티는 유지합니다.
            if (_currentHeartbeatLevel == HeartbeatLevel.Overload)
                AddNoise(overloadNoisePenalty);
        }
    }

    public void AddThreat()
    {
        _threatCount++;
        if (_threatCount == 1) _threatCoroutine = StartCoroutine(ThreatHeartbeatRoutine());
    }

    public void RemoveThreat()
    {
        _threatCount--;
        if (_threatCount <= 0)
        {
            _threatCount = 0;
            if (_threatCoroutine != null) StopCoroutine(_threatCoroutine);
        }
    }

    private IEnumerator ThreatHeartbeatRoutine()
    {
        while (_threatCount > 0)
        {
            yield return _threatWait;
            AddHeartbeat(1);
        }
    }

    private IEnumerator HeartbeatCooldownRoutine()
    {
        while (true)
        {
            yield return _cooldownWait;
            bool isFreezingInput = PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive;
            if (_threatCount == 0 && !isFreezingInput && _currentHeartbeatValue > 0)
            {
                AddHeartbeat(-1);
            }
        }
    }

    public void SetHidingBreathSuppressed(bool suppressed)
    {
        if (_isBreathSuppressedByHiding == suppressed) return;
        _isBreathSuppressedByHiding = suppressed;

        if (AudioManager.Instance == null) return;
        if (suppressed)
            AudioManager.Instance.StopBreathSound();
        else
            UpdateHeartbeatEffects((int)_currentHeartbeatLevel);
    }

    private void UpdateHeartbeatEffects(int level)
    {
        if (AudioManager.Instance == null) return;

        if (_isBreathSuppressedByHiding)
            AudioManager.Instance.StopBreathSound();
        else if (level >= 2)
        {
            if (!string.IsNullOrEmpty(nervousBreathClipName))
                AudioManager.Instance.PlayBreathSound(nervousBreathClipName);
        }
        else if (!string.IsNullOrEmpty(normalBreathClipName))
        {
            AudioManager.Instance.PlayBreathSound(normalBreathClipName);
        }

        if (level >= 2)
        {
            if (!string.IsNullOrEmpty(nervousHeartbeatClipName))
                AudioManager.Instance.PlayStatusSound(nervousHeartbeatClipName);
        }
        else
        {
            AudioManager.Instance.StopStatusSound();
        }
    }
}
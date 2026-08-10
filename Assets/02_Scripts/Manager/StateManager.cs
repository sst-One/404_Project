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

    [Header("상태 사운드 (2D AudioSources)")]
    public AudioSource playerBreathSource;
    public AudioSource playerHeartbeatSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string normalBreathClipName = "";
    public string nervousBreathClipName = "";
    public string nervousHeartbeatClipName = "";

    [Header("Freeze (호흡참기) 패널티 설정")]
    public float heartbeatIncreaseInterval = 1.0f;
    public int heartbeatAmountPerTick = 1;
    public float overloadNoisePenalty = 0.5f;

    private bool _isFreezing;
    private bool _isOverloaded;
    private bool _requireInputReset;
    private Coroutine _freezeHeartbeatCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        StartCoroutine(HeartbeatCooldownRoutine());
        StartCoroutine(NoiseCooldownRoutine());
    }

    private void Update()
    {
        ProcessFreezeState();
    }

    // --- [병합된 FreezeManager 로직] ---
    private void ProcessFreezeState()
    {
        if (_isOverloaded || PlayerController.Instance == null) return;

        bool isFreezeInput = PlayerController.Instance.IsFreezeActive;

        if (_requireInputReset)
        {
            if (!isFreezeInput) _requireInputReset = false;
            return;
        }

        if (isFreezeInput && !_isFreezing)
        {
            _isFreezing = true;
            _freezeHeartbeatCoroutine = StartCoroutine(FreezeHeartbeatRoutine());
        }
        else if (!isFreezeInput && _isFreezing)
        {
            _isFreezing = false;
            if (_freezeHeartbeatCoroutine != null) StopCoroutine(_freezeHeartbeatCoroutine);
        }
    }

    private IEnumerator FreezeHeartbeatRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(heartbeatIncreaseInterval);
        while (_isFreezing)
        {
            yield return wait;
            AddHeartbeat(heartbeatAmountPerTick);
        }
    }

    private void TriggerOverloadPenalty()
    {
        _isOverloaded = true;
        _isFreezing = false;
        if (_freezeHeartbeatCoroutine != null) StopCoroutine(_freezeHeartbeatCoroutine);

        AddNoise(overloadNoisePenalty);
        StartCoroutine(OverloadRecoveryRoutine());
    }

    private IEnumerator OverloadRecoveryRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        _isOverloaded = false;
        _requireInputReset = true;
    }
    // ----------------------------------

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
        WaitForSeconds wait = new WaitForSeconds(1.0f);
        while (true)
        {
            yield return wait;
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

            if (_currentHeartbeatLevel == HeartbeatLevel.Overload) TriggerOverloadPenalty();
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
            bool isFreezingInput = PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive;
            if (_threatCount == 0 && !isFreezingInput && _currentHeartbeatValue > 0)
            {
                AddHeartbeat(-1);
            }
        }
    }

    private void UpdateHeartbeatEffects(int level)
    {
        if (AudioManager.Instance == null) return;
        if (level >= 2)
        {
            AudioClip nervousBreath = AudioManager.Instance.GetClip(nervousBreathClipName);
            if (playerBreathSource != null && nervousBreath != null)
            {
                if (playerBreathSource.clip != nervousBreath) playerBreathSource.clip = nervousBreath;
                if (!playerBreathSource.isPlaying) playerBreathSource.Play();
            }
            AudioClip nervousHeartbeat = AudioManager.Instance.GetClip(nervousHeartbeatClipName);
            if (playerHeartbeatSource != null && nervousHeartbeat != null)
            {
                if (playerHeartbeatSource.clip != nervousHeartbeat) playerHeartbeatSource.clip = nervousHeartbeat;
                if (!playerHeartbeatSource.isPlaying) playerHeartbeatSource.Play();
            }
        }
        else
        {
            AudioClip normalBreath = AudioManager.Instance.GetClip(normalBreathClipName);
            if (playerBreathSource != null && normalBreath != null)
            {
                if (playerBreathSource.clip != normalBreath) playerBreathSource.clip = normalBreath;
                if (!playerBreathSource.isPlaying) playerBreathSource.Play();
            }
            if (playerHeartbeatSource != null && playerHeartbeatSource.isPlaying) playerHeartbeatSource.Stop();
        }
    }
}
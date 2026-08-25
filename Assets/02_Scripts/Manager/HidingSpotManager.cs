using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class HidingSpotManager : MonoBehaviour
{
    public static HidingSpotManager Instance { get; private set; }

    [Header("체류 시간 임계값 (PARAM-028~030)")]
    public float unstableTime = 30f;
    public float failingTime = 45f;
    public float forcedTime = 60f;

    [Header("시청각 연출 트리거")]
    public UnityEvent onUnstableWarning;
    public UnityEvent onFailingWarning;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string warningHeartbeatClipName = "";
    public string tinnitusClipName = "SND-067_HidingSpotUnstableCue_Layer_OneShot";
    public string creakClipName = "";

    public event Action onForceEject;

    // [핫픽스 3] 외부(폰)에서 은신처 진입 여부를 알 수 있도록 프로퍼티 개방
    public bool IsHiding => _isHiding;

    private float _timeInSpot = 0f;
    private int _currentStage = 0;
    private bool _isHiding = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) return;

        if (_isHiding)
        {
            _timeInSpot += Time.deltaTime;
            CheckDegradeStages();
        }
    }

    public void StartHiding()
    {
        _isHiding = true;
        _timeInSpot = 0f;
        _currentStage = 0;
    }

    public void StopHiding()
    {
        _isHiding = false;
        ResetHidingSpot();
    }

    private void CheckDegradeStages()
    {
        if (_timeInSpot >= forcedTime && _currentStage < 3)
        {
            _currentStage = 3;
            if (StateManager.Instance != null)
            {
                StateManager.Instance.AddHeartbeat(1);
                Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
                StateManager.Instance.AddNoise(1.0f, playerPos);
            }
            onForceEject?.Invoke();
            StopHiding();
        }
        else if (_timeInSpot >= failingTime && _currentStage < 2)
        {
            _currentStage = 2;
            if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

            if (!string.IsNullOrEmpty(tinnitusClipName) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGlobal2D(tinnitusClipName, AudioManager.Instance.playerStatusMixerGroup);
            }
            onFailingWarning?.Invoke();
        }
        else if (_timeInSpot >= unstableTime && _currentStage < 1)
        {
            _currentStage = 1;

            if (!string.IsNullOrEmpty(warningHeartbeatClipName) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayStatusSound(warningHeartbeatClipName);
            }
            if (!string.IsNullOrEmpty(creakClipName) && AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGlobal2D(creakClipName, AudioManager.Instance.sfxMixerGroup);
            }
            onUnstableWarning?.Invoke();
        }
    }

    private void ResetHidingSpot()
    {
        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();
        _timeInSpot = 0f;
        _currentStage = 0;
    }
}
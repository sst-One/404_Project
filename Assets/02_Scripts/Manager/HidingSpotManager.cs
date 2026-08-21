using UnityEngine;
using UnityEngine.Events;
using System;

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

    [Header("오디오 피드백 (AudioSources)")]
    public AudioSource warningHeartbeatSource;
    public AudioSource tinnitusSource;
    public AudioSource creakSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string warningHeartbeatClipName = "";
    public string tinnitusClipName = "SND-067_HidingSpotUnstableCue_Layer_OneShot";
    public string creakClipName = "";

    // 60초 강제 퇴출 시 씬 내부의 은신처에게 신호를 보내기 위한 액션
    public event Action onForceEject;

    private float _timeInSpot = 0f;
    private int _currentStage = 0;
    private bool _isHiding = false;

    private void Awake()
    {
        // 씬 로컬 싱글톤 (씬 전환 시 중복 방지 및 파괴 허용)
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        // 씬이 언로드될 때 메모리 릭 방지를 위해 Instance 해제
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

    // 씬 내부의 개별 은신처(HidingSpotAction)에서 진입 시 호출
    public void StartHiding()
    {
        _isHiding = true;
        _timeInSpot = 0f;
        _currentStage = 0;
    }

    // 씬 내부의 개별 은신처에서 퇴출 시 호출
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

                // 플레이어 위치 기준으로 강제 소음 발생
                Vector3 playerPos = PlayerController.Instance != null ? PlayerController.Instance.transform.position : Vector3.zero;
                StateManager.Instance.AddNoise(1.0f, playerPos);
            }

            // 물리적 퇴출을 위해 씬 내부 은신처로 신호 발송
            onForceEject?.Invoke();
            StopHiding();
        }
        else if (_timeInSpot >= failingTime && _currentStage < 2)
        {
            _currentStage = 2;
            if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

            if (tinnitusSource != null && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.GetClip(tinnitusClipName);
                if (clip != null)
                {
                    tinnitusSource.clip = clip;
                    tinnitusSource.Play();
                }
            }
            onFailingWarning?.Invoke();
        }
        else if (_timeInSpot >= unstableTime && _currentStage < 1)
        {
            _currentStage = 1;

            if (warningHeartbeatSource != null && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.GetClip(warningHeartbeatClipName);
                if (clip != null)
                {
                    warningHeartbeatSource.clip = clip;
                    warningHeartbeatSource.Play();
                }
            }

            if (creakSource != null && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.GetClip(creakClipName);
                if (clip != null) creakSource.PlayOneShot(clip);
            }
            onUnstableWarning?.Invoke();
        }
    }

    private void ResetHidingSpot()
    {
        if (warningHeartbeatSource != null) warningHeartbeatSource.Stop();
        if (tinnitusSource != null) tinnitusSource.Stop();
        _timeInSpot = 0f;
        _currentStage = 0;
    }
}
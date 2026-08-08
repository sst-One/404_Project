using UnityEngine;
using UnityEngine.Events;

public class HidingSpotManager : MonoBehaviour
{
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

    private float _timeInSpot = 0f;
    private int _currentStage = 0;
    private Transform _playerTransform;
    private Vector3 _lastKnownPosition;

    private void Start()
    {
        // 씬 로드 시에만 플레이어를 찾음
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _lastKnownPosition = _playerTransform.position;
        }
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) return;

        if (_playerTransform == null) return;

        if (Vector3.Distance(_playerTransform.position, _lastKnownPosition) > 1.5f)
        {
            ResetHidingSpot();
        }
        else
        {
            _timeInSpot += Time.deltaTime;
            CheckDegradeStages();
        }
    }

    private void CheckDegradeStages()
    {
        if (_timeInSpot >= forcedTime && _currentStage < 3)
        {
            _currentStage = 3;
            if (StateManager.Instance != null)
            {
                StateManager.Instance.AddHeartbeat(1);
                StateManager.Instance.AddNoise(1.0f, _playerTransform.position);
            }
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
        if (_timeInSpot >= unstableTime)
        {
            if (warningHeartbeatSource != null) warningHeartbeatSource.Stop();
            if (tinnitusSource != null) tinnitusSource.Stop();
        }

        _timeInSpot = 0f;
        _currentStage = 0;
        _lastKnownPosition = _playerTransform.position;
    }
}
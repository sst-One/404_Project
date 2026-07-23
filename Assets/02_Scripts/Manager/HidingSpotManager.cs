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

    [Header("오디오 피드백")]
    public AudioSource warningHeartbeatSource;
    public AudioSource tinnitusSource;

    private float _timeInSpot = 0f;
    private int _currentStage = 0;

    private Transform _playerTransform;
    private Vector3 _lastKnownPosition;

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            _playerTransform = playerObj.transform;
            _lastKnownPosition = _playerTransform.position;
        }
    }

    private void Update()
    {
        bool isChaseStage = (GameFlowManager.Instance != null &&
                             GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                             GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);

        if (!isChaseStage) return;

        if (_playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) _playerTransform = playerObj.transform;
            return;
        }

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
            Debug.Log("[HidingSpotManager] Forced (60s): 은신처 붕괴. 적 강제 유도.");
            if (StateManager.Instance != null)
            {
                StateManager.Instance.AddHeartbeat(1);
                StateManager.Instance.AddNoise(1.0f, _playerTransform.position);
            }
        }
        else if (_timeInSpot >= failingTime && _currentStage < 2)
        {
            _currentStage = 2;
            Debug.Log("[HidingSpotManager] Failing (45s): 은신처 심각한 불안정.");
            if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
            if (tinnitusSource != null) tinnitusSource.Play();
            onFailingWarning?.Invoke();
        }
        else if (_timeInSpot >= unstableTime && _currentStage < 1)
        {
            _currentStage = 1;
            Debug.Log("[HidingSpotManager] Unstable (30s): 은신처 불안정 전조.");
            if (warningHeartbeatSource != null) warningHeartbeatSource.Play();
            onUnstableWarning?.Invoke();
        }
    }

    private void ResetHidingSpot()
    {
        if (_timeInSpot >= unstableTime)
        {
            Debug.Log("[HidingSpotManager] 새로운 위치 이동. 체류 타이머 및 경고 사운드 리셋.");
            if (warningHeartbeatSource != null) warningHeartbeatSource.Stop();
            if (tinnitusSource != null) tinnitusSource.Stop();
        }

        _timeInSpot = 0f;
        _currentStage = 0;
        _lastKnownPosition = _playerTransform.position;
    }
}
using UnityEngine;

public class HidingSpotManager : MonoBehaviour
{
    [Header("체류 시간 임계값 (PARAM-028~030)")]
    public float unstableTime = 30f;
    public float failingTime = 45f;
    public float forcedTime = 60f;

    private float _timeInSpot = 0f;
    private int _currentStage = 0;

    private Transform _playerTransform;
    private Vector3 _lastKnownPosition;

    private void Start()
    {
        if (PlayerMovement.Instance != null)
        {
            _playerTransform = PlayerMovement.Instance.transform;
            _lastKnownPosition = _playerTransform.position;
        }
    }

    private void Update()
    {
        // 핵심 추격 스테이지(Stage 8~11)에서만 작동하도록 제한
        bool isChaseStage = (GameFlowManager.Instance != null &&
                             GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                             GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);

        if (!isChaseStage) return;

        if (_playerTransform == null)
        {
            if (PlayerMovement.Instance != null) _playerTransform = PlayerMovement.Instance.transform;
            return;
        }

        // [핵심 수정] 플레이어의 물리적 위치가 이전보다 1.5m 이상 변했을 때만 은신처 이동으로 간주
        if (Vector3.Distance(_playerTransform.position, _lastKnownPosition) > 1.5f)
        {
            ResetHidingSpot();
        }
        else
        {
            // 같은 자리에 머물고 있다면 스페이스바(Freeze) 조작 여부와 무관하게 체류 시간 누적
            _timeInSpot += Time.deltaTime;
            CheckDegradeStages();
        }
    }

    private void CheckDegradeStages()
    {
        if (_timeInSpot >= forcedTime && _currentStage < 3)
        {
            _currentStage = 3;
            Debug.Log("[HidingSpotManager] Forced (60s): 은신처 붕괴! 강제 발각 위기. (이동 강요)");
            if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        }
        else if (_timeInSpot >= failingTime && _currentStage < 2)
        {
            _currentStage = 2;
            Debug.Log("[HidingSpotManager] Failing (45s): 은신처 심각한 불안정. 당장 이동하십시오.");
            if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        }
        else if (_timeInSpot >= unstableTime && _currentStage < 1)
        {
            _currentStage = 1;
            Debug.Log("[HidingSpotManager] Unstable (30s): 은신처가 불안정해집니다. 전조 발생.");
        }
    }

    private void ResetHidingSpot()
    {
        _timeInSpot = 0f;
        _currentStage = 0;
        _lastKnownPosition = _playerTransform.position; // 새로운 위치를 원점으로 갱신
        Debug.Log("[HidingSpotManager] 새로운 위치로 이동 완료. 은신처 고착 타이머 리셋.");
    }
}
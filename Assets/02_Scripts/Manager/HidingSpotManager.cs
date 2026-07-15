using UnityEngine;

public class HidingSpotManager : MonoBehaviour
{
    [Header("체류 시간 임계값 (PARAM-028~030)")]
    public float unstableTime = 30f;
    public float failingTime = 45f;
    public float forcedTime = 60f;

    private float _timeInSpot = 0f;
    private bool _isHiding = false;
    private int _currentStage = 0;

    private void Update()
    {
        // 플레이어가 Origin Freeze 상태를 유지 중일 때를 은신처 체류로 간주
        bool isFreezing = FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing;

        if (isFreezing)
        {
            _isHiding = true;
            _timeInSpot += Time.deltaTime;

            CheckDegradeStages();
        }
        else
        {
            if (_isHiding) ResetHidingSpot();
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
        _isHiding = false;
        _currentStage = 0;
        Debug.Log("[HidingSpotManager] 은신처 이탈 및 타이머 리셋 완료.");
    }
}
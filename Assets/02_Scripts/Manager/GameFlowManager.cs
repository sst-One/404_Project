using UnityEngine;
using System;

public enum GameStage
{
    Title, Stage1_Elevator, Stage2_Room, Stage3_Anomaly, Stage4_Man,
    Stage5_Clue, Stage6_Blackout, Stage7_Gem, Stage8_Intruder,
    Stage9_Hide, Stage10_Pressure, Stage11_Call, Stage12_Police, Stage13_Ending
}

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Current Status")]
    public GameStage currentStage = GameStage.Title;
    private GameStage _lastCheckpoint = GameStage.Title;

    public event Action<GameStage> OnStageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AdvanceToStage(GameStage nextStage)
    {
        if (currentStage == nextStage) return;

        Debug.Log($"[GameFlowManager] 스테이지 전환: {currentStage} -> {nextStage}");
        currentStage = nextStage;
        OnStageChanged?.Invoke(currentStage);

        // 핵심 스테이지 진입 시 자동 체크포인트 저장 (POL-010)
        if (nextStage == GameStage.Stage1_Elevator || nextStage == GameStage.Stage6_Blackout ||
            nextStage == GameStage.Stage8_Intruder || nextStage == GameStage.Stage11_Call)
        {
            SaveCheckpoint(nextStage);
        }
    }

    private void SaveCheckpoint(GameStage stage)
    {
        _lastCheckpoint = stage;
        Debug.Log($"[GameFlowManager] 체크포인트 저장됨: {_lastCheckpoint}");
    }

    public void RetryFromCheckpoint()
    {
        Debug.Log($"[GameFlowManager] 체크포인트에서 재시작: {_lastCheckpoint}");
        AdvanceToStage(_lastCheckpoint);

        if (StateManager.Instance != null)
        {
            StateManager.Instance.RemoveThreat();
            // Heartbeat, Noise 등 상태 초기화 로직 추가 호출
        }
    }
}
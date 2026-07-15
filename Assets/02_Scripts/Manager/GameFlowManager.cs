using UnityEngine;
using UnityEngine.SceneManagement;
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
    }

    public void AdvanceToStage(GameStage nextStage)
    {
        if (currentStage == nextStage) return;

        Debug.Log($"[GameFlowManager] 스테이지 전환: {currentStage} -> {nextStage}");
        currentStage = nextStage;

        string targetScene = GetSceneNameForStage(nextStage);
        if (SceneManager.GetActiveScene().name != targetScene)
        {
            Debug.Log($"[GameFlowManager] 물리적 씬 로드 요청: {targetScene}");
            SceneManager.LoadScene(targetScene);
        }

        OnStageChanged?.Invoke(currentStage);

        if (nextStage == GameStage.Stage1_Elevator || nextStage == GameStage.Stage6_Blackout ||
            nextStage == GameStage.Stage8_Intruder || nextStage == GameStage.Stage11_Call)
        {
            SaveCheckpoint(nextStage);
        }
    }

    private string GetSceneNameForStage(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Title:
                return "01_Title";

            case GameStage.Stage1_Elevator:
            case GameStage.Stage3_Anomaly:
            case GameStage.Stage4_Man:
                return "02_Elevator";

            case GameStage.Stage2_Room:
            case GameStage.Stage5_Clue:
            case GameStage.Stage6_Blackout:
            case GameStage.Stage8_Intruder:
            case GameStage.Stage9_Hide:
            case GameStage.Stage10_Pressure:
            case GameStage.Stage11_Call:
                return "04_Room404";

            case GameStage.Stage7_Gem:
                return "03_Corridor_4F";

            case GameStage.Stage12_Police:
            case GameStage.Stage13_Ending:
                return "05_Ending";

            default:
                Debug.LogError($"[GameFlowManager] 정의되지 않은 스테이지 로드 시도: {stage}");
                return "04_Room404"; // 안전장치 유지
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
        }
    }
}
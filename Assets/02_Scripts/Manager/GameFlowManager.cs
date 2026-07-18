using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

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

        string targetScene = GetSceneNameForStage(nextStage);
        string currentScene = SceneManager.GetActiveScene().name;

        currentStage = nextStage;

        // 씬이 실제로 다를 때만 페이드 아웃/인 및 비동기 로드 실행
        if (currentScene != targetScene)
        {
            StartCoroutine(LoadSceneWithFade(nextStage, targetScene));
        }
        else
        {
            // 동일 씬 내부의 스테이지 전환 (예: Stage3 -> Stage4)은 페이드 없이 즉시 상태만 변경
            OnStageChanged?.Invoke(currentStage);
            if (nextStage == GameStage.Stage1_Elevator || nextStage == GameStage.Stage6_Blackout ||
                nextStage == GameStage.Stage8_Intruder || nextStage == GameStage.Stage11_Call)
            {
                SaveCheckpoint(nextStage);
            }
        }
    }

    private string GetDayTextForStage(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Stage1_Elevator: return "Day 1";
            case GameStage.Stage3_Anomaly: return "Day 2";
            case GameStage.Stage6_Blackout: return "Day 3";
            case GameStage.Stage8_Intruder: return "Day 4";
            case GameStage.Stage12_Police: return "Day 5";
            default: return "";
        }
    }

    private IEnumerator LoadSceneWithFade(GameStage nextStage, string targetScene)
    {
        string dayText = GetDayTextForStage(nextStage);
        float fadeOutDuration = 2.0f;
        float blackScreenHoldTime = 1.0f;
        float fadeInDuration = 1.0f;

        if (TitleController.Instance != null)
        {
            TitleController.Instance.FadeOutWithText(dayText, fadeOutDuration);
            yield return new WaitForSecondsRealtime(fadeOutDuration);
        }

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        OnStageChanged?.Invoke(currentStage);

        if (nextStage == GameStage.Stage1_Elevator || nextStage == GameStage.Stage6_Blackout ||
            nextStage == GameStage.Stage8_Intruder || nextStage == GameStage.Stage11_Call)
        {
            SaveCheckpoint(nextStage);
        }

        yield return new WaitForSecondsRealtime(blackScreenHoldTime);

        if (TitleController.Instance != null)
        {
            TitleController.Instance.FadeInOnly(fadeInDuration);
        }
    }

    private string GetSceneNameForStage(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Title: return "01_Title";
            case GameStage.Stage1_Elevator: return "02_Stage1";
            case GameStage.Stage2_Room: return "03_Stage2";
            case GameStage.Stage3_Anomaly:
            case GameStage.Stage4_Man: return "04_Stage3_4";
            case GameStage.Stage5_Clue:
            case GameStage.Stage6_Blackout: return "05_Stage5_6";
            case GameStage.Stage7_Gem: return "06_Stage7";
            case GameStage.Stage8_Intruder:
            case GameStage.Stage9_Hide:
            case GameStage.Stage10_Pressure:
            case GameStage.Stage11_Call: return "07_Stage8_11";
            case GameStage.Stage12_Police:
            case GameStage.Stage13_Ending: return "08_Stage12_13";
            default: return "01_Title";
        }
    }

    private void SaveCheckpoint(GameStage stage)
    {
        _lastCheckpoint = stage;
    }

    public void RetryFromCheckpoint()
    {
        AdvanceToStage(_lastCheckpoint);
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();
    }
}
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

    // [핵심 핫픽스] 현재 로드되어 있는 스테이지 씬의 이름을 추적 (SystemInit 제외)
    private string _currentLoadedStageScene = "";

    public event Action<GameStage> OnStageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // [아키텍처 강제] 게임이 00_SystemInit 씬에서 시작되었다면, 타이틀 씬을 가산(Additive) 로드하여 덧붙입니다.
        if (SceneManager.GetActiveScene().name.Contains("SystemInit"))
        {
            AdvanceToStage(GameStage.Title);
        }
    }

    public void AdvanceToStage(GameStage nextStage)
    {
        // 씬 중복 전환 방지
        if (currentStage == nextStage && _currentLoadedStageScene != "") return;
        Debug.Log($"[GameFlowManager] 스테이지 전환: {currentStage} -> {nextStage}");

        string targetScene = GetSceneNameForStage(nextStage);

        // 로드해야 할 씬이 현재 씬과 다를 경우에만 비동기 가산 로드 실행
        if (_currentLoadedStageScene != targetScene)
        {
            StartCoroutine(LoadSceneAdditiveWithFade(nextStage, targetScene));
        }
        else
        {
            // 동일 씬 내부의 스테이지 전환 (예: Stage8 -> Stage9)
            currentStage = nextStage;
            OnStageChanged?.Invoke(currentStage);
            SaveCheckpointIfNecessary(nextStage);
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

    private IEnumerator LoadSceneAdditiveWithFade(GameStage nextStage, string targetScene)
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

        // [핵심 핫픽스 1] 기존에 로드되어 있던 스테이지 씬만 깔끔하게 언로드. (SystemInit 씬은 영구 보존됨)
        if (!string.IsNullOrEmpty(_currentLoadedStageScene))
        {
            AsyncOperation asyncUnload = SceneManager.UnloadSceneAsync(_currentLoadedStageScene);
            if (asyncUnload != null)
            {
                while (!asyncUnload.isDone) yield return null;
            }
        }

        // [핵심 핫픽스 2] 새 스테이지 씬을 '가산(Additive)' 모드로 로드.
        // 유니티가 그래픽 리소스를 파괴하지 않으므로 MediaPipe 네이티브 스레드가 크래시를 일으키지 않음.
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        _currentLoadedStageScene = targetScene;
        currentStage = nextStage;

        // 새로 로드된 스테이지 씬을 Active 상태로 만들어, 조명 및 네비매쉬 데이터를 갱신
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));

        OnStageChanged?.Invoke(currentStage);
        SaveCheckpointIfNecessary(nextStage);

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

    private void SaveCheckpointIfNecessary(GameStage stage)
    {
        if (stage == GameStage.Stage1_Elevator || stage == GameStage.Stage6_Blackout ||
            stage == GameStage.Stage8_Intruder || stage == GameStage.Stage11_Call)
        {
            _lastCheckpoint = stage;
        }
    }

    public void RetryFromCheckpoint()
    {
        AdvanceToStage(_lastCheckpoint);
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();
    }
}
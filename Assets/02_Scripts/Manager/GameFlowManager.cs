// [Core] GameFlowManager.cs (최종 수정본)
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using System.Collections;

public enum GameStage
{
    Title, Tutorial, Stage1_Elevator, Stage2_Room, Stage3_Anomaly, Stage4_Man,
    Stage5_Clue, Stage6_Blackout, Stage7_Gem, Stage8_Intruder,
    Stage9_Hide, Stage10_Pressure, Stage11_Call, Stage12_Police, Stage13_Ending
}

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance { get; private set; }

    [Header("Current Status")]
    public GameStage currentStage = GameStage.Title;
    private GameStage _lastCheckpoint = GameStage.Title;
    private string _currentLoadedStageScene = "";

    [Header("Bootstrapper & Persistence")]
    [Tooltip("씬 전환 시 파괴되지 않아야 할 최상위 프리팹 객체들을 넣으십시오 (Player, UI_Canvas 등)")]
    public GameObject[] persistentRoots;
    public Transform playerRig;
    public Camera mainCamera;

    public event Action<GameStage> OnStageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // [핵심 핫픽스] 자신을 최상위로 올린 후 파괴 방지
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        // 등록된 시스템/플레이어/UI 루트 객체들을 강제로 최상위로 빼고 파괴 방지 적용
        if (persistentRoots != null)
        {
            foreach (GameObject rootObj in persistentRoots)
            {
                if (rootObj != null)
                {
                    rootObj.transform.SetParent(null);
                    DontDestroyOnLoad(rootObj);
                }
            }
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name.Contains("SystemInit")) AdvanceToStage(GameStage.Title);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;
        if (sceneName.Contains("SystemInit")) return;

        GameObject spawnPoint = GameObject.Find("Player_SpawnPoint");
        if (spawnPoint != null && playerRig != null)
        {
            var cc = playerRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerRig.position = spawnPoint.transform.position;
            playerRig.rotation = spawnPoint.transform.rotation;
            if (cc != null) cc.enabled = true;
        }

        if (playerRig != null)
        {
            if (sceneName == "03_Stage2" || sceneName == "05_Stage5_6" ||
                sceneName == "07_Stage8_11" || sceneName == "08_Stage12_13")
            {
                playerRig.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
            else
            {
                playerRig.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            }
        }

        CleanUpDuplicateSystems(scene);
    }

    private void CleanUpDuplicateSystems(Scene scene)
    {
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            if (cam != mainCamera && cam.gameObject.scene.name == scene.name) Destroy(cam.gameObject);
        }

        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            foreach (EventSystem es in eventSystems)
            {
                if (es.gameObject.scene.name == scene.name) Destroy(es.gameObject);
            }
        }
    }

    public void AdvanceToStage(GameStage nextStage)
    {
        if (currentStage == nextStage && _currentLoadedStageScene != "") return;

        string targetScene = GetSceneNameForStage(nextStage);

        if (nextStage == GameStage.Title)
        {
            StartCoroutine(ReturnToTitleRoutine());
            return;
        }

        if (_currentLoadedStageScene != targetScene)
        {
            StartCoroutine(LoadSceneAdditiveWithFade(nextStage, targetScene));
        }
        else
        {
            currentStage = nextStage;
            OnStageChanged?.Invoke(currentStage);
            SaveCheckpointIfNecessary(nextStage);
        }
    }

    private IEnumerator ReturnToTitleRoutine()
    {
        if (TitleController.Instance != null) TitleController.Instance.FadeInOnly(1.0f);

        if (!string.IsNullOrEmpty(_currentLoadedStageScene))
        {
            yield return SceneManager.UnloadSceneAsync(_currentLoadedStageScene);
            _currentLoadedStageScene = "";
        }

        currentStage = GameStage.Title;
        OnStageChanged?.Invoke(currentStage);

        if (TitleController.Instance != null) TitleController.Instance.ShowMainMenu();
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

        if (TitleController.Instance != null)
        {
            if (nextStage == GameStage.Tutorial) TitleController.Instance.FadeOutWithText("", 1.5f);
            else TitleController.Instance.FadeOutWithText(dayText, 2.0f);
            yield return new WaitForSecondsRealtime(2.0f);
        }

        if (!string.IsNullOrEmpty(_currentLoadedStageScene)) yield return SceneManager.UnloadSceneAsync(_currentLoadedStageScene);

        yield return SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

        _currentLoadedStageScene = targetScene;
        currentStage = nextStage;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));
        OnStageChanged?.Invoke(currentStage);
        SaveCheckpointIfNecessary(nextStage);

        yield return new WaitForSecondsRealtime(1.0f);
        if (TitleController.Instance != null) TitleController.Instance.FadeInOnly(1.0f);
    }

    private string GetSceneNameForStage(GameStage stage)
    {
        switch (stage)
        {
            case GameStage.Tutorial: return "01_Tutorial";
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
            default: return "";
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
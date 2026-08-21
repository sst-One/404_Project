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
    public GameObject[] persistentRoots;
    public Transform playerRig;
    public Camera mainCamera;

    public event Action<GameStage> OnStageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

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
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen());

        // [핵심 핫픽스 2] 암전 상태가 화면에 100% 렌더링된 것을 확인하기 위해 1프레임 추가 대기
        yield return new WaitForEndOfFrame();

        if (!string.IsNullOrEmpty(_currentLoadedStageScene))
        {
            yield return SceneManager.UnloadSceneAsync(_currentLoadedStageScene);
            _currentLoadedStageScene = "";
        }

        currentStage = GameStage.Title;
        OnStageChanged?.Invoke(currentStage);

        if (TitleController.Instance != null) TitleController.Instance.ShowMainMenu();

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen());
    }

    private IEnumerator LoadSceneAdditiveWithFade(GameStage nextStage, string targetScene)
    {
        // 1. 화면 완전히 암전
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen());
        }

        // [핵심 핫픽스 2] 암전 상태가 화면에 100% 렌더링된 것을 확인하기 위해 1프레임 추가 대기 (엔진 비동기 버그 차단)
        yield return new WaitForEndOfFrame();

        // 2. 타이틀 화면 정리
        if (currentStage == GameStage.Title && TitleController.Instance != null)
        {
            TitleController.Instance.HideMainMenu();
        }

        // 3. 씬 언로드 및 로드 (이 동안 화면은 완벽하게 100% 까만 상태 보장)
        if (!string.IsNullOrEmpty(_currentLoadedStageScene)) yield return SceneManager.UnloadSceneAsync(_currentLoadedStageScene);
        yield return SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

        _currentLoadedStageScene = targetScene;
        currentStage = nextStage;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(targetScene));
        OnStageChanged?.Invoke(currentStage);
        SaveCheckpointIfNecessary(nextStage);

        // 4. 로딩 버퍼 대기
        yield return new WaitForSecondsRealtime(0.2f);

        // 5. 화면 밝아짐
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeInScreen());
        }
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
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("메인 메뉴 UI")]
    public GameObject titlePanel;
    public GameObject creditsPanel;

    [Header("도입 영상 세팅")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    public GameObject skipPromptUI;

    [Header("버튼 할당")]
    [Tooltip("처음부터 시작 버튼")]
    public Button btnStartGame;
    [Tooltip("저장된 진행 단계부터 이어하기 버튼")]
    public Button btnContinueGame;
    public Button btnSettings;
    public Button btnCredits;
    public Button btnCloseCredits;
    public Button btnQuitGame;

    private Coroutine _transitionCoroutine;
    private bool _isTransitioning;
    private bool _isPlayingIntroVideo;
    private GameStage _stageAfterIntro = GameStage.Stage1_Elevator;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AddButtonListener(btnStartGame, OnClickStartGame);
        AddButtonListener(btnContinueGame, OnClickContinueGame);
        AddButtonListener(btnSettings, OnClickSettings);
        AddButtonListener(btnCredits, OnClickCredits);
        AddButtonListener(btnCloseCredits, OnClickCloseCredits);
        AddButtonListener(btnQuitGame, OnClickQuitGame);

        if (titlePanel != null) titlePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (introVideoPlayer != null) introVideoPlayer.Stop();
        if (skipPromptUI != null) skipPromptUI.SetActive(false);
    }

    private void AddButtonListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null) return;
        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private void Start()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            ShowMainMenu();
            if (UIManager.Instance != null) StartCoroutine(UIManager.Instance.FadeInScreen());
        }
    }

    private void Update()
    {
        if (_isPlayingIntroVideo && Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
            SkipIntroSequence();
    }

    public void ShowMainMenu()
    {
        if (titlePanel != null) titlePanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (btnStartGame != null) btnStartGame.interactable = true;
        if (btnContinueGame != null)
            btnContinueGame.interactable = GameFlowManager.Instance != null && GameFlowManager.Instance.HasSavedGame();

        if (UIManager.Instance != null) UIManager.Instance.ResetTransientUI();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideMainMenu()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
    }

    private void OnClickStartGame()
    {
        if (_isTransitioning) return;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.ResetSavedGameProgress();
        if (btnStartGame != null) btnStartGame.interactable = false;
        if (btnContinueGame != null) btnContinueGame.interactable = false;
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
        _isTransitioning = true;
        _transitionCoroutine = StartCoroutine(StartTutorialRoutine());
    }

    private IEnumerator StartTutorialRoutine()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.0f));
        HideMainMenu();
        if (UIManager.Instance != null) UIManager.Instance.ResetTransientUI();
        _isTransitioning = false;

        // 처음부터 시작은 오프닝 영상이 아니라 튜토리얼부터 진입합니다.
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Tutorial);
    }

    private void OnClickContinueGame()
    {
        if (_isTransitioning || GameFlowManager.Instance == null) return;
        if (!GameFlowManager.Instance.TryGetSavedGameStage(out GameStage savedStage)) return;

        if (btnStartGame != null) btnStartGame.interactable = false;
        if (btnContinueGame != null) btnContinueGame.interactable = false;
        _isTransitioning = true;
        _transitionCoroutine = StartCoroutine(ContinueGameRoutine(savedStage));
    }

    private IEnumerator ContinueGameRoutine(GameStage savedStage)
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1f));
        HideMainMenu();
        if (UIManager.Instance != null) UIManager.Instance.ResetTransientUI();
        _isTransitioning = false;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(savedStage);
    }

    /// <summary>튜토리얼 완료 후 호출합니다. 오프닝 영상을 재생한 다음 지정 스테이지로 이동합니다.</summary>
    public void PlayIntroThenStartStage(GameStage nextStage)
    {
        if (_isTransitioning) return;
        _stageAfterIntro = nextStage;
        _isTransitioning = true;
        _transitionCoroutine = StartCoroutine(IntroVideoRoutine());
    }

    private IEnumerator IntroVideoRoutine()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.0f));

        if (introVideoPanel != null && introVideoPlayer != null)
        {
            introVideoPanel.SetActive(true);
            introVideoPlayer.time = 0;
            _isPlayingIntroVideo = true;
            introVideoPlayer.Play();
            yield return null;
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen(0.8f));
            yield return new WaitForSecondsRealtime(0.7f);
            if (skipPromptUI != null) skipPromptUI.SetActive(true);

            // 준비가 완료된 뒤 재생이 시작되지 않는 경우에도 무한 대기를 피합니다.
            float startWait = 0f;
            while (!introVideoPlayer.isPlaying && startWait < 10f)
            {
                startWait += Time.unscaledDeltaTime;
                yield return null;
            }
            while (introVideoPlayer.isPlaying) yield return null;

            if (skipPromptUI != null) skipPromptUI.SetActive(false);
            _isPlayingIntroVideo = false;
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.25f);
        }

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.0f));
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        _isTransitioning = false;
        _transitionCoroutine = null;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(_stageAfterIntro);
    }

    private void SkipIntroSequence()
    {
        if (!_isPlayingIntroVideo) return;
        _isPlayingIntroVideo = false;
        if (_transitionCoroutine != null) StopCoroutine(_transitionCoroutine);
        _transitionCoroutine = null;
        if (introVideoPlayer != null) introVideoPlayer.Stop();
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (skipPromptUI != null) skipPromptUI.SetActive(false);
        _isTransitioning = false;
        StartCoroutine(CompleteIntroAfterSkipRoutine());
    }

    private IEnumerator CompleteIntroAfterSkipRoutine()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(0.1f));
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(_stageAfterIntro);
    }

    private void OnClickSettings()
    {
        if (UIManager.Instance != null) UIManager.Instance.TogglePause();
    }

    private void OnClickCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private void OnClickCloseCredits()
    {
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

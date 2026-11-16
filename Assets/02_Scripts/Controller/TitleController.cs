using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using TMPro;

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
    private float _introSkipHeldTime;
    private GameStage _stageAfterIntro = GameStage.Stage1_Elevator;
    internal Texture MenuBackdrop { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        ApplyReferenceLayout();
        AddButtonListener(btnStartGame, RequestNewGame);
        AddButtonListener(btnContinueGame, OnClickContinueGame);
        AddButtonListener(btnSettings, OnClickSettings);
        AddButtonListener(btnCredits, OnClickCredits);
        AddButtonListener(btnCloseCredits, OnClickCloseCredits);
        AddButtonListener(btnQuitGame, RequestQuitGame);

        // 메인 메뉴 주요 버튼에 마우스 오버 시 텍스트 강조와 세로 바를 함께 표시합니다.
        AddHoverFeedback(btnStartGame);
        AddHoverFeedback(btnContinueGame);
        AddHoverFeedback(btnSettings);
        AddHoverFeedback(btnCredits);
        AddHoverFeedback(btnQuitGame);

        if (titlePanel != null) titlePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (introVideoPlayer != null) introVideoPlayer.Stop();
        if (skipPromptUI != null) skipPromptUI.SetActive(false);
    }

    private void ApplyReferenceLayout()
    {
        if (titlePanel != null)
        {
            Transform background = titlePanel.transform.Find("TitleBackground");
            if (background != null && background.TryGetComponent(out RawImage image))
            {
                MenuBackdrop = image.texture;
                image.raycastTarget = false;
            }

            Transform heading = titlePanel.transform.Find("GameTitle_Txt");
            if (heading != null && heading.TryGetComponent(out TMP_Text title))
            {
                Place(title.rectTransform, 0.10f, 0.69f, 0.43f, 0.86f);
                title.text = "<cspace=0.18em>404</cspace>";
                title.alignment = TextAlignmentOptions.MidlineLeft;
                title.fontStyle = FontStyles.Bold;
                title.enableAutoSizing = true;
                title.fontSizeMin = 56f;
                title.fontSizeMax = 128f;
            }
        }

        PlaceMainAction(btnContinueGame, "이어하기", 0.63f);
        PlaceMainAction(btnStartGame, "새 게임", 0.562f);
        PlaceMainAction(btnSettings, "설정", 0.494f);
        PlaceMainAction(btnQuitGame, "게임 종료", 0.426f);
        PlaceMainAction(btnCredits, "제작진", 0.358f);

        if (skipPromptUI != null && skipPromptUI.TryGetComponent(out TMP_Text skip))
        {
            Place(skip.rectTransform, 0.59f, 0.03f, 0.95f, 0.09f);
            skip.alignment = TextAlignmentOptions.MidlineRight;
            skip.fontSize = 22f;
            skip.text = "S 키를 길게 눌러 건너뛰기";
        }
    }

    private void PlaceMainAction(Button button, string label, float top)
    {
        if (button == null) return;
        Place((RectTransform)button.transform, 0.095f, top - 0.058f, 0.30f, top);
        if (button.targetGraphic != null)
            button.targetGraphic.color = new Color(0f, 0f, 0f, 0.025f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return;
        Place(text.rectTransform, 0.09f, 0f, 1f, 1f);
        text.text = label;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.fontSize = 27f;
    }

    private static void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = new Vector2(left, bottom);
        rect.anchorMax = new Vector2(right, top);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }


    private void AddHoverFeedback(Button button)
    {
        if (button == null) return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        Text legacyLabel = label == null ? button.GetComponentInChildren<Text>(true) : null;
        if (label == null && legacyLabel == null) return;
        Color normalColor = label != null ? label.color : legacyLabel.color;
        Color hoverColor = Color.white;
        FontStyles normalTmpStyle = label != null ? label.fontStyle : FontStyles.Normal;
        FontStyle normalLegacyStyle = legacyLabel != null ? legacyLabel.fontStyle : FontStyle.Normal;

        Transform existing = button.transform.Find("Runtime_Hover_Bar");
        GameObject barObject = existing != null ? existing.gameObject : new GameObject("Runtime_Hover_Bar", typeof(RectTransform), typeof(Image));
        if (existing == null) barObject.transform.SetParent(button.transform, false);

        RectTransform barRect = barObject.GetComponent<RectTransform>();
        barRect.anchorMin = new Vector2(0f, 0.18f);
        barRect.anchorMax = new Vector2(0f, 0.82f);
        barRect.pivot = new Vector2(0f, 0.5f);
        barRect.anchoredPosition = new Vector2(2f, 0f);
        barRect.sizeDelta = new Vector2(4f, 0f);
        Image barImage = barObject.GetComponent<Image>();
        barImage.color = Color.white;
        barImage.raycastTarget = false;
        barObject.SetActive(false);

        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) trigger = button.gameObject.AddComponent<EventTrigger>();
        if (trigger.triggers == null) trigger.triggers = new System.Collections.Generic.List<EventTrigger.Entry>();

        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ =>
        {
            if (label != null) { label.color = hoverColor; label.fontStyle = FontStyles.Bold; }
            if (legacyLabel != null) { legacyLabel.color = hoverColor; legacyLabel.fontStyle = FontStyle.Bold; }
            if (barObject != null) barObject.SetActive(true);
        });
        trigger.triggers.Add(enter);

        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ =>
        {
            if (label != null) { label.color = normalColor; label.fontStyle = normalTmpStyle; }
            if (legacyLabel != null) { legacyLabel.color = normalColor; legacyLabel.fontStyle = normalLegacyStyle; }
            if (barObject != null) barObject.SetActive(false);
        });
        trigger.triggers.Add(exit);
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
        if (!_isPlayingIntroVideo || (skipPromptUI != null && !skipPromptUI.activeInHierarchy)) return;
        bool isHeld = Keyboard.current != null && Keyboard.current.sKey.isPressed;
        _introSkipHeldTime = isHeld ? _introSkipHeldTime + Time.unscaledDeltaTime : 0f;
        if (_introSkipHeldTime >= 0.8f) SkipIntroSequence();
    }

    public void ShowMainMenu()
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayBGM("SND-076_메인화면 배경음_Loop");
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
        if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();
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

    private void RequestNewGame()
    {
        if (_isTransitioning) return;
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.HasSavedGame() && UIManager.Instance != null && titlePanel != null)
            UIManager.Instance.ShowMenuDialog(titlePanel.transform, "현재 진행 기록을 초기화할까요?",
                "새 게임을 시작하면 기존 저장 기록이 삭제됩니다.", "새 게임 시작", OnClickStartGame);
        else
            OnClickStartGame();
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
        PlayerController player = PlayerController.Instance;
        VisionTrackingManager tracking = VisionTrackingManager.Instance;
        bool wantsCamera = player != null && player.useWebcam && tracking != null &&
            (tracking.HasSavedCalibration || tracking.IsCalibrated);
        bool restored = false;
        if (wantsCamera)
        {
            yield return StartCoroutine(tracking.RestoreCameraForContinue());
            restored = tracking.IsCameraReady && tracking.IsCalibrated && tracking.isTracking;
            if (restored) player.CompleteInputSetup(false);
        }

        if (UIManager.Instance != null && player != null && (!player.IsInputSetupComplete || (wantsCamera && !restored)))
        {
            yield return StartCoroutine(UIManager.Instance.RunCameraSetup(requireDecision: true));
            if (!PlayerController.Instance.IsInputSetupComplete)
            {
                _isTransitioning = false;
                ShowMainMenu();
                yield break;
            }
        }

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
            _introSkipHeldTime = 0f;
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
        if (UIManager.Instance != null) UIManager.Instance.OpenSettings();
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

    private void RequestQuitGame()
    {
        if (UIManager.Instance != null && titlePanel != null)
            UIManager.Instance.ShowMenuDialog(titlePanel.transform, "게임을 종료할까요?",
                "게임을 종료합니다.", "종료", OnClickQuitGame);
        else
            OnClickQuitGame();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

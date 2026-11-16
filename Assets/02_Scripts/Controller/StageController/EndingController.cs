using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingController : MonoBehaviour
{
    [Header("Ending Scene UI (기존 씬 연결 유지)")]
    public CanvasGroup titleCanvasGroup;
    public RectTransform creditsRectTransform;
    public float titleHoldTime = 2f;
    public float titleFadeTime = 1.5f;
    public float creditScrollSpeed = 50f;
    public float endDelay = 3f;
    public float overrideTargetY = 0f;

    [Header("Ending Audio")]
    public AudioSource endingBgmSource;
    public string endingBgmClipName = "SND-065_EndingTheme_Loop";

    private PlayerController _playerController;
    private FirstPersonCameraLook _cameraLook;
    private bool _playerControllerWasEnabled;
    private bool _cameraLookWasEnabled;
    private bool _controlsLocked;
    private bool _isReturningToTitle;
    private Coroutine _endingRoutine;
    private GameObject _skipButtonObject;
    private GameObject _interpretationPanel;
    private Canvas _endingCanvas;

    private void Start()
    {
        LockPlayerControls();

        // 엔딩에 도달하면 이어하기 저장 데이터를 제거합니다.
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResetSavedGameProgress();

        StartEndingMusic();
        PrepareEndingTexts();
        _endingRoutine = StartCoroutine(EndingSequenceRoutine());
    }

    private void LockPlayerControls()
    {
        _playerController = PlayerController.Instance;
        if (_playerController != null)
        {
            _playerControllerWasEnabled = _playerController.enabled;
            _playerController.SetMovementLock(true);
            _playerController.SetCameraLock(true);
            _cameraLook = _playerController.CamLook;
        }

        if (_cameraLook == null)
            _cameraLook = FindObjectOfType<FirstPersonCameraLook>();

        if (_cameraLook != null)
            _cameraLookWasEnabled = _cameraLook.enabled;

        // 락 플래그로 차단되지 않는 상호작용 입력까지 막습니다.
        if (_playerController != null)
            _playerController.enabled = false;
        if (_cameraLook != null)
            _cameraLook.enabled = false;

        _controlsLocked = true;
    }

    private void RestorePlayerControls()
    {
        if (!_controlsLocked) return;

        if (_playerController != null)
        {
            _playerController.enabled = _playerControllerWasEnabled;
            _playerController.SetMovementLock(false);
            _playerController.SetCameraLock(false);
        }

        if (_cameraLook != null)
            _cameraLook.enabled = _cameraLookWasEnabled;

        _controlsLocked = false;
    }

    private void StartEndingMusic()
    {
        AudioClip clip = AudioManager.Instance != null
            ? AudioManager.Instance.GetClip(endingBgmClipName)
            : null;

        if (endingBgmSource != null)
        {
            if (clip != null) endingBgmSource.clip = clip;
            if (endingBgmSource.clip != null)
            {
                endingBgmSource.loop = true;
                endingBgmSource.Play();
                return;
            }
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(endingBgmClipName))
            AudioManager.Instance.PlayBGM(endingBgmClipName);
    }

    private void PrepareEndingTexts()
    {
        if (titleCanvasGroup != null)
        {
            TMP_Text title = titleCanvasGroup.GetComponentInChildren<TMP_Text>(true);
            if (title != null) title.text = "THE END";
            _endingCanvas = titleCanvasGroup.GetComponentInParent<Canvas>();
        }
        if (_endingCanvas == null && creditsRectTransform != null)
            _endingCanvas = creditsRectTransform.GetComponentInParent<Canvas>();

        if (creditsRectTransform != null)
        {
            TMP_Text credits = creditsRectTransform.GetComponent<TMP_Text>();
            if (credits != null)
            {
                credits.text = "제작진\n\n404\n\n" +
                    "<color=#999999>기획 · 디자인</color>\n김혜진\n\n" +
                    "<color=#999999>개발</color>\n양석원\n\n" +
                    "<color=#999999>사운드 · 디자인</color>\n이우현\n\n" +
                    "<color=#999999>3D 디자인</color>\n주연선";
                credits.alignment = TextAlignmentOptions.Center;
                credits.lineSpacing = 6f;
                credits.enableAutoSizing = true;
                credits.fontSizeMin = 15f;
                credits.fontSizeMax = 30f;
                creditsRectTransform.anchorMin = new Vector2(0.2f, 0.25f);
                creditsRectTransform.anchorMax = new Vector2(0.8f, 0.93f);
                creditsRectTransform.sizeDelta = Vector2.zero;
                credits.ForceMeshUpdate();
                RectTransform parent = creditsRectTransform.parent as RectTransform;
                float height = parent != null ? parent.rect.height : Screen.height;
                float riseDistance = Mathf.Max(1f, height * (creditsRectTransform.anchorMax.y + 0.04f));
                creditsRectTransform.anchoredPosition = Vector2.down * riseDistance;
                creditsRectTransform.gameObject.SetActive(false);
            }
        }
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // v07 TITLE-030: THE END를 먼저 보여주고 404 타이틀로 전환한 뒤 크레딧을 시작합니다.
        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 1f;
            if (titleHoldTime > 0f)
                yield return new WaitForSecondsRealtime(titleHoldTime);
            TMP_Text title = titleCanvasGroup.GetComponentInChildren<TMP_Text>(true);
            if (title != null) title.text = "404";
            for (int flash = 0; flash < 3; flash++)
            {
                titleCanvasGroup.alpha = 0.15f;
                yield return new WaitForSecondsRealtime(0.12f);
                titleCanvasGroup.alpha = 1f;
                yield return new WaitForSecondsRealtime(0.2f);
            }

            float duration = Mathf.Max(0f, titleFadeTime);
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    titleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }
            titleCanvasGroup.alpha = 0f;
            titleCanvasGroup.gameObject.SetActive(false);
        }

        if (UIManager.Instance != null) UIManager.Instance.IsEndingPointerEnabled = true;
        if (creditsRectTransform != null) creditsRectTransform.gameObject.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // 하단까지 화면을 벗어난 뒤 해석 버튼을 표시합니다.
        if (creditsRectTransform != null && creditScrollSpeed > 0f)
        {
            RectTransform parent = creditsRectTransform.parent as RectTransform;
            float parentHeight = parent != null ? parent.rect.height : Screen.height;
            float targetY = Mathf.Max(overrideTargetY, parentHeight * (1f - creditsRectTransform.anchorMin.y) + 24f);
            float speed = Mathf.Max(creditScrollSpeed, (targetY - creditsRectTransform.anchoredPosition.y) / 15f);

            while (creditsRectTransform != null &&
                   creditsRectTransform.anchoredPosition.y < targetY)
            {
                Vector2 position = creditsRectTransform.anchoredPosition;
                position.y = Mathf.Min(targetY, position.y + speed * Time.unscaledDeltaTime);
                creditsRectTransform.anchoredPosition = position;
                yield return null;
            }
        }
        else if (creditsRectTransform != null)
        {
            creditsRectTransform.anchoredPosition = new Vector2(0f, overrideTargetY);
        }
        else if (creditsRectTransform == null)
        {
            Debug.LogWarning("[EndingController] 크레딧 RectTransform이 연결되지 않아 크레딧 스크롤을 건너뜁니다.", this);
        }

        // v07: 크레딧 종료 후 자동으로 타이틀로 빠져나가지 않고 해석 보기 버튼을 대기합니다.
        CreateEndingActionButton("해석 보기", ShowInterpretation);
    }

    private void CreateEndingActionButton(string labelText, UnityEngine.Events.UnityAction action)
    {
        if (_skipButtonObject != null) Destroy(_skipButtonObject);
        if (_endingCanvas == null && creditsRectTransform != null)
            _endingCanvas = creditsRectTransform.GetComponentInParent<Canvas>();
        if (_endingCanvas == null) return;

        _skipButtonObject = new GameObject("Ending_ActionButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline), typeof(Button));
        RectTransform buttonRect = _skipButtonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(_endingCanvas.transform.Find("Game_Viewport") ?? _endingCanvas.transform, false);
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.sizeDelta = new Vector2(240f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, 48f);

        Image background = _skipButtonObject.GetComponent<Image>();
        background.color = new Color(0.02f, 0.02f, 0.02f, 0.92f);
        background.raycastTarget = true;
        background.type = Image.Type.Simple;
        Outline border = _skipButtonObject.GetComponent<Outline>();
        border.effectColor = new Color(0.35f, 0.35f, 0.35f, 1f);
        border.effectDistance = new Vector2(1f, -1f);

        Button button = _skipButtonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.78f, 0.84f, 0.9f, 1f);
        colors.pressedColor = new Color(0.6f, 0.68f, 0.76f, 1f);
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_Text sourceCredits = creditsRectTransform != null ? creditsRectTransform.GetComponent<TMP_Text>() : null;
        if (sourceCredits != null) label.font = sourceCredits.font;
        label.text = labelText;
        label.fontSize = 22f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;
        buttonRect.SetAsLastSibling();
    }

    private void ShowInterpretation()
    {
        if (_skipButtonObject != null) { Destroy(_skipButtonObject); _skipButtonObject = null; }
        if (_interpretationPanel != null)
        {
            _interpretationPanel.SetActive(true);
            CreateEndingActionButton("메인 화면으로", ReturnToTitle);
            return;
        }
        if (_endingCanvas == null) return;

        if (creditsRectTransform != null) creditsRectTransform.gameObject.SetActive(false);
        _interpretationPanel = new GameObject("Ending_Interpretation_Panel", typeof(RectTransform), typeof(Image));
        RectTransform panelRect = _interpretationPanel.GetComponent<RectTransform>();
        panelRect.SetParent(_endingCanvas.transform.Find("Game_Viewport") ?? _endingCanvas.transform, false);
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        Image panelBackground = _interpretationPanel.GetComponent<Image>();
        panelBackground.color = Color.black;
        panelBackground.raycastTarget = true;

        CreateInterpretationText("엔딩 해석", new Vector2(0.25f, 0.89f), new Vector2(0.75f, 0.96f), 30f, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateInterpretationText("404", new Vector2(0.35f, 0.75f), new Vector2(0.65f, 0.88f), 92f, TextAlignmentOptions.Center, FontStyles.Bold);
        CreateInterpretationText("01  반전\n그가 건넨 빨간 보석은 주인공의 새끼손가락 반지에 꼭 들어맞습니다.\n되살아난 살인의 기억이 밝히는 범인은, 쫓기던 주인공 자신입니다.", new Vector2(0.16f, 0.59f), new Vector2(0.84f, 0.72f), 20f, TextAlignmentOptions.Center, FontStyles.Normal);
        CreateInterpretationText("02  숨겨진 단서\n발견된 얼굴 가면과 사라지는 피의 환영은 감춰진 범행의 흔적입니다.\n주인공이 믿었던 회사 생활도 착각이었고, 아내를 잃은 경찰은 그를 홀로 추적했습니다.", new Vector2(0.13f, 0.42f), new Vector2(0.87f, 0.55f), 20f, TextAlignmentOptions.Center, FontStyles.Normal);
        CreateInterpretationText("03  404 / NOT FOUND\n찾지 못한 것은 방이 아니라, 주인공이 잃어버린 자신의 얼굴과 기억입니다.", new Vector2(0.18f, 0.29f), new Vector2(0.82f, 0.37f), 20f, TextAlignmentOptions.Center, FontStyles.Normal);
        CreateEndingActionButton("메인 화면으로", ReturnToTitle);
    }

    private void CreateInterpretationText(string value, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment, FontStyles style)
    {
        GameObject textObject = new GameObject("Interpretation_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(_interpretationPanel.transform, false);
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        TMP_Text sourceCredits = creditsRectTransform != null ? creditsRectTransform.GetComponent<TMP_Text>() : null;
        if (sourceCredits != null) text.font = sourceCredits.font;
        text.text = value;
        text.fontSize = fontSize;
        text.enableAutoSizing = true;
        text.fontSizeMin = 13f;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.fontStyle = style;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void ReturnToTitle()
    {
        if (_isReturningToTitle) return;
        _isReturningToTitle = true;

        if (_endingRoutine != null)
        {
            StopCoroutine(_endingRoutine);
            _endingRoutine = null;
        }

        if (_skipButtonObject != null)
        {
            Destroy(_skipButtonObject);
            _skipButtonObject = null;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetSavedGameProgress();
            // GameFlowManager가 페이드아웃 → 엔딩 씬 언로드 → 메인 메뉴 표시를 처리합니다.
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
        else
        {
            Debug.LogError("[EndingController] GameFlowManager를 찾지 못해 메인 화면으로 돌아갈 수 없습니다.", this);
            RestorePlayerControls();
        }
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null) UIManager.Instance.IsEndingPointerEnabled = false;
        RestorePlayerControls();
    }
}

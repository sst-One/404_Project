using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;
using System;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Core UI Panels")]
    public GameObject systemMenuPanel;
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Global Screen Fade")]
    public CanvasGroup globalFadeCanvasGroup;
    public float defaultFadeDuration = 0.4f;

    [Header("Day Transition UI (UI-005)")]
    public GameObject dayTransitionPanel;
    public RawImage dayTransitionImage;
    public TextMeshProUGUI dayTransitionText;
    public Texture[] dayTextures;

    [Header("ESC Menu: Camera Setup")]
    public TMP_Dropdown cameraDropdown;

    [Header("ESC Menu: Buttons")]
    public Button btnResume;
    public Button btnQuit;
    public Button btnRecalibrate;

    [Header("ESC Menu: Sliders & Toggles")]
    // 구버전 씬 호환용. 새 UI에서는 가로/세로 감도 슬라이더를 사용합니다.
    public Slider rotationSpeedSlider;
    public Slider horizontalSensitivitySlider;
    public Slider verticalSensitivitySlider;
    public Slider brightnessSlider;
    [Tooltip("게임 설정 탭의 실내 조명 밝기 슬라이더. 별도 Slider를 생성해 연결하세요.")]
    public Slider interiorBrightnessSlider;
    public TextMeshProUGUI interiorBrightnessValueText;
    public Slider leanThresholdSlider;
    public Slider reachThresholdSlider;
    public Slider deadzoneSlider;
    public Toggle invertYawToggle;

    [Header("ESC Menu: Value Texts")]
    public TextMeshProUGUI rotationValueText;
    public TextMeshProUGUI horizontalSensitivityValueText;
    public TextMeshProUGUI verticalSensitivityValueText;
    public TextMeshProUGUI brightnessValueText;
    public TextMeshProUGUI leanValueText;
    public TextMeshProUGUI reachValueText;
    public TextMeshProUGUI deadzoneValueText;

    public bool IsPaused { get; private set; } = false;
    internal bool IsEndingPointerEnabled { get; set; }
    public bool IsDialogueActive { get; set; } = false;
    public bool isDayTransitioning { get; private set; } = false;

    private FirstPersonCameraLook _cameraLook;
    private List<VideoPlayer> _pausedVideos = new List<VideoPlayer>();
    private enum SettingsTab { Display, Audio, Controls, Gaze }
    private readonly Button[] _settingsTabs = new Button[4];
    private readonly GameObject[] _settingsPages = new GameObject[4];
    private ScrollRect _settingsScroll;
    private GameObject _pauseMenu;
    private RawImage _menuBackdrop;
    private GameObject _menuDialog;
    private Toggle _runtimeInvertPitchToggle;
    private Button _displayModeButton;
    private TextMeshProUGUI _resolutionLabel;
    private GameObject _cameraSetupRoot;
    private Texture2D _calibrationDotTexture;
    private int _cameraSetupAction;
    private bool _isCameraSetupOpen;
    private const float GameAspectRatio = 16f / 9f;
    private readonly List<RectTransform> _screenContents = new List<RectTransform>();
    private Canvas _vignetteCanvas;
    private Canvas _letterboxCanvas;
    private RectTransform[] _letterboxBars;
    private int _viewportWidth;
    private int _viewportHeight;
    private bool _settingsTabsInitialized;

    // 설정 UI는 전부 런타임에 생성합니다. 기존 Inspector 연결은 구버전 씬 호환용으로만 남깁니다.
    private GameObject _runtimeSettingsRoot;
    private GameObject _controlsSettingsGroup;
    private GameObject _gameSettingsGroup;
    private Button _cameraCycleButton;
    private TextMeshProUGUI _cameraCycleLabel;
    private readonly List<string> _availableCameraNames = new List<string>();
    private int _selectedCameraIndex;
    private Toggle _runtimeInvertYawToggle;
    private TextMeshProUGUI _runtimeInvertYawLabel;
    private TextMeshProUGUI _runtimeHorizontalValue;
    private TextMeshProUGUI _runtimeVerticalValue;
    private TextMeshProUGUI _runtimeLeanValue;
    private TextMeshProUGUI _runtimeReachValue;
    private TextMeshProUGUI _runtimeDeadzoneValue;
    private TextMeshProUGUI _runtimeScreenBrightnessValue;
    private TextMeshProUGUI _runtimeInteriorBrightnessValue;
    private Slider _vignetteSlider;
    private TextMeshProUGUI _runtimeVignetteValue;
    private Image _vignetteImage;
    private Image _sceneFlashImage;
    private const string VignettePrefKey = "PlayerVignetteStrength";
    private const float DefaultVignetteStrength = 0.28f;

    private WaitForSecondsRealtime _waitQuick;
    private WaitForSecondsRealtime _waitNormal;
    private WaitForSecondsRealtime _waitLong;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        _waitQuick = new WaitForSecondsRealtime(0.1f);
        _waitNormal = new WaitForSecondsRealtime(0.5f);
        _waitLong = new WaitForSecondsRealtime(1.5f);

        if (globalFadeCanvasGroup != null)
        {
            globalFadeCanvasGroup.gameObject.SetActive(true);
            globalFadeCanvasGroup.alpha = 1f;
            globalFadeCanvasGroup.blocksRaycasts = true;
        }

        if (dayTransitionPanel != null) dayTransitionPanel.SetActive(false);
    }

    private void Start()
    {
        _cameraLook = FindObjectOfType<FirstPersonCameraLook>();

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        HideSubtitle();

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        ApplyResponsiveCanvasSettings();
        if (TryGetComponent(out Canvas canvas)) FitScreenCanvas(canvas);
        UpdateGameViewport();
        CreateVignetteOverlay();
        InitializeSettingsTabs();
        InitCalibrationUI();
        StyleDialogueSubtitle();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cameraLook = FindObjectOfType<FirstPersonCameraLook>();
        ApplyResponsiveCanvasSettings();
        foreach (GameObject root in scene.GetRootGameObjects())
            if (root.TryGetComponent(out Canvas canvas) && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                FitScreenCanvas(canvas);
        UpdateGameViewport();
        if (_settingsTabsInitialized) SyncSlidersToCurrentValues();
    }

    private void ApplyResponsiveCanvasSettings()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                continue;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referencePixelsPerUnit = 100f;
        }
    }

    private void FitScreenCanvas(Canvas canvas)
    {
        if (canvas.transform.Find("Game_Viewport") != null) return;
        int originalChildren = canvas.transform.childCount;
        RectTransform content = CreateViewportContent(canvas);
        for (int i = 0; i < originalChildren;)
        {
            if (!(canvas.transform.GetChild(i) is RectTransform panel))
            {
                i++;
                continue;
            }
            Vector2 min = panel.anchorMin;
            Vector2 max = panel.anchorMax;
            Vector2 offsetMin = panel.offsetMin;
            Vector2 offsetMax = panel.offsetMax;
            panel.SetParent(content, false);
            SetRect(panel, min, max, offsetMin, offsetMax);
            originalChildren--;
        }
    }

    private Canvas CreateScreenOverlay(string objectName, int order, bool interactive)
    {
        GameObject overlay = new GameObject(objectName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        DontDestroyOnLoad(overlay);
        Canvas canvas = overlay.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        CanvasScaler scaler = overlay.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
        if (interactive) overlay.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private RectTransform CreateViewportContent(Canvas canvas)
    {
        RectTransform content = CreateUIObject("Game_Viewport", canvas.transform).GetComponent<RectTransform>();
        _screenContents.Add(content);
        FitViewportContent(content, GetGameViewport());
        return content;
    }

    private static Rect GetGameViewport()
    {
        float aspect = (float)Mathf.Max(1, Screen.width) / Mathf.Max(1, Screen.height);
        if (aspect > GameAspectRatio)
        {
            float width = GameAspectRatio / aspect;
            return new Rect((1f - width) * 0.5f, 0f, width, 1f);
        }
        float height = aspect / GameAspectRatio;
        return new Rect(0f, (1f - height) * 0.5f, 1f, height);
    }

    private void FitViewportContent(RectTransform content, Rect viewport)
    {
        SetRect(content, viewport.min, viewport.max, Vector2.zero, Vector2.zero);
    }

    private void UpdateGameViewport()
    {
        _viewportWidth = Screen.width;
        _viewportHeight = Screen.height;
        Rect viewport = GetGameViewport();
        _screenContents.RemoveAll(content => content == null);
        foreach (RectTransform content in _screenContents)
            FitViewportContent(content, viewport);

        Camera camera = GameFlowManager.Instance != null ? GameFlowManager.Instance.mainCamera : Camera.main;
        if (camera != null)
        {
            camera.rect = viewport;
            camera.aspect = GameAspectRatio;
        }

        if (_letterboxCanvas == null)
        {
            _letterboxCanvas = CreateScreenOverlay("Game_Letterbox", 300, false);
            _letterboxBars = new RectTransform[2];
            for (int i = 0; i < _letterboxBars.Length; i++)
            {
                Image bar = CreateUIObject($"Bar_{i}", _letterboxCanvas.transform).AddComponent<Image>();
                bar.color = Color.black;
                bar.raycastTarget = false;
                _letterboxBars[i] = bar.rectTransform;
            }
        }
        _letterboxCanvas.enabled = viewport.width < 0.9999f || viewport.height < 0.9999f;
        if (viewport.width < 0.9999f)
        {
            Place(_letterboxBars[0], 0f, 0f, viewport.xMin, 1f);
            Place(_letterboxBars[1], viewport.xMax, 0f, 1f, 1f);
        }
        else
        {
            Place(_letterboxBars[0], 0f, 0f, 1f, viewport.yMin);
            Place(_letterboxBars[1], 0f, viewport.yMax, 1f, 1f);
        }
    }

    private void CreateVignetteOverlay()
    {
        if (_vignetteImage != null) return;

        _vignetteCanvas = CreateScreenOverlay("Runtime_Vignette_Canvas", 1, false);
        RectTransform content = CreateViewportContent(_vignetteCanvas);

        GameObject imageObject = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(content, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        StretchRect(rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Texture2D texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        texture.name = "Runtime_Vignette_Texture";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < 512; y++)
        {
            for (int x = 0; x < 512; x++)
            {
                float nx = (x / 511f - 0.5f) * 2f;
                float ny = (y / 511f - 0.5f) * 2f;
                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.SmoothStep(0.48f, 1.28f, distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        _vignetteImage = imageObject.GetComponent<Image>();
        _vignetteImage.sprite = Sprite.Create(texture, new Rect(0, 0, 512, 512), new Vector2(0.5f, 0.5f));
        _vignetteImage.type = Image.Type.Simple;
        _vignetteImage.preserveAspect = false;
        _vignetteImage.raycastTarget = false;
        SetVignetteStrength(PlayerPrefs.GetFloat(VignettePrefKey, DefaultVignetteStrength));
    }

    internal IEnumerator FlashSceneEdges()
    {
        CreateVignetteOverlay();
        if (_sceneFlashImage == null)
        {
            GameObject overlay = new GameObject("Scene_EdgeFlash", typeof(RectTransform), typeof(Image));
            overlay.transform.SetParent(_vignetteImage.transform.parent, false);
            StretchRect(overlay.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _sceneFlashImage = overlay.GetComponent<Image>();
            _sceneFlashImage.sprite = _vignetteImage.sprite;
            _sceneFlashImage.raycastTarget = false;
        }

        const float duration = 0.4f;
        _sceneFlashImage.enabled = true;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.unscaledDeltaTime)
        {
            _sceneFlashImage.color = new Color(1f, 1f, 1f, 0.65f * (1f - elapsed / duration));
            yield return null;
        }
        _sceneFlashImage.enabled = false;
    }

    private void SetVignetteStrength(float strength)
    {
        strength = Mathf.Clamp(strength, 0f, 0.65f);
        if (_vignetteImage != null)
        {
            Color color = Color.black;
            color.a = strength;
            _vignetteImage.color = color;
        }
        PlayerPrefs.SetFloat(VignettePrefKey, strength);
        PlayerPrefs.Save();
        UpdateSliderTexts();
    }

    private void InitializeSettingsTabs()
    {
        if (_settingsTabsInitialized || systemMenuPanel == null)
            return;

        _settingsTabsInitialized = true;

        // 기존 프리팹의 개별 설정 UI는 비활성화합니다. 새 UI와 중복되지 않습니다.
        foreach (Transform child in systemMenuPanel.transform)
            child.gameObject.SetActive(false);

        _runtimeSettingsRoot = CreateUIObject("Runtime_Settings_Root", systemMenuPanel.transform);
        StretchRect(_runtimeSettingsRoot.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        GameObject backdrop = CreateUIObject("Menu_Backdrop", systemMenuPanel.transform);
        backdrop.transform.SetSiblingIndex(_runtimeSettingsRoot.transform.GetSiblingIndex());
        Place(backdrop.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
        _menuBackdrop = backdrop.AddComponent<RawImage>();
        _menuBackdrop.color = Color.black;
        Image rootImage = _runtimeSettingsRoot.AddComponent<Image>();
        rootImage.color = new Color(0f, 0f, 0f, 0.28f);
        if (systemMenuPanel.TryGetComponent(out Image oldBackground)) oldBackground.enabled = false;

        Transform settings = _runtimeSettingsRoot.transform;
        TextMeshProUGUI title = CreateText("설정", settings, 42f, TextAlignmentOptions.Left);
        title.fontStyle = FontStyles.Bold;
        Place(title.rectTransform, 0.08f, 0.83f, 0.3f, 0.91f);
        Button close = CreateRuntimeButton("닫기", settings);
        Place((RectTransform)close.transform, 0.84f, 0.84f, 0.925f, 0.9f);
        close.onClick.AddListener(CloseSettings);

        string[] tabNames = { "화면", "음향", "조작", "시선" };
        for (int i = 0; i < tabNames.Length; i++)
        {
            SettingsTab tab = (SettingsTab)i;
            Button button = CreateRuntimeButton(tabNames[i], settings);
            StyleSidebarAction(button);
            float top = 0.78f - i * 0.067f;
            Place((RectTransform)button.transform, 0.075f, top - 0.054f, 0.235f, top);
            button.onClick.AddListener(() => SetSettingsTab(tab));
            _settingsTabs[i] = button;
        }
        Image separator = CreateUIObject("Settings_Divider", settings).AddComponent<Image>();
        separator.color = new Color(1f, 1f, 1f, 0.14f);
        separator.raycastTarget = false;
        Place(separator.rectTransform, 0.25f, 0.14f, 0.251f, 0.79f);

        // 스크롤 영역
        GameObject scrollObject = CreateUIObject("Settings_ScrollView", _runtimeSettingsRoot.transform);
        Place(scrollObject.GetComponent<RectTransform>(), 0.30f, 0.14f, 0.89f, 0.80f);
        ScrollRect scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        GameObject viewport = CreateUIObject("Viewport", scrollObject.transform);
        StretchRect(viewport.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.015f);
        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0f, 0f);
        VerticalLayoutGroup contentLayout = content.AddComponent<VerticalLayoutGroup>();
        contentLayout.padding = new RectOffset(18, 18, 12, 18);
        contentLayout.spacing = 12f;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        _settingsScroll = scroll;

        for (int i = 0; i < tabNames.Length; i++)
        {
            GameObject page = CreateUIObject($"Settings_{(SettingsTab)i}", content.transform);
            ConfigureVerticalGroup(page, 6f);
            CreateSectionHeader(tabNames[i], page.transform);
            _settingsPages[i] = page;
        }
        _controlsSettingsGroup = _settingsPages[(int)SettingsTab.Controls];
        _gameSettingsGroup = _settingsPages[(int)SettingsTab.Display];
        Transform gaze = _settingsPages[(int)SettingsTab.Gaze].transform;
        Transform audio = _settingsPages[(int)SettingsTab.Audio].transform;

        GameObject cameraRow = CreateSettingRow("카메라", gaze);
        _cameraCycleButton = CreateRuntimeButton("카메라 검색 중...", cameraRow.transform);
        LayoutElement cameraButtonLayout = _cameraCycleButton.GetComponent<LayoutElement>();
        cameraButtonLayout.minWidth = 320f;
        cameraButtonLayout.preferredWidth = 420f;
        _cameraCycleLabel = _cameraCycleButton.GetComponentInChildren<TextMeshProUGUI>(true);
        _cameraCycleButton.onClick.AddListener(OnClickRecalibrate);

        horizontalSensitivitySlider = CreateSliderRow("좌우 시선 감도", 0.1f, 8f, 2f, gaze, out _runtimeHorizontalValue);
        verticalSensitivitySlider = CreateSliderRow("상하 시선 감도", 0.1f, 8f, 2f, gaze, out _runtimeVerticalValue);
        deadzoneSlider = CreateSliderRow("미세 떨림 무시", 0.01f, 0.15f, 0.03f, gaze, out _runtimeDeadzoneValue);
        leanThresholdSlider = CreateSliderRow("몸 기울임 민감도", 0.05f, 3f, 0.15f, _controlsSettingsGroup.transform, out _runtimeLeanValue);
        reachThresholdSlider = CreateSliderRow("손 뻗기 민감도", 0.1f, 0.6f, 0.2f, _controlsSettingsGroup.transform, out _runtimeReachValue);

        GameObject toggleRow = CreateSettingRow("좌우 반전", gaze);
        _runtimeInvertYawToggle = CreateRuntimeToggle(toggleRow.transform, "좌우 반전 사용");
        _runtimeInvertYawLabel = _runtimeInvertYawToggle.GetComponentInChildren<TextMeshProUGUI>();
        _runtimeInvertYawToggle.isOn = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
        _runtimeInvertYawToggle.onValueChanged.AddListener(v =>
        {
            if (_cameraLook != null) _cameraLook.SetInvertYaw(v);
            PlayerPrefs.SetInt("InvertYaw", v ? 1 : 0);
            PlayerPrefs.Save();
        });

        _runtimeInvertPitchToggle = CreateRuntimeToggle(CreateSettingRow("상하 반전", gaze).transform, "사용");
        _runtimeInvertPitchToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FirstPersonCameraLook.PitchPreferenceKey, 0) == 1);
        _runtimeInvertPitchToggle.onValueChanged.AddListener(value =>
        {
            if (_cameraLook != null) _cameraLook.SetInvertPitch(value);
        });

        Button recalibrateButton = CreateRuntimeButton("시선 재조정", gaze);
        recalibrateButton.onClick.AddListener(OnClickRecalibrate);

        ConfigureDisplaySettings(_gameSettingsGroup.transform);
        _vignetteSlider = CreateSliderRow("시야 가장자리 어둡기", 0f, 0.65f, PlayerPrefs.GetFloat(VignettePrefKey, DefaultVignetteStrength), _gameSettingsGroup.transform, out _runtimeVignetteValue);
        _vignetteSlider.onValueChanged.AddListener(SetVignetteStrength);
        brightnessSlider = CreateSliderRow("화면 시야 밝기", 0.2f, 1f, 1f, _gameSettingsGroup.transform, out _runtimeScreenBrightnessValue);
        interiorBrightnessSlider = CreateSliderRow("실내 조명 밝기", 1f, 2.5f,
            BrightnessManager.Instance != null ? BrightnessManager.Instance.defaultInteriorBrightness : 1.35f,
            _gameSettingsGroup.transform, out _runtimeInteriorBrightnessValue);

        AudioManager manager = AudioManager.Instance;
        if (manager != null)
        {
            AddVolumeSetting(audio, "전체 음량", manager.MasterVolume, manager.SetMasterVolume);
            AddVolumeSetting(audio, "효과음", manager.SfxVolume, manager.SetSfxVolume);
            AddVolumeSetting(audio, "배경음", manager.BgmVolume, manager.SetBgmVolume);
            AddVolumeSetting(audio, "음성", manager.VoiceVolume, manager.SetVoiceVolume);
            Toggle mute = CreateRuntimeToggle(CreateSettingRow("전체 음소거", audio).transform, "사용");
            bool isMuted = PlayerPrefs.GetInt("404_Audio_Muted", 0) == 1;
            mute.SetIsOnWithoutNotify(isMuted);
            AudioListener.volume = isMuted ? 0f : 1f;
            mute.onValueChanged.AddListener(value =>
            {
                AudioListener.volume = value ? 0f : 1f;
                PlayerPrefs.SetInt("404_Audio_Muted", value ? 1 : 0);
                PlayerPrefs.Save();
            });
        }

        BuildPauseMenu();

        PopulateRuntimeCameraList();
        SetSettingsTab(SettingsTab.Display);
    }

    private void SetSettingsTab(SettingsTab tab)
    {
        for (int i = 0; i < _settingsPages.Length; i++)
        {
            bool selected = i == (int)tab;
            _settingsPages[i].SetActive(selected);
            SetRuntimeButtonSelected(_settingsTabs[i], selected);
        }
        Canvas.ForceUpdateCanvases();
        _settingsScroll.verticalNormalizedPosition = 1f;
        if (tab == SettingsTab.Display) RefreshDisplayLabels();
    }

    private void ConfigureDisplaySettings(Transform display)
    {
        GameObject mode = CreateSettingRow("화면 모드", display);
        _displayModeButton = CreateRuntimeButton("화면 모드", mode.transform);
        _displayModeButton.GetComponent<LayoutElement>().flexibleWidth = 1f;
        _displayModeButton.onClick.AddListener(() =>
        {
            Screen.fullScreen = !Screen.fullScreen;
            StartCoroutine(RefreshDisplayAfterFrame());
        });
        _resolutionLabel = CreateText("", CreateSettingRow("해상도", display).transform, 20f, TextAlignmentOptions.Right);
        _resolutionLabel.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;
    }

    private IEnumerator RefreshDisplayAfterFrame()
    {
        yield return null;
        RefreshDisplayLabels();
    }

    private void RefreshDisplayLabels()
    {
        SetButtonLabel(_displayModeButton, Screen.fullScreen ? "전체 화면" : "창 모드");
        _resolutionLabel.text = $"{Screen.width} × {Screen.height}";
    }

    private void AddVolumeSetting(Transform parent, string title, float initialValue, Action<float> apply)
    {
        Slider slider = CreateSliderRow(title, 0f, 1f, initialValue, parent, out TextMeshProUGUI valueText);
        slider.onValueChanged.AddListener(value =>
        {
            apply(value);
            valueText.text = value.ToString("0.00");
        });
    }

    private void BuildPauseMenu()
    {
        _pauseMenu = CreateUIObject("Pause_Menu", systemMenuPanel.transform);
        Place(_pauseMenu.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
        Image shade = _pauseMenu.AddComponent<Image>();
        shade.color = new Color(0f, 0f, 0f, 0.22f);
        TextMeshProUGUI heading = CreateText("일시 정지", _pauseMenu.transform, 48f, TextAlignmentOptions.Left);
        heading.fontStyle = FontStyles.Bold;
        Place(heading.rectTransform, 0.1f, 0.68f, 0.4f, 0.79f);
        AddPauseAction("계속하기", 0.66f, ResumeGame);
        AddPauseAction("설정", 0.58f, OpenSettingsPage);
        AddPauseAction("메인 메뉴", 0.50f, () => ShowMenuDialog(_pauseMenu.transform,
            "메인 메뉴로 돌아갈까요?", "현재 장면을 종료합니다.", "돌아가기", OnClickQuit));
        _pauseMenu.SetActive(false);
    }

    private void AddPauseAction(string label, float top, UnityEngine.Events.UnityAction action)
    {
        Button button = CreateRuntimeButton(label, _pauseMenu.transform);
        StyleSidebarAction(button);
        Place((RectTransform)button.transform, 0.095f, top - 0.061f, 0.275f, top);
        button.onClick.AddListener(action);
    }

    public void OpenSettings()
    {
        if (!IsPaused) PauseGame();
        if (_runtimeSettingsRoot != null) OpenSettingsPage();
    }

    private void OpenSettingsPage()
    {
        _pauseMenu.SetActive(false);
        _runtimeSettingsRoot.SetActive(true);
        SetSettingsTab(SettingsTab.Display);
    }

    private void CloseSettings()
    {
        if (_runtimeSettingsRoot != null) _runtimeSettingsRoot.SetActive(false);
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        ResumeGame();
        SetCursorForCurrentStage();
    }

    internal void ShowMenuDialog(Transform parent, string title, string detail, string confirm,
        UnityEngine.Events.UnityAction action, string cancel = "취소", UnityEngine.Events.UnityAction onCancel = null)
    {
        CloseMenuDialog();
        _menuDialog = CreateUIObject("Menu_Confirmation", parent);
        Place(_menuDialog.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
        _menuDialog.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);
        TextMeshProUGUI heading = CreateText(title, _menuDialog.transform, 40f, TextAlignmentOptions.Center);
        heading.fontStyle = FontStyles.Bold;
        Place(heading.rectTransform, 0.18f, 0.54f, 0.82f, 0.63f);
        TextMeshProUGUI description = CreateText(detail, _menuDialog.transform, 22f, TextAlignmentOptions.Center);
        description.color = new Color(0.78f, 0.78f, 0.78f);
        Place(description.rectTransform, 0.18f, 0.47f, 0.82f, 0.55f);
        Button accept = CreateRuntimeButton(confirm, _menuDialog.transform);
        Button back = CreateRuntimeButton(cancel, _menuDialog.transform);
        Place((RectTransform)accept.transform, 0.33f, 0.38f, 0.49f, 0.443f);
        Place((RectTransform)back.transform, 0.51f, 0.38f, 0.67f, 0.443f);
        SetRuntimeButtonSelected(accept, true);
        accept.onClick.AddListener(() => { CloseMenuDialog(); action?.Invoke(); });
        back.onClick.AddListener(() => { CloseMenuDialog(); onCancel?.Invoke(); });
    }

    private void CloseMenuDialog()
    {
        if (_menuDialog == null) return;
        _menuDialog.SetActive(false);
        Destroy(_menuDialog);
        _menuDialog = null;
    }

    private void StyleDialogueSubtitle()
    {
        if (subtitlePanel == null || subtitleText == null) return;
        Transform previousContainer = subtitleText.transform.parent;
        if (previousContainer != null && previousContainer != subtitlePanel.transform)
        {
            subtitleText.rectTransform.SetParent(subtitlePanel.transform, false);
            if (previousContainer.TryGetComponent(out Graphic oldBackground)) oldBackground.enabled = false;
        }
        Image shade = CreateUIObject("Subtitle_Shade", subtitlePanel.transform).AddComponent<Image>();
        shade.transform.SetAsFirstSibling();
        shade.color = new Color(0f, 0f, 0f, 0.60f);
        shade.raycastTarget = false;
        Place(shade.rectTransform, 0f, 0f, 1f, 0.19f);
        Place(subtitleText.rectTransform, 0.06f, 0.05f, 0.68f, 0.16f);
        subtitleText.alignment = TextAlignmentOptions.MidlineLeft;
        subtitleText.enableAutoSizing = true;
        subtitleText.fontSizeMin = 24f;
        subtitleText.fontSizeMax = 38f;
        subtitleText.raycastTarget = false;
    }

    private void Place(RectTransform rect, float left, float bottom, float right, float top)
    {
        SetRect(rect, new Vector2(left, bottom), new Vector2(right, top), Vector2.zero, Vector2.zero);
    }

    private GameObject CreateUIObject(string objectName, Transform parent)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private void StretchRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        StretchRect(rect, anchorMin, anchorMax, offsetMin, offsetMax);
    }

    private TextMeshProUGUI CreateText(string value, Transform parent, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject go = CreateUIObject("Text_" + value, parent);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = alignment;
        text.raycastTarget = false;
        if (subtitleText != null && subtitleText.font != null) text.font = subtitleText.font;
        else if (TMP_Settings.defaultFontAsset != null) text.font = TMP_Settings.defaultFontAsset;
        return text;
    }

    private Button CreateRuntimeButton(string label, Transform parent)
    {
        GameObject go = CreateUIObject("Button_" + label, parent);
        Image image = go.AddComponent<Image>();
        image.color = new Color(0.075f, 0.075f, 0.075f, 0.72f);
        Outline border = go.AddComponent<Outline>();
        border.effectColor = new Color(0.54f, 0.54f, 0.54f, 0.5f);
        border.effectDistance = new Vector2(1f, -1f);
        Button button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        TextMeshProUGUI text = CreateText(label, go.transform, 21f, TextAlignmentOptions.Center);
        StretchRect(text.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.minHeight = 48f;
        layout.preferredHeight = 52f;
        return button;
    }

    private void SetRuntimeButtonSelected(Button button, bool selected)
    {
        if (button == null) return;
        Transform marker = button.transform.Find("Selection_Marker");
        if (marker != null)
        {
            marker.gameObject.SetActive(selected);
            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                label.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
                label.color = selected ? Color.white : new Color(0.72f, 0.72f, 0.72f);
            }
            return;
        }
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = selected
            ? new Color(0.27f, 0.27f, 0.27f, 0.82f)
            : new Color(0.075f, 0.075f, 0.075f, 0.72f);
    }

    private void StyleSidebarAction(Button button)
    {
        button.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.04f);
        button.GetComponent<Outline>().enabled = false;
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.alignment = TextAlignmentOptions.MidlineLeft;
            Place(label.rectTransform, 0.13f, 0f, 1f, 1f);
        }
        Image marker = CreateUIObject("Selection_Marker", button.transform).AddComponent<Image>();
        marker.raycastTarget = false;
        Place(marker.rectTransform, 0.02f, 0.30f, 0.028f, 0.70f);
        marker.gameObject.SetActive(false);
    }

    private void ConfigureVerticalGroup(GameObject go, float spacing)
    {
        VerticalLayoutGroup layout = go.AddComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        LayoutElement element = go.AddComponent<LayoutElement>();
        element.minHeight = 0f;
        element.flexibleHeight = 0f;
    }

    private void CreateSectionHeader(string label, Transform parent)
    {
        TextMeshProUGUI header = CreateText(label, parent, 25f, TextAlignmentOptions.Left);
        header.color = Color.white;
        header.fontStyle = FontStyles.Bold;
        LayoutElement layout = header.gameObject.AddComponent<LayoutElement>();
        layout.preferredHeight = 42f;
        layout.minHeight = 42f;
    }

    private GameObject CreateSettingRow(string label, Transform parent)
    {
        GameObject row = CreateUIObject("SettingRow_" + label, parent);
        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.padding = new RectOffset(8, 8, 4, 4);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        LayoutElement rowElement = row.AddComponent<LayoutElement>();
        rowElement.minHeight = 46f;
        rowElement.preferredHeight = 51f;
        TextMeshProUGUI title = CreateText(label, row.transform, 20f, TextAlignmentOptions.Left);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.minWidth = 140f;
        titleLayout.preferredWidth = 210f;
        titleLayout.flexibleWidth = 0f;
        return row;
    }

    private Slider CreateSliderRow(string label, float min, float max, float defaultValue, Transform parent, out TextMeshProUGUI valueText)
    {
        GameObject row = CreateSettingRow(label, parent);
        GameObject sliderObject = CreateUIObject("Slider_" + label, row.transform);
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.1f, 0.1f, 0.35f);
        LayoutElement sliderElement = sliderObject.AddComponent<LayoutElement>();
        sliderElement.minHeight = 34f;
        sliderElement.preferredHeight = 38f;
        sliderElement.minWidth = 180f;
        sliderElement.flexibleWidth = 1f;
        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;

        GameObject fillArea = CreateUIObject("Fill Area", sliderObject.transform);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        StretchRect(fillAreaRect, new Vector2(0f, 0.46f), new Vector2(1f, 0.54f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchRect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.8f, 0.8f, 0.8f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform);
        StretchRect(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(12f, 12f);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;

        valueText = CreateText(defaultValue.ToString("0.00"), row.transform, 18f, TextAlignmentOptions.Right);
        LayoutElement valueLayout = valueText.gameObject.AddComponent<LayoutElement>();
        valueLayout.minWidth = 82f;
        valueLayout.preferredWidth = 92f;
        valueLayout.flexibleWidth = 0f;
        slider.value = Mathf.Clamp(defaultValue, min, max);
        return slider;
    }

    private Toggle CreateRuntimeToggle(Transform parent, string label)
    {
        GameObject toggleObject = CreateUIObject("Toggle_" + label, parent);
        HorizontalLayoutGroup layout = toggleObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        LayoutElement element = toggleObject.AddComponent<LayoutElement>();
        element.preferredHeight = 48f;
        element.preferredWidth = 320f;

        GameObject box = CreateUIObject("Checkbox", toggleObject.transform);
        RectTransform boxRect = box.GetComponent<RectTransform>();
        boxRect.sizeDelta = new Vector2(28f, 28f);
        Image boxImage = box.AddComponent<Image>();
        boxImage.color = new Color(0.16f, 0.2f, 0.24f, 1f);
        GameObject check = CreateUIObject("Checkmark", box.transform);
        StretchRect(check.GetComponent<RectTransform>(), new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), Vector2.zero, Vector2.zero);
        Image checkImage = check.AddComponent<Image>();
        checkImage.color = new Color(0.25f, 0.85f, 0.8f, 1f);

        TextMeshProUGUI text = CreateText(label, toggleObject.transform, 19f, TextAlignmentOptions.Left);
        LayoutElement textLayout = text.gameObject.AddComponent<LayoutElement>();
        textLayout.preferredWidth = 220f;
        Toggle toggle = toggleObject.AddComponent<Toggle>();
        toggle.targetGraphic = boxImage;
        toggle.graphic = checkImage;
        toggle.isOn = false;
        return toggle;
    }

    private void PopulateRuntimeCameraList()
    {
        _availableCameraNames.Clear();
        WebCamDevice[] devices = WebCamTexture.devices;
        foreach (WebCamDevice device in devices) _availableCameraNames.Add(device.name);

        string selected = VisionTrackingManager.Instance != null ? VisionTrackingManager.Instance.SelectedDeviceName : string.Empty;
        _selectedCameraIndex = _availableCameraNames.IndexOf(selected);
        if (_selectedCameraIndex < 0 && _availableCameraNames.Count > 0)
        {
            _selectedCameraIndex = 0;
        }
        UpdateCameraButtonLabel();
    }

    private void UpdateCameraButtonLabel()
    {
        if (_cameraCycleLabel == null) return;
        string label = _availableCameraNames.Count == 0
            ? "카메라 검색"
            : "카메라: " + _availableCameraNames[Mathf.Clamp(_selectedCameraIndex, 0, _availableCameraNames.Count - 1)] + "  (눌러서 변경)";
        _cameraCycleLabel.text = label;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            if (_letterboxCanvas != null) Destroy(_letterboxCanvas.gameObject);
            if (_vignetteCanvas != null) Destroy(_vignetteCanvas.gameObject);
            if (_cameraSetupRoot != null) Destroy(_cameraSetupRoot);
            if (_calibrationDotTexture != null) Destroy(_calibrationDotTexture);
            Instance = null;
        }
    }

    private void Update()
    {
        if (Screen.width != _viewportWidth || Screen.height != _viewportHeight)
        {
            UpdateGameViewport();
            if (_resolutionLabel != null) RefreshDisplayLabels();
        }

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isCameraSetupOpen)
            {
                _cameraSetupAction = -1;
                return;
            }
            if (_menuDialog != null)
            {
                CloseMenuDialog();
                return;
            }
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
            {
                if (systemMenuPanel != null && systemMenuPanel.activeInHierarchy)
                {
                    CloseSettings();
                }
                return;
            }
            if (IsPaused && _runtimeSettingsRoot != null && _runtimeSettingsRoot.activeInHierarchy)
                CloseSettings();
            else
                TogglePause();
        }

    }

    private void LateUpdate()
    {
        if (!IsPaused && !_isCameraSetupOpen && _menuDialog == null)
        {
            if (GameFlowManager.Instance != null &&
                (Cursor.lockState != CursorLockMode.Locked || Cursor.visible || IsPointerStage()))
                SetCursorForCurrentStage();
        }
    }

    public bool IsAnyUIBlocking()
    {
        bool isMainMenuActive = TitleController.Instance != null &&
                                TitleController.Instance.titlePanel != null &&
                                TitleController.Instance.titlePanel.activeInHierarchy;
        bool isIntroVideoActive = TitleController.Instance != null &&
                                  TitleController.Instance.introVideoPanel != null &&
                                  TitleController.Instance.introVideoPanel.activeInHierarchy;
        return IsPaused || IsDialogueActive || _isCameraSetupOpen || isMainMenuActive || isIntroVideoActive;
    }

    public void TogglePause()
    {
        if (IsPaused) ResumeGame();
        else PauseGame();
    }

    private void PauseGame()
    {
        if (IsPaused) return;
        IsPaused = true;
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (_cameraLook == null) _cameraLook = FindObjectOfType<FirstPersonCameraLook>();
        SyncSlidersToCurrentValues();

        PopulateRuntimeCameraList();

        if (systemMenuPanel != null)
        {
            systemMenuPanel.transform.SetAsLastSibling();
            systemMenuPanel.SetActive(true);
        }
        InitializeSettingsTabs();
        if (_menuBackdrop != null)
        {
            _menuBackdrop.texture = TitleController.Instance != null ? TitleController.Instance.MenuBackdrop : null;
            _menuBackdrop.color = _menuBackdrop.texture != null ? new Color(0.56f, 0.56f, 0.56f) : Color.black;
        }
        if (_runtimeSettingsRoot != null) _runtimeSettingsRoot.SetActive(false);
        if (_pauseMenu != null) _pauseMenu.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _pausedVideos.Clear();
        VideoPlayer[] allVideos = FindObjectsOfType<VideoPlayer>();
        foreach (VideoPlayer vp in allVideos)
        {
            if (vp.isPlaying && vp.gameObject.name != "IntroVideo_Player")
            {
                vp.Pause();
                _pausedVideos.Add(vp);
            }
        }
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        SaveCalibrationSettings();
        CloseMenuDialog();

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        foreach (VideoPlayer vp in _pausedVideos)
        {
            if (vp != null) vp.Play();
        }
        _pausedVideos.Clear();

        SetCursorForCurrentStage(force: true);
        if (!IsPointerStage()) StartCoroutine(RelockCursorAfterEscape());

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private IEnumerator RelockCursorAfterEscape()
    {
        // Escape can release the native cursor after the pause-menu callback has run.
        while (Keyboard.current != null && Keyboard.current.escapeKey.isPressed && !IsPaused)
            yield return null;
        yield return new WaitForEndOfFrame();
        if (!IsPaused && !_isCameraSetupOpen && _menuDialog == null && !IsPointerStage())
            SetCursorForCurrentStage(force: true);
    }

    private void OnApplicationFocus(bool focused)
    {
        if (focused && !IsPaused && !_isCameraSetupOpen && _menuDialog == null)
            SetCursorForCurrentStage(force: true);
    }

    private void SetCursorForCurrentStage(bool force = false)
    {
        bool showPointer = IsPointerStage();
        CursorLockMode lockMode = showPointer ? CursorLockMode.None : CursorLockMode.Locked;
        if (force || Cursor.lockState != lockMode) Cursor.lockState = lockMode;
        if (force || Cursor.visible != showPointer) Cursor.visible = showPointer;
    }

    private static bool IsPointerStage()
    {
        if (GameFlowManager.Instance == null) return false;
        GameStage stage = GameFlowManager.Instance.currentStage;
        return stage == GameStage.Title ||
            (stage == GameStage.Stage13_Ending && Instance != null && Instance.IsEndingPointerEnabled);
    }

    public void OnClickQuit()
    {
        SaveCalibrationSettings();
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        else ResetTransientUI();
    }

    public void OnClickRecalibrate()
    {
        if (!_isCameraSetupOpen) StartCoroutine(RunCameraSetup());
    }

    internal IEnumerator RunCameraSetup(bool requireDecision = false)
    {
        if (_isCameraSetupOpen) yield break;
        _isCameraSetupOpen = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        VisionTrackingManager tracking = VisionTrackingManager.Instance;
        Canvas canvas = CreateScreenOverlay("Camera_Setup_Canvas", 100, true);
        GameObject canvasObject = canvas.gameObject;
        _cameraSetupRoot = canvasObject;
        Transform contents = CreateViewportContent(canvas);
        Image background = CreateUIObject("Camera_Setup_Background", contents).AddComponent<Image>();
        Place(background.rectTransform, 0f, 0f, 1f, 1f);
        background.color = new Color(0.043f, 0.043f, 0.043f);
        TextMeshProUGUI title = CreateText("", contents, 36f, TextAlignmentOptions.Center);
        title.fontStyle = FontStyles.Bold;
        TextMeshProUGUI detail = CreateText("", contents, 22f, TextAlignmentOptions.Center);
        title.enableAutoSizing = true;
        title.fontSizeMin = 23f;
        title.fontSizeMax = 36f;
        detail.enableAutoSizing = true;
        detail.fontSizeMin = 16f;
        detail.fontSizeMax = 22f;
        Place(title.rectTransform, 0.14f, 0.84f, 0.86f, 0.92f);
        Place(detail.rectTransform, 0.14f, 0.78f, 0.86f, 0.85f);
        GameObject previewFrame = CreateUIObject("Camera_Preview_Frame", contents);
        Place((RectTransform)previewFrame.transform, 0.25f, 0.27f, 0.75f, 0.76f);
        RawImage preview = CreateUIObject("Camera_Live_Preview", previewFrame.transform).AddComponent<RawImage>();
        preview.raycastTarget = false;
        AspectRatioFitter aspect = preview.gameObject.AddComponent<AspectRatioFitter>();
        aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        aspect.aspectRatio = 16f / 9f;
        RawImage mark = CreateUIObject("Calibration_Center_Dot", contents).AddComponent<RawImage>();
        Place(mark.rectTransform, 0.5f, 0.5f, 0.5f, 0.5f);
        mark.rectTransform.sizeDelta = new Vector2(12f, 12f);
        mark.color = Color.white;
        mark.raycastTarget = false;
        if (_calibrationDotTexture == null)
        {
            const int diameter = 16;
            _calibrationDotTexture = new Texture2D(diameter, diameter, TextureFormat.RGBA32, false);
            var pixels = new Color32[diameter * diameter];
            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), new Vector2(7.5f, 7.5f));
                    pixels[y * diameter + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Clamp01(8f - distance)));
                }
            _calibrationDotTexture.SetPixels32(pixels);
            _calibrationDotTexture.Apply(false, true);
        }
        mark.texture = _calibrationDotTexture;
        Button primary = CreateRuntimeButton("", contents);
        Button secondary = CreateRuntimeButton("", contents);
        Button deviceSelector = CreateRuntimeButton("", contents);
        Place((RectTransform)deviceSelector.transform, 0.26f, 0.44f, 0.74f, 0.52f);
        deviceSelector.onClick.AddListener(() =>
        {
            if (_availableCameraNames.Count < 2) return;
            _selectedCameraIndex = (_selectedCameraIndex + 1) % _availableCameraNames.Count;
            SetButtonLabel(deviceSelector, $"{_availableCameraNames[_selectedCameraIndex]}  ({_selectedCameraIndex + 1}/{_availableCameraNames.Count})");
        });
        Place((RectTransform)primary.transform, 0.335f, 0.10f, 0.485f, 0.163f);
        Place((RectTransform)secondary.transform, 0.515f, 0.10f, 0.665f, 0.163f);
        primary.onClick.AddListener(() => _cameraSetupAction = 1);
        secondary.onClick.AddListener(() => _cameraSetupAction = 2);

        void ShowStep(string heading, string description, string first, string second, bool showPreview = false, bool showMark = false)
        {
            _cameraSetupAction = 0;
            title.text = heading;
            detail.text = description;
            deviceSelector.gameObject.SetActive(false);
            previewFrame.SetActive(showPreview);
            mark.gameObject.SetActive(showMark);
            primary.gameObject.SetActive(first != null);
            secondary.gameObject.SetActive(second != null);
            if (first != null) SetButtonLabel(primary, first);
            if (second != null) SetButtonLabel(secondary, second);
        }

        void UpdatePreview()
        {
            Texture texture = tracking != null ? tracking.CameraPreview : null;
            if (preview.texture != texture) preview.texture = texture;
            if (texture != null && texture.width > 16 && texture.height > 16)
                aspect.aspectRatio = (float)texture.width / texture.height;
        }

        bool complete = false;
        bool useFallback = tracking == null;
        bool mustChooseInput = requireDecision;
        try
        {
            while (tracking != null && !complete)
            {
                PopulateRuntimeCameraList();
                if (_availableCameraNames.Count > 0)
                {
                    ShowStep("카메라 선택", "장치 이름을 눌러 선택하고 카메라 연결을 시작하세요.", "카메라 연결", "다시 검색");
                    deviceSelector.gameObject.SetActive(true);
                    deviceSelector.interactable = _availableCameraNames.Count > 1;
                    SetButtonLabel(deviceSelector, $"{_availableCameraNames[_selectedCameraIndex]}  ({_selectedCameraIndex + 1}/{_availableCameraNames.Count})");
                    while (_cameraSetupAction == 0) yield return null;
                    if (_cameraSetupAction < 0 && !mustChooseInput) break;
                    if (_cameraSetupAction != 1) continue;

                    string selectedDevice = _availableCameraNames[_selectedCameraIndex];
                    mustChooseInput = true;
                    ShowStep("카메라 연결 중…", selectedDevice, null, "다른 카메라");
                    tracking.ReceiveCameraSelection(selectedDevice);

                    float deadline = Time.realtimeSinceStartup + 12f;
                    while (_cameraSetupAction == 0 && tracking.IsCameraOpening && Time.realtimeSinceStartup < deadline)
                    {
                        yield return null;
                    }
                    if (_cameraSetupAction < 0 && !mustChooseInput) break;
                    if (_cameraSetupAction != 0) continue;
                    UpdatePreview();

                    if (tracking.IsCameraReady && preview.texture != null)
                    {
                        ShowStep("카메라 화면 확인", "화면 가운데에 얼굴과 양손이 보이는지 확인해 주세요.", "영점 조절 시작", "다시 확인", showPreview: true);
                        while (_cameraSetupAction == 0 && tracking.IsCameraReady)
                        {
                            UpdatePreview();
                            yield return null;
                        }
                        if (_cameraSetupAction < 0 && !mustChooseInput) break;
                        if (_cameraSetupAction != 1 && _cameraSetupAction != 0) continue;

                        if (_cameraSetupAction == 1 && tracking.IsCameraReady)
                        {
                            ShowStep("시선 영점 조절", "화면 중앙의 점을 바라보고 자세를 유지해 주세요.", null, "취소", showMark: true);
                            tracking.StartCalibration();
                            deadline = Time.realtimeSinceStartup + Mathf.Max(12f, tracking.calibrationDuration + 8f);
                            int shownProgress = -1;
                            while (_cameraSetupAction == 0 && tracking.IsCameraReady &&
                                !tracking.IsCalibrated && Time.realtimeSinceStartup < deadline)
                            {
                                int progress = Mathf.FloorToInt(tracking.CalibrationProgress * 100f);
                                if (progress != shownProgress)
                                {
                                    detail.text = $"화면 중앙의 점을 바라보고 자세를 유지해 주세요.  {progress}%";
                                    shownProgress = progress;
                                }
                                yield return null;
                            }
                            if (_cameraSetupAction != 0 || !tracking.IsCalibrated) tracking.CancelCalibration();
                            if (_cameraSetupAction < 0 && !mustChooseInput) break;
                            if (_cameraSetupAction != 0) continue;
                            if (tracking.IsCalibrated && tracking.IsCameraReady)
                            {
                                ShowStep("시선 영점 조절 완료", "편한 자세를 기준으로 시선 방향을 저장했습니다.", "게임으로 돌아가기", "다시 조절");
                                while (_cameraSetupAction == 0 && tracking.IsCameraReady) yield return null;
                                if (_cameraSetupAction != 1 || !tracking.IsCameraReady) continue;
                                complete = true;
                                continue;
                            }
                        }
                    }
                }

                string failureMessage = tracking.IsCameraReady
                    ? "카메라 영상은 들어오지만 얼굴을 인식하지 못했습니다. 얼굴이 화면에 보이는지 확인해 주세요."
                    : "장치에서 영상이 들어오지 않습니다. 카메라 연결이나 OBS 가상 카메라의 시작 상태를 확인해 주세요.";
                ShowStep("카메라 입력을 확인할 수 없습니다", failureMessage, "카메라 다시 선택", "키보드·마우스");
                while (_cameraSetupAction == 0) yield return null;
                if (_cameraSetupAction == 1 || (_cameraSetupAction < 0 && mustChooseInput)) continue;
                useFallback = _cameraSetupAction == 2;
                break;
            }
        }
        finally
        {
            if (_cameraSetupRoot == canvasObject)
            {
                if (PlayerController.Instance != null && (complete || useFallback))
                    PlayerController.Instance.CompleteInputSetup(useFallback);
                CloseCameraSetup();
            }
        }
    }

    internal void CloseCameraSetup()
    {
        if (!_isCameraSetupOpen) return;
        if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.CancelCalibration();
        _cameraSetupAction = -1;
        if (_cameraSetupRoot != null)
        {
            _cameraSetupRoot.SetActive(false);
            Destroy(_cameraSetupRoot);
        }
        _cameraSetupRoot = null;
        _isCameraSetupOpen = false;
        if (!IsPaused) SetCursorForCurrentStage();
    }

    private void SetButtonLabel(Button button, string label)
    {
        if (button == null) return;
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null) text.text = label;
    }

    private void InitCalibrationUI()
    {
        if (btnResume != null) { btnResume.onClick.RemoveAllListeners(); btnResume.onClick.AddListener(ResumeGame); }
        if (btnQuit != null) { btnQuit.onClick.RemoveAllListeners(); btnQuit.onClick.AddListener(OnClickQuit); }

        ConfigureSlider(horizontalSensitivitySlider, 0.1f, 8f, 2f);
        ConfigureSlider(verticalSensitivitySlider, 0.1f, 8f, 2f);
        ConfigureSlider(brightnessSlider, 0.2f, 1f, 1f);
        ConfigureSlider(_vignetteSlider, 0f, 0.65f, PlayerPrefs.GetFloat(VignettePrefKey, DefaultVignetteStrength));
        SetVignetteStrength(_vignetteSlider != null ? _vignetteSlider.value : DefaultVignetteStrength);
        ConfigureSlider(interiorBrightnessSlider, 1f, 2.5f,
            BrightnessManager.Instance != null ? BrightnessManager.Instance.defaultInteriorBrightness : 1.35f);
        ConfigureSlider(leanThresholdSlider, 0.05f, 3f, 0.15f);
        ConfigureSlider(reachThresholdSlider, 0.1f, 0.6f, 0.2f);
        ConfigureSlider(deadzoneSlider, 0.01f, 0.15f, 0.03f);

        if (horizontalSensitivitySlider != null)
            horizontalSensitivitySlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetHorizontalSensitivity(v); UpdateSliderTexts(); });
        if (verticalSensitivitySlider != null)
            verticalSensitivitySlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetVerticalSensitivity(v); UpdateSliderTexts(); });
        if (brightnessSlider != null)
            brightnessSlider.onValueChanged.AddListener(v => { if (BrightnessManager.Instance != null) BrightnessManager.Instance.SetBrightness(v); UpdateSliderTexts(); });
        if (interiorBrightnessSlider != null)
            interiorBrightnessSlider.onValueChanged.AddListener(v => { if (BrightnessManager.Instance != null) BrightnessManager.Instance.SetInteriorBrightness(v); UpdateSliderTexts(); });
        if (leanThresholdSlider != null)
            leanThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.leanDepthThreshold = v; UpdateSliderTexts(); });
        if (reachThresholdSlider != null)
            reachThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.reachDepthThreshold = v; UpdateSliderTexts(); });
        if (deadzoneSlider != null)
            deadzoneSlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.deadzoneRadius = v; UpdateSliderTexts(); });

        SyncSlidersToCurrentValues();
    }

    private void SyncSlidersToCurrentValues()
    {
        if (VisionTrackingManager.Instance != null)
        {
            if (leanThresholdSlider != null) leanThresholdSlider.SetValueWithoutNotify(VisionTrackingManager.Instance.leanDepthThreshold);
            if (reachThresholdSlider != null) reachThresholdSlider.SetValueWithoutNotify(VisionTrackingManager.Instance.reachDepthThreshold);
        }
        if (_cameraLook != null)
        {
            if (horizontalSensitivitySlider != null) horizontalSensitivitySlider.SetValueWithoutNotify(_cameraLook.horizontalSensitivity);
            if (verticalSensitivitySlider != null) verticalSensitivitySlider.SetValueWithoutNotify(_cameraLook.verticalSensitivity);
            if (deadzoneSlider != null) deadzoneSlider.SetValueWithoutNotify(_cameraLook.deadzoneRadius);
        }
        if (brightnessSlider != null && BrightnessManager.Instance != null)
            brightnessSlider.SetValueWithoutNotify(BrightnessManager.Instance.Brightness);
        if (interiorBrightnessSlider != null && BrightnessManager.Instance != null)
            interiorBrightnessSlider.SetValueWithoutNotify(BrightnessManager.Instance.InteriorBrightness);
        if (_vignetteSlider != null)
            _vignetteSlider.SetValueWithoutNotify(PlayerPrefs.GetFloat(VignettePrefKey, DefaultVignetteStrength));
        if (_runtimeInvertYawToggle != null)
            _runtimeInvertYawToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt("InvertYaw", 0) == 1);
        if (_runtimeInvertPitchToggle != null)
            _runtimeInvertPitchToggle.SetIsOnWithoutNotify(PlayerPrefs.GetInt(FirstPersonCameraLook.PitchPreferenceKey, 0) == 1);
        UpdateSliderTexts();
    }

    private void UpdateSliderTexts()
    {
        if (_runtimeHorizontalValue != null && horizontalSensitivitySlider != null) _runtimeHorizontalValue.text = horizontalSensitivitySlider.value.ToString("0.0");
        if (_runtimeVerticalValue != null && verticalSensitivitySlider != null) _runtimeVerticalValue.text = verticalSensitivitySlider.value.ToString("0.0");
        if (_runtimeLeanValue != null && leanThresholdSlider != null) _runtimeLeanValue.text = leanThresholdSlider.value.ToString("0.00");
        if (_runtimeReachValue != null && reachThresholdSlider != null) _runtimeReachValue.text = reachThresholdSlider.value.ToString("0.00");
        if (_runtimeDeadzoneValue != null && deadzoneSlider != null) _runtimeDeadzoneValue.text = deadzoneSlider.value.ToString("0.000");
        if (_runtimeScreenBrightnessValue != null && brightnessSlider != null) _runtimeScreenBrightnessValue.text = brightnessSlider.value.ToString("P0");
        if (_runtimeInteriorBrightnessValue != null && interiorBrightnessSlider != null) _runtimeInteriorBrightnessValue.text = (interiorBrightnessSlider.value * 100f).ToString("0") + "%";
        if (_runtimeVignetteValue != null && _vignetteSlider != null) _runtimeVignetteValue.text = (_vignetteSlider.value * 100f).ToString("0") + "%";
    }

    private void SaveCalibrationSettings()
    {
        if (VisionTrackingManager.Instance != null)
        {
            PlayerPrefs.SetFloat("LeanThreshold", VisionTrackingManager.Instance.leanDepthThreshold);
            PlayerPrefs.SetFloat("ReachThreshold", VisionTrackingManager.Instance.reachDepthThreshold);
        }
        if (_cameraLook != null)
        {
            PlayerPrefs.SetFloat("RotationSpeed", _cameraLook.headRotationSpeed);
            PlayerPrefs.SetFloat("HorizontalSensitivity", _cameraLook.horizontalSensitivity);
            PlayerPrefs.SetFloat("VerticalSensitivity", _cameraLook.verticalSensitivity);
            PlayerPrefs.SetFloat("DeadzoneRadius", _cameraLook.deadzoneRadius);
        }
        if (BrightnessManager.Instance != null)
        {
            PlayerPrefs.SetFloat("PlayerBrightness", BrightnessManager.Instance.Brightness);
            PlayerPrefs.SetFloat("InteriorBrightness", BrightnessManager.Instance.InteriorBrightness);
        }
        if (_vignetteSlider != null)
            PlayerPrefs.SetFloat(VignettePrefKey, _vignetteSlider.value);
        if (_runtimeInvertYawToggle != null)
            PlayerPrefs.SetInt("InvertYaw", _runtimeInvertYawToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("IsCalibrated", 1);
        PlayerPrefs.Save();
    }

    private void ConfigureSlider(Slider slider, float min, float max, float defaultValue)
    {
        if (slider == null) return;
        float previousValue = slider.value;
        slider.minValue = min;
        slider.maxValue = max;
        slider.value = Mathf.Clamp(previousValue <= 0f ? defaultValue : previousValue, min, max);
    }

    // 타이틀 복귀/새 게임 시작 시 이전 씬의 자막이나 대화 상태가 남지 않도록 정리합니다.
    public void ResetTransientUI()
    {
        CloseCameraSetup();
        if (_sceneFlashImage != null) _sceneFlashImage.enabled = false;
        HideSubtitle();
        IsDialogueActive = false;
        isDayTransitioning = false;
        if (dayTransitionPanel != null) dayTransitionPanel.SetActive(false);
    }

    public IEnumerator ShowInteractiveSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) yield break;

        IsDialogueActive = true;
        subtitleText.text = NarrativeData.FormatDialogueText(message);
        subtitlePanel.SetActive(true);

        float duration = Mathf.Max(2.5f, message.Length * 0.1f);
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) break;
            if (PlayerController.Instance != null && PlayerController.Instance.IsGripTriggered) break;
            yield return null;
        }

        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }

    // 음성 대사 전용 자막: 지정된 음원 길이만큼 표시하고 입력으로 조기 종료하지 않습니다.
    public IEnumerator ShowSubtitleForDuration(string message, float duration)
    {
        if (subtitlePanel == null || subtitleText == null) yield break;

        IsDialogueActive = true;
        subtitleText.text = NarrativeData.FormatDialogueText(message);
        subtitlePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));

        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }

    public void ShowSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) return;
        subtitleText.text = NarrativeData.FormatDialogueText(message);
        subtitlePanel.SetActive(true);
    }

    public IEnumerator ShowGameplayHint(string message, float duration)
    {
        if (subtitlePanel == null || subtitleText == null || IsDialogueActive) yield break;
        ShowSubtitle(message);
        string displayed = subtitleText.text;
        yield return new WaitForSecondsRealtime(duration);
        if (!IsDialogueActive && subtitleText.text == displayed) HideSubtitle();
    }

    public void HideSubtitle()
    {
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (subtitleText != null) subtitleText.text = "";
        IsDialogueActive = false;
    }

    public IEnumerator FadeOutScreen(float customDuration = -1f)
    {
        if (globalFadeCanvasGroup == null) yield break;
        float fadeTime = customDuration < 0f ? defaultFadeDuration : customDuration;
        globalFadeCanvasGroup.gameObject.SetActive(true);
        globalFadeCanvasGroup.blocksRaycasts = true;

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            globalFadeCanvasGroup.alpha = Mathf.Lerp(globalFadeCanvasGroup.alpha, 1f, timer / fadeTime);
            yield return null;
        }
        globalFadeCanvasGroup.alpha = 1f;
    }

    public IEnumerator FadeInScreen(float customDuration = -1f)
    {
        if (isDayTransitioning) yield break;

        if (globalFadeCanvasGroup == null) yield break;
        float fadeTime = customDuration < 0f ? defaultFadeDuration : customDuration;
        globalFadeCanvasGroup.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.unscaledDeltaTime;
            globalFadeCanvasGroup.alpha = Mathf.Lerp(globalFadeCanvasGroup.alpha, 0f, timer / fadeTime);
            yield return null;
        }
        globalFadeCanvasGroup.alpha = 0f;
        globalFadeCanvasGroup.blocksRaycasts = false;
        globalFadeCanvasGroup.gameObject.SetActive(false);
    }

    public IEnumerator ShowDayTransition(int day)
    {
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D("SND-071_DayChange_OneShot", AudioManager.Instance.uiMixerGroup, 0.75f);

        isDayTransitioning = true;
        if (dayTransitionPanel == null) { isDayTransitioning = false; yield break; }

        dayTransitionPanel.SetActive(true);
        if (dayTransitionText != null) dayTransitionText.text = "Day " + day;

        int index = day - 1;
        if (dayTransitionImage != null && dayTextures != null && index >= 0 && index < dayTextures.Length && dayTextures[index] != null)
        {
            dayTransitionImage.gameObject.SetActive(true);
            dayTransitionImage.texture = dayTextures[index];
        }

        CanvasGroup cg = dayTransitionPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dayTransitionPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        yield return _waitNormal;

        if (globalFadeCanvasGroup != null)
        {
            float fadeTime = defaultFadeDuration;
            float timer = 0f;
            while (timer < fadeTime)
            {
                timer += Time.unscaledDeltaTime;
                globalFadeCanvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeTime);
                yield return null;
            }
            globalFadeCanvasGroup.alpha = 0f;
            globalFadeCanvasGroup.blocksRaycasts = false;
            globalFadeCanvasGroup.gameObject.SetActive(false);
        }

        yield return _waitLong;

        float hideTimer = 0f;
        while (hideTimer < 1.0f) { hideTimer += Time.unscaledDeltaTime; cg.alpha = 1f - hideTimer; yield return null; }
        cg.alpha = 0f;

        dayTransitionPanel.SetActive(false);
        isDayTransitioning = false;
    }
}

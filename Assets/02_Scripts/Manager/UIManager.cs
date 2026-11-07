using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using TMPro;

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
    public bool IsDialogueActive { get; set; } = false;
    public bool isDayTransitioning { get; private set; } = false;

    private FirstPersonCameraLook _cameraLook;
    private List<VideoPlayer> _pausedVideos = new List<VideoPlayer>();
    private Button _controlsTabButton;
    private Button _gameTabButton;
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
        CreateVignetteOverlay();
        InitializeSettingsTabs();
        InitCalibrationUI();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _cameraLook = FindObjectOfType<FirstPersonCameraLook>();
        ApplyResponsiveCanvasSettings();
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
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 100f;
        }
    }

    private void CreateVignetteOverlay()
    {
        if (_vignetteImage != null) return;

        GameObject canvasObject = new GameObject("Runtime_Vignette_Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 1;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject imageObject = new GameObject("Vignette", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
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
                texture.SetPixel(x, y, new Color(0f, 0f, 0f, alpha));
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
        Image rootImage = _runtimeSettingsRoot.AddComponent<Image>();
        rootImage.color = new Color(0.035f, 0.045f, 0.06f, 0.97f);

        TextMeshProUGUI title = CreateText("설정", _runtimeSettingsRoot.transform, 34, TextAlignmentOptions.Center);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = new Vector2(0.05f, 0.88f);
        titleRect.anchorMax = new Vector2(0.95f, 0.99f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;

        _controlsTabButton = CreateRuntimeButton("조작 설정", _runtimeSettingsRoot.transform);
        _gameTabButton = CreateRuntimeButton("게임 설정", _runtimeSettingsRoot.transform);
        SetRect(_controlsTabButton.GetComponent<RectTransform>(), new Vector2(0.27f, 0.79f), new Vector2(0.48f, 0.87f), Vector2.zero, Vector2.zero);
        SetRect(_gameTabButton.GetComponent<RectTransform>(), new Vector2(0.52f, 0.79f), new Vector2(0.73f, 0.87f), Vector2.zero, Vector2.zero);
        _controlsTabButton.onClick.AddListener(() => SetSettingsTab(true));
        _gameTabButton.onClick.AddListener(() => SetSettingsTab(false));

        // 스크롤 영역
        GameObject scrollObject = CreateUIObject("Settings_ScrollView", _runtimeSettingsRoot.transform);
        SetRect(scrollObject.GetComponent<RectTransform>(), new Vector2(0.06f, 0.15f), new Vector2(0.94f, 0.77f), new Vector2(0f, 0f), new Vector2(0f, 0f));
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

        _controlsSettingsGroup = CreateUIObject("Controls_Settings", content.transform);
        ConfigureVerticalGroup(_controlsSettingsGroup, 14f);
        _gameSettingsGroup = CreateUIObject("Game_Settings", content.transform);
        ConfigureVerticalGroup(_gameSettingsGroup, 14f);

        CreateSectionHeader("카메라", _controlsSettingsGroup.transform);
        GameObject cameraRow = CreateSettingRow("카메라 장치", _controlsSettingsGroup.transform);
        _cameraCycleButton = CreateRuntimeButton("카메라 검색 중...", cameraRow.transform);
        LayoutElement cameraButtonLayout = _cameraCycleButton.GetComponent<LayoutElement>();
        cameraButtonLayout.minWidth = 320f;
        cameraButtonLayout.preferredWidth = 420f;
        _cameraCycleLabel = _cameraCycleButton.GetComponentInChildren<TextMeshProUGUI>();
        _cameraCycleButton.onClick.AddListener(CycleCameraDevice);

        CreateSectionHeader("조작 감도", _controlsSettingsGroup.transform);
        horizontalSensitivitySlider = CreateSliderRow("좌우 회전 감도", 0.1f, 8f, 2f, _controlsSettingsGroup.transform, out _runtimeHorizontalValue);
        verticalSensitivitySlider = CreateSliderRow("상하 회전 감도", 0.1f, 8f, 2f, _controlsSettingsGroup.transform, out _runtimeVerticalValue);
        deadzoneSlider = CreateSliderRow("미세 떨림 무시", 0.01f, 0.15f, 0.03f, _controlsSettingsGroup.transform, out _runtimeDeadzoneValue);
        leanThresholdSlider = CreateSliderRow("몸 기울임 민감도", 0.05f, 3f, 0.15f, _controlsSettingsGroup.transform, out _runtimeLeanValue);
        reachThresholdSlider = CreateSliderRow("손 뻗기 민감도", 0.1f, 0.6f, 0.2f, _controlsSettingsGroup.transform, out _runtimeReachValue);

        GameObject toggleRow = CreateSettingRow("좌우 반전", _controlsSettingsGroup.transform);
        _runtimeInvertYawToggle = CreateRuntimeToggle(toggleRow.transform, "좌우 반전 사용");
        _runtimeInvertYawLabel = _runtimeInvertYawToggle.GetComponentInChildren<TextMeshProUGUI>();
        _runtimeInvertYawToggle.isOn = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
        _runtimeInvertYawToggle.onValueChanged.AddListener(v =>
        {
            if (_cameraLook != null) _cameraLook.SetInvertYaw(v);
            PlayerPrefs.SetInt("InvertYaw", v ? 1 : 0);
            PlayerPrefs.Save();
        });

        Button recalibrateButton = CreateRuntimeButton("카메라 영점 재설정", _controlsSettingsGroup.transform);
        recalibrateButton.onClick.AddListener(OnClickRecalibrate);

        CreateSectionHeader("화면 및 조명", _gameSettingsGroup.transform);
        _vignetteSlider = CreateSliderRow("시야 가장자리 어둡기", 0f, 0.65f, PlayerPrefs.GetFloat(VignettePrefKey, DefaultVignetteStrength), _gameSettingsGroup.transform, out _runtimeVignetteValue);
        _vignetteSlider.onValueChanged.AddListener(SetVignetteStrength);
        brightnessSlider = CreateSliderRow("화면 시야 밝기", 0.2f, 1f, 1f, _gameSettingsGroup.transform, out _runtimeScreenBrightnessValue);
        interiorBrightnessSlider = CreateSliderRow("실내 조명 밝기", 1f, 2.5f,
            BrightnessManager.Instance != null ? BrightnessManager.Instance.defaultInteriorBrightness : 1.35f,
            _gameSettingsGroup.transform, out _runtimeInteriorBrightnessValue);

        // 하단 고정 버튼
        GameObject footer = CreateUIObject("Settings_Footer", _runtimeSettingsRoot.transform);
        SetRect(footer.GetComponent<RectTransform>(), new Vector2(0.06f, 0.025f), new Vector2(0.94f, 0.12f), Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup footerLayout = footer.AddComponent<HorizontalLayoutGroup>();
        footerLayout.spacing = 18f;
        footerLayout.childControlWidth = true;
        footerLayout.childControlHeight = true;
        footerLayout.childForceExpandWidth = true;
        footerLayout.childForceExpandHeight = true;
        btnResume = CreateRuntimeButton("계속하기", footer.transform);
        btnQuit = CreateRuntimeButton("타이틀로 돌아가기", footer.transform);
        btnResume.onClick.AddListener(ResumeGame);
        btnQuit.onClick.AddListener(OnClickQuit);

        PopulateRuntimeCameraList();
        SetSettingsTab(true);
    }

    private void SetSettingsTab(bool showControls)
    {
        if (_controlsSettingsGroup != null) _controlsSettingsGroup.SetActive(showControls);
        if (_gameSettingsGroup != null) _gameSettingsGroup.SetActive(!showControls);
        UpdateSettingsTabVisuals(showControls);
    }

    private void UpdateSettingsTabVisuals(bool showControls)
    {
        SetRuntimeButtonSelected(_controlsTabButton, showControls);
        SetRuntimeButtonSelected(_gameTabButton, !showControls);
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
        image.color = new Color(0.13f, 0.19f, 0.24f, 1f);
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
        Image image = button.GetComponent<Image>();
        if (image != null) image.color = selected
            ? new Color(0.12f, 0.42f, 0.48f, 1f)
            : new Color(0.13f, 0.19f, 0.24f, 1f);
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
        header.color = new Color(0.55f, 0.85f, 0.9f, 1f);
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
        rowElement.minHeight = 58f;
        rowElement.preferredHeight = 62f;
        TextMeshProUGUI title = CreateText(label, row.transform, 20f, TextAlignmentOptions.Left);
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.minWidth = 260f;
        titleLayout.preferredWidth = 300f;
        titleLayout.flexibleWidth = 0f;
        return row;
    }

    private Slider CreateSliderRow(string label, float min, float max, float defaultValue, Transform parent, out TextMeshProUGUI valueText)
    {
        GameObject row = CreateSettingRow(label, parent);
        GameObject sliderObject = CreateUIObject("Slider_" + label, row.transform);
        Image background = sliderObject.AddComponent<Image>();
        background.color = new Color(0.12f, 0.15f, 0.18f, 1f);
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
        StretchRect(fillAreaRect, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        StretchRect(fill.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = new Color(0.2f, 0.72f, 0.78f, 1f);
        slider.fillRect = fill.GetComponent<RectTransform>();

        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObject.transform);
        StretchRect(handleArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(8f, 0f), new Vector2(-8f, 0f));
        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(18f, 28f);
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
            _selectedCameraIndex = _availableCameraNames.FindIndex(n => !n.Contains("OBS") && !n.Contains("Virtual"));
            if (_selectedCameraIndex < 0) _selectedCameraIndex = 0;
        }
        UpdateCameraButtonLabel();
    }

    private void CycleCameraDevice()
    {
        if (_availableCameraNames.Count == 0)
        {
            PopulateRuntimeCameraList();
            if (_availableCameraNames.Count == 0) return;
        }
        _selectedCameraIndex = (_selectedCameraIndex + 1) % _availableCameraNames.Count;
        string selectedName = _availableCameraNames[_selectedCameraIndex];
        if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.ReceiveCameraSelection(selectedName);
        UpdateCameraButtonLabel();
    }

    private void UpdateCameraButtonLabel()
    {
        if (_cameraCycleLabel == null) return;
        string label = _availableCameraNames.Count == 0
            ? "카메라 없음 - 다시 눌러 검색"
            : "카메라: " + _availableCameraNames[Mathf.Clamp(_selectedCameraIndex, 0, _availableCameraNames.Count - 1)] + "  (눌러서 변경)";
        _cameraCycleLabel.text = label;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
            {
                if (systemMenuPanel != null && systemMenuPanel.activeInHierarchy)
                {
                    ResumeGame();
                }
                return;
            }
            TogglePause();
        }

        if (!IsPaused && !IsDialogueActive)
        {
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage != GameStage.Title)
            {
                if (Cursor.lockState != CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
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
        return IsPaused || isMainMenuActive || isIntroVideoActive;
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

        if (systemMenuPanel != null) systemMenuPanel.SetActive(true);
        InitializeSettingsTabs();
        SetSettingsTab(true);

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

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        foreach (VideoPlayer vp in _pausedVideos)
        {
            if (vp != null) vp.Play();
        }
        _pausedVideos.Clear();

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void OnClickQuit()
    {
        ResetTransientUI();
        SaveCalibrationSettings();
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
    }

    public void OnClickRecalibrate()
    {
        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.RecalibrateOrigin();
        }
        ResumeGame();
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
        HideSubtitle();
        IsDialogueActive = false;
        isDayTransitioning = false;
        if (dayTransitionPanel != null) dayTransitionPanel.SetActive(false);
    }

    public IEnumerator ShowInteractiveSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) yield break;

        IsDialogueActive = true;
        subtitleText.text = message;
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
        subtitleText.text = message;
        subtitlePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(Mathf.Max(0.01f, duration));

        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }

    public void ShowSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) return;
        subtitleText.text = message;
        subtitlePanel.SetActive(true);
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
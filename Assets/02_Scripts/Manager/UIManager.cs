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

        InitCalibrationUI();
        InitializeSettingsTabs();
        ApplyResponsiveCanvasSettings();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyResponsiveCanvasSettings();
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

    private void InitializeSettingsTabs()
    {
        if (_settingsTabsInitialized || systemMenuPanel == null)
            return;

        _settingsTabsInitialized = true;
        _controlsTabButton = CreateSettingsTabButton("조작 설정", new Vector2(260f, 300f));
        _gameTabButton = CreateSettingsTabButton("게임 설정", new Vector2(520f, 300f));

        if (_controlsTabButton != null)
            _controlsTabButton.onClick.AddListener(() => SetSettingsTab(true));
        if (_gameTabButton != null)
            _gameTabButton.onClick.AddListener(() => SetSettingsTab(false));

        SetSettingsTab(true);
    }

    private Button CreateSettingsTabButton(string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(
            "SettingsTab_" + label.Replace(" ", ""),
            typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(systemMenuPanel.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 54f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.16f, 0.2f, 0.96f);

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = new Color(0.18f, 0.23f, 0.28f, 1f),
            highlightedColor = new Color(0.28f, 0.38f, 0.44f, 1f),
            pressedColor = new Color(0.08f, 0.12f, 0.15f, 1f),
            selectedColor = new Color(0.28f, 0.38f, 0.44f, 1f),
            disabledColor = new Color(0.12f, 0.12f, 0.12f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.08f
        };
        button.navigation = new Navigation { mode = Navigation.Mode.None };

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = label;
        if (subtitleText != null && subtitleText.font != null)
            text.font = subtitleText.font;
        else if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 25f;
        text.color = Color.white;
        text.raycastTarget = false;
        return button;
    }

    private void SetSettingsTab(bool showControls)
    {
        if (systemMenuPanel == null) return;

        Slider[] sliders = systemMenuPanel.GetComponentsInChildren<Slider>(true);
        foreach (Slider slider in sliders)
        {
            if (slider == null) continue;
            bool isBrightness = brightnessSlider != null && slider == brightnessSlider;
            bool isLegacyRotation = rotationSpeedSlider != null &&
                                    slider == rotationSpeedSlider &&
                                    (horizontalSensitivitySlider != null || verticalSensitivitySlider != null);
            slider.gameObject.SetActive(showControls
                ? (!isBrightness && !isLegacyRotation)
                : isBrightness);
        }

        if (cameraDropdown != null) cameraDropdown.gameObject.SetActive(showControls);
        if (invertYawToggle != null) invertYawToggle.gameObject.SetActive(showControls);
        if (btnRecalibrate != null) btnRecalibrate.gameObject.SetActive(showControls);

        Transform cameraLabel = systemMenuPanel.transform.Find("Camera_Select_Text");
        if (cameraLabel != null) cameraLabel.gameObject.SetActive(showControls);

        UpdateSettingsTabVisuals(showControls);
    }

    private void UpdateSettingsTabVisuals(bool showControls)
    {
        if (_controlsTabButton != null)
        {
            Image image = _controlsTabButton.GetComponent<Image>();
            if (image != null) image.color = showControls
                ? new Color(0.18f, 0.42f, 0.48f, 1f)
                : new Color(0.12f, 0.16f, 0.2f, 0.96f);
        }
        if (_gameTabButton != null)
        {
            Image image = _gameTabButton.GetComponent<Image>();
            if (image != null) image.color = !showControls
                ? new Color(0.18f, 0.42f, 0.48f, 1f)
                : new Color(0.12f, 0.16f, 0.2f, 0.96f);
        }
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

        PopulateCameraDropdown();

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

        if (cameraDropdown != null && cameraDropdown.options.Count > 0)
        {
            string selectedName = cameraDropdown.options[cameraDropdown.value].text;
            if (VisionTrackingManager.Instance != null)
            {
                VisionTrackingManager.Instance.ReceiveCameraSelection(selectedName);
            }
        }

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

    private void PopulateCameraDropdown()
    {
        if (cameraDropdown == null) return;

        WebCamDevice[] devices = WebCamTexture.devices;
        cameraDropdown.ClearOptions();
        List<string> options = new List<string>();
        int defaultIndex = 0;
        int activeIndex = -1;

        string currentSelected = "";
        if (VisionTrackingManager.Instance != null && !string.IsNullOrEmpty(VisionTrackingManager.Instance.SelectedDeviceName))
        {
            currentSelected = VisionTrackingManager.Instance.SelectedDeviceName;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            options.Add(devices[i].name);

            if (devices[i].name == currentSelected)
            {
                activeIndex = i;
            }

            if (!devices[i].name.Contains("OBS") && !devices[i].name.Contains("Virtual") && defaultIndex == 0)
            {
                defaultIndex = i;
            }
        }

        if (options.Count > 0)
        {
            cameraDropdown.AddOptions(options);

            if (activeIndex != -1)
            {
                cameraDropdown.value = activeIndex;
            }
            else
            {
                cameraDropdown.value = defaultIndex;
            }

            cameraDropdown.RefreshShownValue();
        }
    }

    private void InitCalibrationUI()
    {
        if (btnResume != null) { btnResume.onClick.RemoveAllListeners(); btnResume.onClick.AddListener(ResumeGame); }
        if (btnQuit != null) { btnQuit.onClick.RemoveAllListeners(); btnQuit.onClick.AddListener(OnClickQuit); }
        if (btnRecalibrate != null) { btnRecalibrate.onClick.RemoveAllListeners(); btnRecalibrate.onClick.AddListener(OnClickRecalibrate); }

        bool useSplitSensitivitySliders = horizontalSensitivitySlider != null || verticalSensitivitySlider != null;
        if (rotationSpeedSlider != null)
        {
            rotationSpeedSlider.minValue = 1f;
            rotationSpeedSlider.maxValue = 8f;
            if (useSplitSensitivitySliders) rotationSpeedSlider.gameObject.SetActive(false);
        }
        if (rotationValueText != null && useSplitSensitivitySliders) rotationValueText.gameObject.SetActive(false);
        ConfigureSlider(horizontalSensitivitySlider, 0.1f, 8f, 2f);
        ConfigureSlider(verticalSensitivitySlider, 0.1f, 8f, 2f);
        ConfigureSlider(brightnessSlider, 0.2f, 1f, 1f);

        if (invertYawToggle != null)
        {
            invertYawToggle.isOn = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
            invertYawToggle.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetInvertYaw(v); });
        }

        if (leanThresholdSlider != null) leanThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.leanDepthThreshold = v; UpdateSliderTexts(); });
        if (reachThresholdSlider != null) reachThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.reachDepthThreshold = v; UpdateSliderTexts(); });
        if (rotationSpeedSlider != null && horizontalSensitivitySlider == null && verticalSensitivitySlider == null) rotationSpeedSlider.onValueChanged.AddListener(v =>
        {
            if (_cameraLook != null)
            {
                _cameraLook.headRotationSpeed = v;
                _cameraLook.SetHorizontalSensitivity(v);
                _cameraLook.SetVerticalSensitivity(v);
            }
            UpdateSliderTexts();
        });
        if (horizontalSensitivitySlider != null) horizontalSensitivitySlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetHorizontalSensitivity(v); UpdateSliderTexts(); });
        if (verticalSensitivitySlider != null) verticalSensitivitySlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetVerticalSensitivity(v); UpdateSliderTexts(); });
        if (brightnessSlider != null) brightnessSlider.onValueChanged.AddListener(v => { if (BrightnessManager.Instance != null) BrightnessManager.Instance.SetBrightness(v); UpdateSliderTexts(); });
        if (deadzoneSlider != null) deadzoneSlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.deadzoneRadius = v; UpdateSliderTexts(); });

        if (cameraDropdown != null)
        {
            cameraDropdown.onValueChanged.RemoveAllListeners();
            cameraDropdown.onValueChanged.AddListener(index => {
                if (cameraDropdown.options.Count > index)
                {
                    string selectedName = cameraDropdown.options[index].text;
                    if (VisionTrackingManager.Instance != null)
                    {
                        VisionTrackingManager.Instance.ReceiveCameraSelection(selectedName);
                    }
                }
            });
        }
    }

    private void SyncSlidersToCurrentValues()
    {
        if (VisionTrackingManager.Instance != null)
        {
            if (leanThresholdSlider != null) leanThresholdSlider.value = VisionTrackingManager.Instance.leanDepthThreshold;
            if (reachThresholdSlider != null) reachThresholdSlider.value = VisionTrackingManager.Instance.reachDepthThreshold;
        }
        if (_cameraLook != null)
        {
            if (rotationSpeedSlider != null) rotationSpeedSlider.value = _cameraLook.headRotationSpeed;
            if (horizontalSensitivitySlider != null) horizontalSensitivitySlider.value = _cameraLook.horizontalSensitivity;
            if (verticalSensitivitySlider != null) verticalSensitivitySlider.value = _cameraLook.verticalSensitivity;
            if (deadzoneSlider != null) deadzoneSlider.value = _cameraLook.deadzoneRadius;
        }
        if (brightnessSlider != null && BrightnessManager.Instance != null) brightnessSlider.value = BrightnessManager.Instance.Brightness;
        UpdateSliderTexts();
    }

    private void UpdateSliderTexts()
    {
        if (leanThresholdSlider != null && leanValueText != null) leanValueText.text = string.Format("이동(기울임) 민감도: {0:F2}", leanThresholdSlider.value);
        if (reachThresholdSlider != null && reachValueText != null) reachValueText.text = string.Format("조작(손뻗기) 민감도: {0:F2}", reachThresholdSlider.value);
        if (rotationSpeedSlider != null && rotationValueText != null) rotationValueText.text = string.Format("화면 회전 감도(구버전): {0:F1}", rotationSpeedSlider.value);
        if (horizontalSensitivitySlider != null && horizontalSensitivityValueText != null) horizontalSensitivityValueText.text = string.Format("좌우 회전 감도: {0:F1}", horizontalSensitivitySlider.value);
        if (verticalSensitivitySlider != null && verticalSensitivityValueText != null) verticalSensitivityValueText.text = string.Format("상하 회전 감도: {0:F1}", verticalSensitivitySlider.value);
        if (brightnessSlider != null && brightnessValueText != null) brightnessValueText.text = string.Format("시야 밝기: {0:P0}", brightnessSlider.value);
        if (deadzoneSlider != null && deadzoneValueText != null) deadzoneValueText.text = string.Format("미세 떨림 무시(데드존): {0:F3}", deadzoneSlider.value);
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
        if (BrightnessManager.Instance != null) PlayerPrefs.SetFloat("PlayerBrightness", BrightnessManager.Instance.Brightness);
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
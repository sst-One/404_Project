using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
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

    private WaitForSecondsRealtime _waitQuick;
    private WaitForSecondsRealtime _waitNormal;
    private WaitForSecondsRealtime _waitLong;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
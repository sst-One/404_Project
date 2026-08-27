using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
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

    [Header("ESC Menu: Buttons")]
    public Button btnResume;
    public Button btnQuit;

    [Header("ESC Menu: Sliders & Toggles")]
    public Slider rotationSpeedSlider;
    public Slider leanThresholdSlider;
    public Slider reachThresholdSlider;
    public Slider deadzoneSlider;
    public Toggle invertYawToggle;

    [Header("ESC Menu: Value Texts")]
    public TextMeshProUGUI rotationValueText;
    public TextMeshProUGUI leanValueText;
    public TextMeshProUGUI reachValueText;
    public TextMeshProUGUI deadzoneValueText;

    public bool IsPaused { get; private set; } = false;
    public bool IsDialogueActive { get; set; } = false;
    public bool isDayTransitioning { get; private set; } = false;

    private FirstPersonCameraLook _cameraLook;

    // [완전 최적화] 코루틴 대기 객체 캐싱 (Heap 할당 제로화)
    private WaitForSecondsRealtime _waitQuick;
    private WaitForSecondsRealtime _waitNormal;
    private WaitForSecondsRealtime _waitLong;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // [핵심 픽스] 캐싱 객체 실제 할당
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
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

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
    }

    public bool IsAnyUIBlocking()
    {
        bool isMainMenuActive = TitleController.Instance != null &&
                                TitleController.Instance.titlePanel != null &&
                                TitleController.Instance.titlePanel.activeInHierarchy;
        return IsPaused || isMainMenuActive;
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

        if (systemMenuPanel != null) systemMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        SaveCalibrationSettings();

        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        // [최적화 적용] new 키워드 제거
        yield return _waitQuick;

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void OnClickQuit()
    {
        SaveCalibrationSettings();
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
    }

    private void InitCalibrationUI()
    {
        if (btnResume != null) { btnResume.onClick.RemoveAllListeners(); btnResume.onClick.AddListener(ResumeGame); }
        if (btnQuit != null) { btnQuit.onClick.RemoveAllListeners(); btnQuit.onClick.AddListener(OnClickQuit); }

        if (rotationSpeedSlider != null) { rotationSpeedSlider.minValue = 1f; rotationSpeedSlider.maxValue = 20f; }

        if (invertYawToggle != null)
        {
            invertYawToggle.isOn = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
            invertYawToggle.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.SetInvertYaw(v); });
        }

        if (leanThresholdSlider != null) leanThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.leanDepthThreshold = v; UpdateSliderTexts(); });
        if (reachThresholdSlider != null) reachThresholdSlider.onValueChanged.AddListener(v => { if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.reachDepthThreshold = v; UpdateSliderTexts(); });
        if (rotationSpeedSlider != null) rotationSpeedSlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.headRotationSpeed = v; UpdateSliderTexts(); });
        if (deadzoneSlider != null) deadzoneSlider.onValueChanged.AddListener(v => { if (_cameraLook != null) _cameraLook.deadzoneRadius = v; UpdateSliderTexts(); });
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
            if (deadzoneSlider != null) deadzoneSlider.value = _cameraLook.deadzoneRadius;
        }
        UpdateSliderTexts();
    }

    private void UpdateSliderTexts()
    {
        if (leanThresholdSlider != null && leanValueText != null) leanValueText.text = string.Format("이동(기울임) 민감도: {0:F2}", leanThresholdSlider.value);
        if (reachThresholdSlider != null && reachValueText != null) reachValueText.text = string.Format("조작(손뻗기) 민감도: {0:F2}", reachThresholdSlider.value);
        if (rotationSpeedSlider != null && rotationValueText != null) rotationValueText.text = string.Format("화면 회전 감도: {0:F0}", rotationSpeedSlider.value);
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
            PlayerPrefs.SetFloat("DeadzoneRadius", _cameraLook.deadzoneRadius);
        }
        PlayerPrefs.SetInt("IsCalibrated", 1);
        PlayerPrefs.Save();
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
        if (subtitlePanel == null || subtitleText == null) return;
        subtitlePanel.SetActive(false);
        subtitleText.text = "";
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

        // [최적화 적용] new 키워드 제거
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

        // [최적화 적용] new 키워드 제거
        yield return _waitLong;

        float hideTimer = 0f;
        while (hideTimer < 1.0f) { hideTimer += Time.unscaledDeltaTime; cg.alpha = 1f - hideTimer; yield return null; }
        cg.alpha = 0f;

        dayTransitionPanel.SetActive(false);
        isDayTransitioning = false;
    }
}
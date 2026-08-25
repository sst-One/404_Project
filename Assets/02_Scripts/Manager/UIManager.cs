using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Reference")]
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
    [Tooltip("Day 1부터 Day 5까지 차례대로 배열에 5개의 텍스처를 할당하세요.")]
    public Texture[] dayTextures; // [핫픽스] 배열 집중화

    public UnityEngine.UI.Button btnResume;
    public UnityEngine.UI.Button btnQuit;

    public bool IsPaused { get; private set; } = false;
    public bool IsDialogueActive { get; set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

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
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        if (btnResume != null)
        {
            btnResume.onClick.RemoveAllListeners();
            btnResume.onClick.AddListener(TogglePause);
        }
        if (btnQuit != null)
        {
            btnQuit.onClick.RemoveAllListeners();
            btnQuit.onClick.AddListener(OnClickQuit);
        }
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (GameFlowManager.Instance != null &&
               (GameFlowManager.Instance.currentStage == GameStage.Title || GameFlowManager.Instance.currentStage == GameStage.Tutorial))
            {
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

        if (systemMenuPanel != null) systemMenuPanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        yield return new WaitForSecondsRealtime(0.1f);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnClickResume() { ResumeGame(); }
    public void OnClickQuit()
    {
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        Time.timeScale = 1f;
        AudioListener.pause = false;
        IsPaused = false;
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
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

    // [핫픽스] 텍스처 인자를 없애고 자체 배열에서 인덱스 참조
    public IEnumerator ShowDayTransition(int day)
    {
        if (dayTransitionPanel == null) yield break;

        dayTransitionPanel.SetActive(true);
        if (dayTransitionText != null) dayTransitionText.text = "Day " + day;

        // 인덱스는 0부터 시작하므로 day - 1
        int index = day - 1;
        if (dayTransitionImage != null && dayTextures != null && index >= 0 && index < dayTextures.Length && dayTextures[index] != null)
        {
            dayTransitionImage.gameObject.SetActive(true);
            dayTransitionImage.texture = dayTextures[index];
        }
        else if (dayTransitionImage != null) dayTransitionImage.gameObject.SetActive(false);

        CanvasGroup cg = dayTransitionPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dayTransitionPanel.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        float timer = 0f;
        while (timer < 1.0f) { timer += Time.unscaledDeltaTime; cg.alpha = timer; yield return null; }
        cg.alpha = 1f;

        yield return new WaitForSecondsRealtime(2.5f);

        timer = 0f;
        while (timer < 1.0f) { timer += Time.unscaledDeltaTime; cg.alpha = 1f - timer; yield return null; }
        cg.alpha = 0f;

        dayTransitionPanel.SetActive(false);
    }
}
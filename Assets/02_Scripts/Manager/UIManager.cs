using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Reference")]
    public GameObject systemMenuPanel;
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    [Header("Global Screen Fade")]
    public CanvasGroup globalFadeCanvasGroup;

    public UnityEngine.UI.Button btnResume;
    public UnityEngine.UI.Button btnQuit;

    public bool IsPaused { get; private set; } = false;

    // [수정점] 외부 컷신 컨트롤러에서 컷신 잠금을 제어할 수 있도록 private set을 set으로 개방
    public bool IsDialogueActive { get; set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        if (subtitlePanel != null) subtitlePanel.SetActive(false);
        if (globalFadeCanvasGroup != null) globalFadeCanvasGroup.alpha = 0f;

        Time.timeScale = 1f;
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
        bool isTitleActive = TitleController.Instance != null &&
                             TitleController.Instance.titlePanel != null &&
                             TitleController.Instance.titlePanel.activeInHierarchy;

        return IsPaused || IsDialogueActive || isTitleActive;
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
        IsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OnClickResume()
    {
        ResumeGame();
    }

    public void OnClickQuit()
    {
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);

        Time.timeScale = 1f;
        IsPaused = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
    }

    public IEnumerator ShowInteractiveSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) yield break;

        IsDialogueActive = true;
        subtitleText.text = message;
        subtitlePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(0.5f);

        bool isSkipped = false;
        bool wasReaching = false;

        if (PlayerController.Instance != null)
        {
            wasReaching = PlayerController.Instance.IsGripHeld;
        }

        while (!isSkipped)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.IsGripHeld && !wasReaching)
            {
                isSkipped = true;
            }
            if (PlayerController.Instance != null)
            {
                wasReaching = PlayerController.Instance.IsGripHeld;
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isSkipped = true;
            }

            yield return null;
        }

        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }

    public IEnumerator FadeOutScreen(float fadeDuration)
    {
        if (globalFadeCanvasGroup == null) yield break;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            globalFadeCanvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
            yield return null;
        }
        globalFadeCanvasGroup.alpha = 1f;
    }
}
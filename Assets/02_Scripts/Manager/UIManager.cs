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

    public UnityEngine.UI.Button btnResume;
    public UnityEngine.UI.Button btnQuit;

    // 공용 블로킹 상태 플래그
    public bool IsPaused { get; private set; } = false;
    public bool IsDialogueActive { get; private set; } = false;

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

    // --- [1. 통합된 UI 블로킹 체크] ---
    public bool IsAnyUIBlocking()
    {
        bool isTitleActive = TitleController.Instance != null &&
                             TitleController.Instance.titlePanel != null &&
                             TitleController.Instance.titlePanel.activeInHierarchy;

        return IsPaused || IsDialogueActive || isTitleActive;
    }

    // --- [2. 흡수된 SystemMenuController 로직] ---
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

        // 씬 전환 전 정지된 시간을 복구하고 상태를 초기화합니다.
        Time.timeScale = 1f;
        IsPaused = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
        else
        {
            Debug.LogError("[UIManager] GameFlowManager를 찾을 수 없습니다.");
        }
    }

    // --- [3. 흡수된 SubtitleController 로직] ---
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
}
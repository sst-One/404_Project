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
    public Texture[] dayTextures;

    public UnityEngine.UI.Button btnResume;
    public UnityEngine.UI.Button btnQuit;

    public bool IsPaused { get; private set; } = false;
    public bool IsDialogueActive { get; set; } = false;
    public bool isDayTransitioning { get; private set; } = false;

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
        // [핫픽스 3] Day 전환 중일 때는 강제 페이드(밝아짐)를 무시하고 락을 검
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

    // [요구사항 100% 동기화 시퀀스]
    public IEnumerator ShowDayTransition(int day)
    {
        isDayTransitioning = true;
        if (dayTransitionPanel == null)
        {
            isDayTransitioning = false;
            yield break;
        }

        // Day 패널 세팅 및 100% 불투명 처리 (현재 글로벌 블랙 화면에 가려져 보이지 않음)
        dayTransitionPanel.SetActive(true);
        if (dayTransitionText != null) dayTransitionText.text = "Day " + day;

        int index = day - 1;
        if (dayTransitionImage != null && dayTextures != null && index >= 0 && index < dayTextures.Length && dayTextures[index] != null)
        {
            dayTransitionImage.gameObject.SetActive(true);
            dayTransitionImage.texture = dayTextures[index];
        }
        else if (dayTransitionImage != null) dayTransitionImage.gameObject.SetActive(false);

        CanvasGroup cg = dayTransitionPanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = dayTransitionPanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;

        // 1. 이전 씬 종료 후 계속 어두운 상태로 0.5초 대기
        yield return new WaitForSecondsRealtime(0.5f);

        // 2. 0.5초 뒤 밝아지면서 (글로벌 페이드 해제) Day 이미지가 보임
        if (globalFadeCanvasGroup != null)
        {
            float fadeTime = defaultFadeDuration; // 보통 0.4~0.8초
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

        // 3. 밝아진 상태로 Day 이미지를 약 1.5초 유지하며 감상
        yield return new WaitForSecondsRealtime(1.5f);

        // 4. 서서히 사라지며 정상 게임 화면으로 복귀
        float hideTimer = 0f;
        while (hideTimer < 1.0f) { hideTimer += Time.unscaledDeltaTime; cg.alpha = 1f - hideTimer; yield return null; }
        cg.alpha = 0f;

        dayTransitionPanel.SetActive(false);
        isDayTransitioning = false; // 락 해제
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("메인 메뉴 UI")]
    public GameObject titlePanel;
    public GameObject creditsPanel;

    [Header("버튼 할당 (인스펙터 필수)")]
    public Button btnStartGame;
    public Button btnSettings;
    public Button btnCredits;
    public Button btnCloseCredits;
    public Button btnQuitGame;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (btnStartGame != null)
        {
            btnStartGame.onClick.RemoveAllListeners();
            btnStartGame.onClick.AddListener(OnClickStartGame);
        }

        if (btnSettings != null)
        {
            btnSettings.onClick.RemoveAllListeners();
            btnSettings.onClick.AddListener(OnClickSettings);
        }

        if (btnCredits != null)
        {
            btnCredits.onClick.RemoveAllListeners();
            btnCredits.onClick.AddListener(OnClickCredits);
        }

        if (btnCloseCredits != null)
        {
            btnCloseCredits.onClick.RemoveAllListeners();
            btnCloseCredits.onClick.AddListener(OnClickCloseCredits);
        }

        if (btnQuitGame != null)
        {
            btnQuitGame.onClick.RemoveAllListeners();
            btnQuitGame.onClick.AddListener(OnClickQuitGame);
        }

        if (titlePanel != null) titlePanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void Start()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            ShowMainMenu();

            if (UIManager.Instance != null)
            {
                StartCoroutine(UIManager.Instance.FadeInScreen());
            }
        }
    }

    public void ShowMainMenu()
    {
        if (titlePanel != null) titlePanel.SetActive(true);

        // [핫픽스] 크레딧 창을 닫을 때만 꺼지도록 명확히 제어
        if (creditsPanel != null) creditsPanel.SetActive(false);

        if (btnStartGame != null) btnStartGame.interactable = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideMainMenu()
    {
        if (titlePanel != null) titlePanel.SetActive(false);
    }

    private void OnClickStartGame()
    {
        if (btnStartGame != null) btnStartGame.interactable = false;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen());
        }

        yield return new WaitForSecondsRealtime(0.5f);

        HideMainMenu();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Tutorial);
        }
    }

    private void OnClickSettings()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.TogglePause();
        }
    }

    private void OnClickCredits()
    {
        // [핫픽스] HideMainMenu() 삭제: 배경(MainMenu_Panel)이 그대로 켜져 있고 그 위에 크레딧이 덮어짐
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private void OnClickCloseCredits()
    {
        // 배경은 어차피 켜져 있으므로 크레딧 창만 끄면 됨
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    private void OnClickQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();    
#endif
    }
}
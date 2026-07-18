using System.Collections;
using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject mainMenuPanel;
    public GameObject explanationPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        // 타이틀 씬 진입 시에만 커서 해제 및 메뉴 표시
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ShowMainMenu();
        }
        else
        {
            // 인게임 중이라면 메뉴 숨김 처리
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (explanationPanel != null) explanationPanel.SetActive(false);
        }
    }

    public void OnClickStartGame()
    {
        Debug.Log("[MainMenu] 게임 시작 클릭. 온보딩 및 페이드 연출 시작.");
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);

        if (OnboardingController.Instance != null)
        {
            OnboardingController.Instance.StartOnboarding(() =>
            {
                StartGameAction();
            });
        }
        else
        {
            StartGameAction();
        }
    }

    private void StartGameAction()
    {
        // 커서 잠금 처리 후 GameFlowManager에게 씬 전환(페이드 포함) 위임
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage1_Elevator);
        }
    }

    public void OnClickExplanation()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (explanationPanel != null) explanationPanel.SetActive(true);
    }

    public void OnClickCloseExplanation()
    {
        ShowMainMenu();
    }

    public void OnClickQuitGame()
    {
        Debug.Log("[MainMenu] 게임 종료.");
        Application.Quit();
    }

    private void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (explanationPanel != null) explanationPanel.SetActive(false);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("메인 메뉴 UI")]
    public GameObject titlePanel;
    public GameObject[] titleUIContainers;
    public GameObject explanationPanel;

    [Header("버튼 할당 (인스펙터 필수)")]
    public Button btnStartGame;
    public Button btnTutorial;
    public Button btnExplanation;
    public Button btnCloseExplanation;
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
        if (btnTutorial != null)
        {
            btnTutorial.onClick.RemoveAllListeners();
            btnTutorial.onClick.AddListener(OnClickStartGame);
        }
        if (btnExplanation != null)
        {
            btnExplanation.onClick.RemoveAllListeners();
            btnExplanation.onClick.AddListener(OnClickExplanation);
        }
        if (btnCloseExplanation != null)
        {
            btnCloseExplanation.onClick.RemoveAllListeners();
            btnCloseExplanation.onClick.AddListener(OnClickCloseExplanation);
        }
        if (btnQuitGame != null)
        {
            btnQuitGame.onClick.RemoveAllListeners();
            btnQuitGame.onClick.AddListener(OnClickQuitGame);
        }

        if (titlePanel != null) titlePanel.SetActive(false);
        if (explanationPanel != null) explanationPanel.SetActive(false);
    }

    private void Start()
    {
        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title)
        {
            ShowMainMenu();
        }
    }

    public void ShowMainMenu()
    {
        if (titlePanel != null) titlePanel.SetActive(false);

        foreach (var container in titleUIContainers)
        {
            if (container != null && container != titlePanel) container.SetActive(true);
        }

        if (explanationPanel != null) explanationPanel.SetActive(false);

        if (btnStartGame != null) btnStartGame.interactable = true;
        if (btnTutorial != null) btnTutorial.interactable = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void HideMainMenu()
    {
        foreach (var container in titleUIContainers)
        {
            if (container != null && container != this.gameObject) container.SetActive(false);
        }
    }

    private void OnClickStartGame()
    {
        if (btnStartGame != null) btnStartGame.interactable = false;
        if (btnTutorial != null) btnTutorial.interactable = false;

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        // UIManager에 위임 (파라미터 공란)
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen());
        }

        HideMainMenu();

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Tutorial);
        }

        // 로딩 지연 버퍼 단축
        yield return new WaitForSecondsRealtime(0.2f);

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeInScreen());
        }
    }

    private void OnClickExplanation()
    {
        HideMainMenu();
        if (explanationPanel != null) explanationPanel.SetActive(true);
    }

    private void OnClickCloseExplanation()
    {
        ShowMainMenu();
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
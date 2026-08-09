using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("페이드 연출 전용 (검은 화면)")]
    public GameObject titlePanel;
    public Image fadeBackground;
    public TextMeshProUGUI titleText;

    [Header("메인 메뉴 UI")]
    public GameObject[] titleUIContainers;
    public GameObject explanationPanel;

    [Header("버튼 할당 (인스펙터 필수)")]
    public Button btnStartGame;
    public Button btnTutorial; // [추가됨] 튜토리얼 버튼 전용 슬롯
    public Button btnExplanation;
    public Button btnCloseExplanation;
    public Button btnQuitGame;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // 인스펙터의 OnClick() 리스트에 Missing이 떠있어도 코드가 강제로 연결을 덮어씌웁니다.
        if (btnStartGame != null)
        {
            btnStartGame.onClick.RemoveAllListeners();
            btnStartGame.onClick.AddListener(OnClickStartGame);
        }
        if (btnTutorial != null)
        {
            btnTutorial.onClick.RemoveAllListeners();
            btnTutorial.onClick.AddListener(OnClickStartGame); // 튜토리얼 씬으로 연결
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
            if (container != null && container != titlePanel)
            {
                container.SetActive(true);
            }
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
            if (container != null && container != this.gameObject)
            {
                container.SetActive(false);
            }
        }
    }

    private void OnClickStartGame()
    {
        Debug.Log("[TitleController] Start/Tutorial 버튼 클릭! 튜토리얼 씬 로드 요청.");
        if (btnStartGame != null) btnStartGame.interactable = false;
        if (btnTutorial != null) btnTutorial.interactable = false;

        HideMainMenu();

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Tutorial);
        }
        else
        {
            Debug.LogError("[TitleController 치명적 오류] GameFlowManager가 씬에 존재하지 않습니다!");
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

    public void FadeOutWithText(string text, float duration)
    {
        if (titlePanel == null) return;
        StartCoroutine(FadeOutTextRoutine(text, duration));
    }

    public void FadeInOnly(float duration)
    {
        if (titlePanel == null) return;
        StartCoroutine(FadeInRoutine(duration));
    }

    private IEnumerator FadeOutTextRoutine(string text, float duration)
    {
        titlePanel.SetActive(true);

        Color bgColor = fadeBackground != null ? fadeBackground.color : Color.black;
        Color textColor = titleText != null ? titleText.color : Color.white;

        if (titleText != null) titleText.text = text;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(0f, 1f, timer / duration);

            if (fadeBackground != null)
            {
                bgColor.a = alpha;
                fadeBackground.color = bgColor;
            }
            if (titleText != null && !string.IsNullOrEmpty(text))
            {
                textColor.a = alpha;
                titleText.color = textColor;
            }
            yield return null;
        }
    }

    private IEnumerator FadeInRoutine(float duration)
    {
        Color bgColor = fadeBackground != null ? fadeBackground.color : Color.black;
        Color textColor = titleText != null ? titleText.color : Color.white;

        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / duration);

            if (fadeBackground != null)
            {
                bgColor.a = alpha;
                fadeBackground.color = bgColor;
            }
            if (titleText != null)
            {
                textColor.a = alpha;
                titleText.color = textColor;
            }
            yield return null;
        }

        titlePanel.SetActive(false);
    }
}
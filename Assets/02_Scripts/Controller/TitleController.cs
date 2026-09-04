using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("메인 메뉴 UI")]
    public GameObject titlePanel;
    public GameObject creditsPanel;

    [Header("도입 영상 세팅 (인스펙터 직접 연결)")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;

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

        // [결함 1 코드 픽스] 초기화 시 영상 패널을 끄고, Video Player의 자동 재생을 강제로 정지시킴
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (introVideoPlayer != null) introVideoPlayer.Stop();
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

        // [결함 2 픽스] 여기서 HideMainMenu()를 즉시 호출하던 것을 제거하여, 배경이 갑자기 사라지는 현상 방지
        StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        // 1. 메인 메뉴가 켜져 있는 상태에서 페이드 아웃을 먼저 수행 (자연스러운 블랙 스크린 전환)
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));
        }

        // 화면이 완전히 까매진 후 메인 메뉴를 숨김
        HideMainMenu();

        // 2. 도입 영상 시퀀스
        if (introVideoPanel != null && introVideoPlayer != null)
        {
            introVideoPanel.SetActive(true);
            introVideoPlayer.Play();

            // 버퍼링 방어 후 페이드 인 (영상 노출)
            yield return new WaitForSeconds(0.2f);
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.0f));

            // 영상 튕김 방지 및 온전한 재생 대기
            yield return new WaitForSeconds(1.0f);
            while (introVideoPlayer.isPlaying)
            {
                yield return null;
            }

            // 3. 영상 재생 완료 후 다시 페이드 아웃
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));

            introVideoPanel.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 4. 완벽한 블랙 스크린 상태에서 튜토리얼 씬 로드
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
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    private void OnClickCloseCredits()
    {
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
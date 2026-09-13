using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.InputSystem;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("메인 메뉴 UI")]
    public GameObject titlePanel;
    public GameObject creditsPanel;

    [Header("도입 영상 세팅 (인스펙터 직접 연결)")]
    public GameObject introVideoPanel;
    public VideoPlayer introVideoPlayer;
    [Tooltip("S키를 눌러 스킵 문구가 적힌 UI 오브젝트를 연결하세요.")]
    public GameObject skipPromptUI; // [스킵 픽스] 스킵 안내 UI 슬롯 추가

    [Header("버튼 할당 (인스펙터 필수)")]
    public Button btnStartGame;
    public Button btnSettings;
    public Button btnCredits;
    public Button btnCloseCredits;
    public Button btnQuitGame;

    private Coroutine _transitionCoroutine;
    private bool _isTransitioning = false;

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

        // 초기화 시 영상 패널 끄기 및 자동 재생 정지
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (introVideoPlayer != null) introVideoPlayer.Stop();
        if (skipPromptUI != null) skipPromptUI.SetActive(false);
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

    private void Update()
    {
        // [스킵 픽스] 영상 재생(트랜지션) 중일 때 S키를 누르면 강제 스킵 발동
        if (_isTransitioning && Keyboard.current != null && Keyboard.current.sKey.wasPressedThisFrame)
        {
            SkipIntroSequence();
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

        _isTransitioning = true;
        _transitionCoroutine = StartCoroutine(StartGameTransitionRoutine());
    }

    private IEnumerator StartGameTransitionRoutine()
    {
        // 1. 메인 메뉴가 켜져 있는 상태에서 페이드 아웃 (자연스러운 블랙 스크린 전환)
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));
        }

        HideMainMenu();

        // 2. 도입 영상 시퀀스
        if (introVideoPanel != null && introVideoPlayer != null)
        {
            introVideoPanel.SetActive(true);
            introVideoPlayer.Play();

            // 버퍼링 방어 후 페이드 인 (영상 노출)
            yield return new WaitForSeconds(0.2f);
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.0f));

            // 영상 튕김 방지 대기 후 스킵 안내 UI 노출
            yield return new WaitForSeconds(1.0f);
            if (skipPromptUI != null) skipPromptUI.SetActive(true);

            // 온전한 재생 대기
            while (introVideoPlayer.isPlaying)
            {
                yield return null;
            }

            if (skipPromptUI != null) skipPromptUI.SetActive(false);

            // 3. 영상 재생 완료 후 다시 페이드 아웃
            if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));

            introVideoPanel.SetActive(false);
        }
        else
        {
            yield return new WaitForSecondsRealtime(0.5f);
        }

        // 4. 블랙 스크린 상태에서 튜토리얼 씬 로드
        _isTransitioning = false;
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Tutorial);
        }
    }

    // [스킵 픽스] 스킵 시 강제 호출되는 인터럽트 로직
    private void SkipIntroSequence()
    {
        Debug.Log("[TitleController] 유저 입력 감지. 도입 영상 스킵 및 튜토리얼 강제 진입.");

        _isTransitioning = false;

        // 돌고 있는 기존 페이드 및 재생 코루틴 사살
        if (_transitionCoroutine != null)
        {
            StopCoroutine(_transitionCoroutine);
            _transitionCoroutine = null;
        }

        // 영상 및 스킵 UI 즉시 종료
        if (introVideoPlayer != null) introVideoPlayer.Stop();
        if (introVideoPanel != null) introVideoPanel.SetActive(false);
        if (skipPromptUI != null) skipPromptUI.SetActive(false);

        // 시각적 꼬임 방지: 화면을 즉시 0초 만에 완벽한 암전 상태로 덮어씌움
        if (UIManager.Instance != null)
        {
            StartCoroutine(UIManager.Instance.FadeOutScreen(0f));
        }

        // 즉시 스테이지 강제 전이
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
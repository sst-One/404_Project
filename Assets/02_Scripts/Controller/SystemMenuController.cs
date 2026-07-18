using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SystemMenuController : MonoBehaviour
{
    public static SystemMenuController Instance { get; private set; }
    public bool IsPaused { get; private set; } = false;

    [Header("UI Reference")]
    public GameObject systemMenuPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);
        // 씬 로드 시 예외 방지를 위해 타임스케일 강제 정상화
        Time.timeScale = 1f;
        IsPaused = false;
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 타이틀 화면에서는 ESC 메뉴 호출 차단
            if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Title) return;

            TogglePause();
        }
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

        // 메뉴 조작을 위해 커서 해제 및 가시화
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("[SystemMenu] 게임 일시정지 (ESC). 커서 활성화.");
    }

    public void ResumeGame()
    {
        if (!IsPaused) return;

        // UI 비활성화 및 커서 숨김 처리는 지연 없이 즉시 실행
        if (systemMenuPanel != null) systemMenuPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;

        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        // 중복 입력 방지를 위한 최소 프레임 대기 후 시간 정상화
        yield return new WaitForSecondsRealtime(0.1f);

        Time.timeScale = 1f;
        IsPaused = false;
        Debug.Log("[SystemMenu] 게임 복귀 완료. 타임스케일 정상화.");
    }

    // UI 버튼 (계속하기) 에서 호출
    public void OnClickResume()
    {
        ResumeGame();
    }

    // UI 버튼 (종료하기) 에서 호출
    public void OnClickQuit()
    {
        Debug.Log("[SystemMenu] 게임 종료 요청.");
        Application.Quit();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SystemMenuController : MonoBehaviour
{
    public static SystemMenuController Instance { get; private set; }

    public bool IsPaused { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // ESC 키 입력 감지 (Fallback/디버그 포함)
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
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
        IsPaused = true;
        Time.timeScale = 0f;
        Debug.Log("[SystemMenu] 게임 일시정지 (ESC). 모든 입력이 차단됩니다.");

        // 향후 UI 매니저를 통해 일시정지 메뉴 캔버스 활성화 로직 추가
    }

    private void ResumeGame()
    {
        Debug.Log("[SystemMenu] 게임 복귀 대기 중 (0.2s 유예)...");
        // 입력 중복(Bounce) 방지를 위해 코루틴으로 지연 복귀 처리 (POL-009)
        StartCoroutine(ResumeRoutine());
    }

    private IEnumerator ResumeRoutine()
    {
        // UI가 닫히는 페이드 효과 시간을 위한 Unscaled Time 대기
        yield return new WaitForSecondsRealtime(0.2f);

        Time.timeScale = 1f;
        IsPaused = false;
        Debug.Log("[SystemMenu] 게임 복귀 완료. 입력이 활성화됩니다.");
    }
}
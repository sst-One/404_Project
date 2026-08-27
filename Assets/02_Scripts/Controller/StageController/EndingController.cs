using System.Collections;
using UnityEngine;

public class EndingController : MonoBehaviour
{
    [Header("Ending Credits UI")]
    public GameObject creditsPanel;
    public float creditDuration = 25.0f; // 크레딧 대기 시간

    private void Start()
    {
        // 1. 엔딩 씬 진입 즉시 플레이어 이동 및 시점 완벽 차단
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementLock(true);
            PlayerController.Instance.SetCameraLock(true);
        }

        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // 크레딧 패널 켜기
        if (creditsPanel != null) creditsPanel.SetActive(true);

        // 페이드 인으로 씬 시작
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeInScreen(2.0f));
        }

        // 크레딧 진행 대기
        yield return new WaitForSeconds(creditDuration);

        // 크레딧 패널 끄기
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // 2. 크레딧 종료 후 global_Fade 화면을 켜서 검은 배경 유지
        if (UIManager.Instance != null && UIManager.Instance.globalFadeCanvasGroup != null)
        {
            UIManager.Instance.globalFadeCanvasGroup.gameObject.SetActive(true);
            UIManager.Instance.globalFadeCanvasGroup.alpha = 1f;
            UIManager.Instance.globalFadeCanvasGroup.blocksRaycasts = true;
        }

        // 마우스 커서 잠금 해제 (메인메뉴 조작을 위해)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. 메인 메뉴 패널 켜기 및 타이틀 씬 상태로 전환하여 다시하기/종료 선택 보장
        if (TitleController.Instance != null && TitleController.Instance.titlePanel != null)
        {
            TitleController.Instance.titlePanel.SetActive(true);
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
    }
}
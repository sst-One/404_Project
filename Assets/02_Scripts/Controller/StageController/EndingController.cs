using System.Collections;
using UnityEngine;
using TMPro;

public class EndingController : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup titleCanvasGroup;
    public RectTransform creditsRectTransform;

    [Header("Timing Settings")]
    public float titleHoldTime = 2.0f;
    public float titleFadeTime = 1.5f;
    public float creditScrollSpeed = 50.0f;
    public float endDelay = 3.0f;

    [Tooltip("크레딧이 멈출 최종 Y좌표 수동 설정 (0이면 자동 계산)")]
    public float overrideTargetY = 0f;

    [Header("Audio Settings")]
    public AudioSource endingBgmSource;
    public string endingBgmClipName = "SND-065_EndingTheme_Loop";

    private void Start()
    {
        if (titleCanvasGroup != null) titleCanvasGroup.alpha = 0f;

        // BGM 재생 연동 (AudioManager 라우팅)
        if (endingBgmSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(endingBgmClipName);
            if (clip != null)
            {
                endingBgmSource.clip = clip;
                endingBgmSource.Play();
            }
        }

        // 마우스 커서 숨김 및 UI 상호작용 강제 차단 (POL-017)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (UIManager.Instance != null) UIManager.Instance.IsDialogueActive = true;

        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // 1. 타이틀 페이드 인
        if (titleCanvasGroup != null)
        {
            float timer = 0f;
            while (timer < titleFadeTime)
            {
                timer += Time.deltaTime;
                titleCanvasGroup.alpha = Mathf.Clamp01(timer / titleFadeTime);
                yield return null;
            }
            titleCanvasGroup.alpha = 1f;

            // 2. 타이틀 유지
            yield return new WaitForSeconds(titleHoldTime);

            // 3. 타이틀 페이드 아웃
            timer = 0f;
            while (timer < titleFadeTime)
            {
                timer += Time.deltaTime;
                titleCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / titleFadeTime);
                yield return null;
            }
            titleCanvasGroup.alpha = 0f;
        }

        // 4. 크레딧 스크롤
        if (creditsRectTransform != null)
        {
            float targetY = overrideTargetY > 0f ? overrideTargetY : creditsRectTransform.rect.height + Screen.height;
            while (creditsRectTransform.anchoredPosition.y < targetY)
            {
                creditsRectTransform.anchoredPosition += new Vector2(0f, creditScrollSpeed * Time.deltaTime);
                yield return null;
            }
        }

        // 5. 종료 대기
        yield return new WaitForSeconds(endDelay);

        // 6. 타이틀 화면 복귀 및 제어권 반환 (강제 종료 Application.Quit() 폐기)
        if (UIManager.Instance != null) UIManager.Instance.IsDialogueActive = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
    }
}
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

    [Header("Audio Clip Name")]
    public string endingBgmClipName = "SND-065_EndingTheme_Loop";

    private void Start()
    {
        if (titleCanvasGroup != null) titleCanvasGroup.alpha = 0f;

        // [최적화] BGM 재생 로직을 전역 AudioManager.PlayBGM으로 완전히 위임
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(endingBgmClipName))
        {
            AudioManager.Instance.PlayBGM(endingBgmClipName);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (UIManager.Instance != null) UIManager.Instance.IsDialogueActive = true;

        StartCoroutine(EndingSequenceRoutine());
    }

    private IEnumerator EndingSequenceRoutine()
    {
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

            yield return new WaitForSeconds(titleHoldTime);

            timer = 0f;
            while (timer < titleFadeTime)
            {
                timer += Time.deltaTime;
                titleCanvasGroup.alpha = 1f - Mathf.Clamp01(timer / titleFadeTime);
                yield return null;
            }
            titleCanvasGroup.alpha = 0f;
        }

        if (creditsRectTransform != null)
        {
            float targetY = overrideTargetY > 0f ? overrideTargetY : creditsRectTransform.rect.height + Screen.height;
            while (creditsRectTransform.anchoredPosition.y < targetY)
            {
                creditsRectTransform.anchoredPosition += new Vector2(0f, creditScrollSpeed * Time.deltaTime);
                yield return null;
            }
        }

        yield return new WaitForSeconds(endDelay);

        if (AudioManager.Instance != null) AudioManager.Instance.StopBGM();

        if (UIManager.Instance != null) UIManager.Instance.IsDialogueActive = false;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
    }
}
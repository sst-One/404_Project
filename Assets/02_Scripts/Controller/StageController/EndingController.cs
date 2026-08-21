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

    private void Start()
    {
        if (titleCanvasGroup != null) titleCanvasGroup.alpha = 0f;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }

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
            float targetY = creditsRectTransform.rect.height + Screen.height;
            while (creditsRectTransform.anchoredPosition.y < targetY)
            {
                creditsRectTransform.anchoredPosition += new Vector2(0f, creditScrollSpeed * Time.deltaTime);
                yield return null;
            }
        }

        yield return new WaitForSeconds(endDelay);
        QuitGame();
    }

    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
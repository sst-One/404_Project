using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TitleController : MonoBehaviour
{
    public static TitleController Instance { get; private set; }

    [Header("UI Reference")]
    public GameObject titlePanel;
    public Image fadeBackground;
    public TextMeshProUGUI titleText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (titlePanel != null) titlePanel.SetActive(false);
    }

    // GameFlowManager나 씬 시작 시 TitleController.Instance.ShowTitleFade("Day 1"); 으로 호출
    public void ShowTitleFade(string text, float holdTime = 2f)
    {
        if (titlePanel == null) return;
        StartCoroutine(FadeRoutine(text, holdTime));
    }

    private IEnumerator FadeRoutine(string text, float holdTime)
    {
        titlePanel.SetActive(true);
        titleText.text = text;

        Color bgColor = fadeBackground.color;
        Color textColor = titleText.color;

        // 1. 페이드 인 (화면 어두워짐, 글자 나타남)
        float timer = 0f;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime;
            bgColor.a = Mathf.Lerp(0f, 1f, timer);
            textColor.a = Mathf.Lerp(0f, 1f, timer);
            fadeBackground.color = bgColor;
            titleText.color = textColor;
            yield return null;
        }

        // 2. 타이틀 유지
        yield return new WaitForSecondsRealtime(holdTime);

        // 3. 페이드 아웃 (화면 밝아짐, 글자 사라짐)
        timer = 0f;
        while (timer < 1f)
        {
            timer += Time.unscaledDeltaTime;
            bgColor.a = Mathf.Lerp(1f, 0f, timer);
            textColor.a = Mathf.Lerp(1f, 0f, timer);
            fadeBackground.color = bgColor;
            titleText.color = textColor;
            yield return null;
        }

        titlePanel.SetActive(false);
    }
}
using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleController : MonoBehaviour
{
    public static SubtitleController Instance { get; private set; }

    [Header("UI Reference")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText; // [수정] TextMeshProUGUI로 타입 변경 완료

    private Coroutine hideCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (subtitlePanel != null)
        {
            subtitlePanel.SetActive(false);
        }
    }

    public void ShowSubtitle(string message, float duration = 3.0f)
    {
        if (subtitlePanel == null || subtitleText == null) return;

        subtitleText.text = message;
        subtitlePanel.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideSubtitleAfterDelay(duration));
    }

    private IEnumerator HideSubtitleAfterDelay(float delay)
    {
        // POL-008 일시정지 정책 대응을 위해 Realtime 사용
        yield return new WaitForSecondsRealtime(delay);
        subtitlePanel.SetActive(false);
        subtitleText.text = "";
    }
}
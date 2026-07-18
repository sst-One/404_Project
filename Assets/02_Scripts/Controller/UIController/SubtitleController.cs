using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections;

public class SubtitleController : MonoBehaviour
{
    public static SubtitleController Instance { get; private set; }

    [Header("UI Reference")]
    public GameObject subtitlePanel;
    public TextMeshProUGUI subtitleText;

    // 대화 진행 중 백그라운드 상호작용을 완벽히 차단하기 위한 플래그
    public bool IsDialogueActive { get; private set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (subtitlePanel != null) subtitlePanel.SetActive(false);
    }

    public IEnumerator ShowInteractiveSubtitle(string message)
    {
        if (subtitlePanel == null || subtitleText == null) yield break;

        // 대사 시작 시 잠금 활성화
        IsDialogueActive = true;
        subtitleText.text = message;
        subtitlePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(0.5f);

        yield return new WaitUntil(() => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame);

        // 대사 종료 시 패널 즉시 숨김 및 잠금 해제
        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }
}
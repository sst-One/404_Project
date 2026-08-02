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

        IsDialogueActive = true;
        subtitleText.text = message;
        subtitlePanel.SetActive(true);

        yield return new WaitForSecondsRealtime(0.5f);

        bool isSkipped = false;
        bool wasReaching = false;

        if (VisionTrackingManager.Instance != null)
        {
            wasReaching = VisionTrackingManager.Instance.GetReachState();
        }

        while (!isSkipped)
        {
            // 웹캠 Reach 입력 감지
            if (VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.isTracking)
            {
                bool currentReach = VisionTrackingManager.Instance.GetReachState();
                if (currentReach && !wasReaching) isSkipped = true;
                wasReaching = currentReach;
            }
            // Fallback PC 입력 감지
            else if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                isSkipped = true;
            }

            yield return null;
        }

        subtitlePanel.SetActive(false);
        subtitleText.text = "";
        IsDialogueActive = false;
    }
}
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("UI Controllers")]
    public SystemMenuController systemMenuController;
    public SubtitleController subtitleController;
    public OnboardingController onboardingController;
    public TitleController titleController;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // 인게임 상호작용 객체들이 UI 차단 상태를 확인할 때 사용하는 공통 속성
    public bool IsAnyUIBlocking()
    {
        bool isSystemMenuOpen = systemMenuController != null && systemMenuController.IsPaused;
        bool isSubtitleActive = subtitleController != null && subtitleController.IsDialogueActive;

        return isSystemMenuOpen || isSubtitleActive;
    }
}
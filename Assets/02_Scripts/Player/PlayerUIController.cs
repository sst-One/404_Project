using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerGazeController))]
[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerUIController : MonoBehaviour
{
    [Header("UI Reference")]
    public RectTransform indicatorRect;
    private Image indicatorImage;

    [Header("Color Settings (투명도 Alpha 조정됨)")]
    [Tooltip("허공, 장애물 등을 바라볼 때 (기본 연한 투명)")]
    public Color defaultColor = new Color(1f, 1f, 1f, 0.3f); // Alpha 30%

    [Tooltip("오브젝트를 스칠 때 (사물이 잘 보이도록 매우 투명)")]
    public Color hoverColor = new Color(1f, 0.92f, 0.016f, 0.15f); // Yellow 계열, Alpha 15%

    [Tooltip("오브젝트/바닥 응시 완료 시 (사물이 잘 보이도록 매우 투명)")]
    public Color readyColor = new Color(0f, 1f, 1f, 0.15f); // Cyan 계열, Alpha 15%

    [Tooltip("실제 이동 중")]
    public Color moveColor = new Color(1f, 0f, 1f, 0.3f); // Magenta 계열, Alpha 30%

    private PlayerGazeController gazeController;
    private PlayerInputProvider inputProvider;
    private PlayerMovement playerMovement;

    private void Awake()
    {
        gazeController = GetComponent<PlayerGazeController>();
        inputProvider = GetComponent<PlayerInputProvider>();
        playerMovement = GetComponent<PlayerMovement>();

        if (indicatorRect != null)
        {
            indicatorImage = indicatorRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (indicatorRect == null || indicatorImage == null) return;

        // 1. UI 인디케이터 위치를 마우스 포인터(시점) 화면 좌표와 1:1로 동기화
        indicatorRect.position = inputProvider.GazeScreenPosition;

        // 2. 상태에 따른 즉각적인 색상 및 투명도 변형
        if (playerMovement != null && playerMovement.IsMoving)
        {
            indicatorImage.color = moveColor;
        }
        else if (gazeController.IsFloorValid)
        {
            indicatorImage.color = readyColor;
        }
        else if (gazeController.CurrentHoverTarget != null)
        {
            if (gazeController.IsTargetReady)
            {
                indicatorImage.color = readyColor;
            }
            else
            {
                indicatorImage.color = hoverColor;
            }
        }
        else
        {
            indicatorImage.color = defaultColor;
        }
    }
}
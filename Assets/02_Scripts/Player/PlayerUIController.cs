using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerGazeController))]
[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerUIController : MonoBehaviour
{
    [Header("UI Reference")]
    public RectTransform indicatorRect;
    private Image indicatorImage;

    [Header("Color Settings")]
    public Color defaultColor = new Color(1f, 1f, 1f, 0.3f);
    public Color hoverColor = new Color(1f, 0.92f, 0.016f, 0.15f);
    public Color readyColor = new Color(0f, 1f, 1f, 0.15f);
    public Color moveColor = new Color(1f, 0f, 1f, 0.3f);

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
        // 씬 전환 시 인디케이터 연결이 끊어지는 현상 방어 및 자동 재연결
        if (indicatorRect == null)
        {
            GameObject indicatorObj = GameObject.Find("Indicator");
            if (indicatorObj != null)
            {
                indicatorRect = indicatorObj.GetComponent<RectTransform>();
                indicatorImage = indicatorObj.GetComponent<Image>();
            }

            if (indicatorRect == null) return;
        }

        if (indicatorImage == null) return;

        indicatorRect.position = inputProvider.GazeScreenPosition;

        if (playerMovement != null && playerMovement.IsMoving)
        {
            indicatorImage.color = moveColor;
        }
        else if (gazeController != null && gazeController.IsFloorValid)
        {
            indicatorImage.color = readyColor;
        }
        else if (gazeController != null && gazeController.CurrentHoverTarget != null)
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
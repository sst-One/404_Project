// [Player] PlayerUIController.cs (종속성 해제 및 최적화)
using UnityEngine;
using UnityEngine.UI;

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

    private void Awake()
    {
        if (indicatorRect != null)
        {
            indicatorImage = indicatorRect.GetComponent<Image>();
        }
    }

    private void Update()
    {
        if (PlayerController.Instance == null) return;

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

        indicatorRect.position = PlayerController.Instance.GazeScreenPosition;

        if (PlayerController.Instance.IsMoving)
        {
            indicatorImage.color = moveColor;
        }
        else if (PlayerController.Instance.IsFloorValid)
        {
            indicatorImage.color = readyColor;
        }
        else if (PlayerController.Instance.CurrentHoverTarget != null)
        {
            indicatorImage.color = PlayerController.Instance.IsTargetReady ? readyColor : hoverColor;
        }
        else
        {
            indicatorImage.color = defaultColor;
        }
    }
}
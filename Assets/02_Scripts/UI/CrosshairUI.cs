using UnityEngine;
using UnityEngine.UI;

public class CrosshairUI : MonoBehaviour
{
    public Image crosshairImage;
    public Color defaultColor = Color.white;
    public Color activeColor = Color.green;

    private void Start()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.OnFocusChanged += HandleFocusChanged;
        }
        SetCrosshairColor(defaultColor);
    }

    private void OnDestroy()
    {
        if (InteractionManager.Instance != null)
        {
            InteractionManager.Instance.OnFocusChanged -= HandleFocusChanged;
        }
    }

    private void HandleFocusChanged(bool hasFocus)
    {
        SetCrosshairColor(hasFocus ? activeColor : defaultColor);
    }

    private void SetCrosshairColor(Color color)
    {
        if (crosshairImage != null)
        {
            crosshairImage.color = color;
        }
    }
}
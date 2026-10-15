using UnityEngine;
using UnityEngine.UI;

/// <summary>전체 화면 검정 오버레이의 알파를 이용해 플레이어 시야 밝기를 조절합니다.</summary>
public class BrightnessManager : MonoBehaviour
{
    public static BrightnessManager Instance { get; private set; }

    [Header("Brightness Overlay")]
    [Tooltip("화면 전체를 덮는 검정 Image. Canvas의 최상단에 배치하세요.")]
    public Image brightnessOverlay;
    [Range(0.2f, 1f)] public float defaultBrightness = 1f;

    public float Brightness { get; private set; } = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        if (brightnessOverlay == null) brightnessOverlay = GetComponent<Image>();
        Brightness = PlayerPrefs.GetFloat("PlayerBrightness", defaultBrightness);
        ApplyBrightness();
    }

    public void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp(value, 0.2f, 1f);
        ApplyBrightness();
        PlayerPrefs.SetFloat("PlayerBrightness", Brightness);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness()
    {
        if (brightnessOverlay == null) return;
        Color overlayColor = brightnessOverlay.color;
        overlayColor.r = 0f;
        overlayColor.g = 0f;
        overlayColor.b = 0f;
        overlayColor.a = 1f - Brightness;
        brightnessOverlay.color = overlayColor;
        brightnessOverlay.raycastTarget = false;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}

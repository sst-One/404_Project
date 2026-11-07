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

    [Header("Interior Lighting")]
    [Tooltip("실내 조명 기본 배율. 1.35는 기존 조명보다 35% 밝습니다.")]
    [Range(1f, 2.5f)] public float defaultInteriorBrightness = 1.35f;
    public float InteriorBrightness { get; private set; } = 1.35f;

    private readonly System.Collections.Generic.Dictionary<Light, float> _baseLightIntensities =
        new System.Collections.Generic.Dictionary<Light, float>();
    private float _baseAmbientIntensity = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        if (brightnessOverlay == null) brightnessOverlay = GetComponent<Image>();
        Brightness = PlayerPrefs.GetFloat("PlayerBrightness", defaultBrightness);
        InteriorBrightness = PlayerPrefs.GetFloat("InteriorBrightness", defaultInteriorBrightness);
        _baseAmbientIntensity = RenderSettings.ambientIntensity;
        CacheSceneLights();
        ApplyBrightness();
        ApplyInteriorBrightness();
    }

    public void SetBrightness(float value)
    {
        Brightness = Mathf.Clamp(value, 0.2f, 1f);
        ApplyBrightness();
        PlayerPrefs.SetFloat("PlayerBrightness", Brightness);
        PlayerPrefs.Save();
    }

    public void SetInteriorBrightness(float value)
    {
        InteriorBrightness = Mathf.Clamp(value, 1f, 2.5f);
        ApplyInteriorBrightness();
        PlayerPrefs.SetFloat("InteriorBrightness", InteriorBrightness);
        PlayerPrefs.Save();
    }

    private void CacheSceneLights()
    {
        Light[] lights = FindObjectsOfType<Light>(true);
        foreach (Light lightSource in lights)
        {
            if (lightSource != null && !_baseLightIntensities.ContainsKey(lightSource))
                _baseLightIntensities.Add(lightSource, lightSource.intensity);
        }
    }

    private void ApplyInteriorBrightness()
    {
        // 씬 전환 뒤 새로 생성된 조명도 기본 강도를 저장해 적용합니다.
        CacheSceneLights();
        foreach (var pair in _baseLightIntensities)
        {
            if (pair.Key != null)
                pair.Key.intensity = pair.Value * InteriorBrightness;
        }
        RenderSettings.ambientIntensity = _baseAmbientIntensity * InteriorBrightness;
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

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // 씬마다 설정된 원래 환경광 강도를 새 기준값으로 저장한 뒤 사용자 배율을 적용합니다.
        _baseAmbientIntensity = RenderSettings.ambientIntensity;
        CacheSceneLights();
        ApplyInteriorBrightness();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }
}

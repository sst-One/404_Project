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
    [Tooltip("실내 조명 기본 배율. 1.6은 씬에 설정된 기본 조명보다 60% 밝습니다.")]
    [Range(1f, 2.5f)] public float defaultInteriorBrightness = 1.6f;
    public float InteriorBrightness { get; private set; } = 1.6f;

    private readonly System.Collections.Generic.Dictionary<Light, float> _baseLightIntensities =
        new System.Collections.Generic.Dictionary<Light, float>();
    private float _baseAmbientIntensity = 1f;
    private const float SceneBrightnessScale = 1.15f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged += OnActiveSceneChanged;
        if (brightnessOverlay == null) brightnessOverlay = GetComponent<Image>();
        // 기존 밝기 저장값은 이전 범위/씬 조명 기준으로 저장되어 있어
        // 이번 조정 버전에서 한 번만 기본 밝기 기준을 새로 적용합니다.
        const int brightnessSettingsVersion = 1;
        int savedVersion = PlayerPrefs.GetInt("BrightnessSettingsVersion", 0);
        if (savedVersion < brightnessSettingsVersion)
        {
            Brightness = defaultBrightness;
            InteriorBrightness = Mathf.Clamp(defaultInteriorBrightness, 1f, 2.5f);
            PlayerPrefs.SetFloat("PlayerBrightness", Brightness);
            PlayerPrefs.SetFloat("InteriorBrightness", InteriorBrightness);
            PlayerPrefs.SetInt("BrightnessSettingsVersion", brightnessSettingsVersion);
            PlayerPrefs.Save();
        }
        else
        {
            Brightness = Mathf.Clamp(PlayerPrefs.GetFloat("PlayerBrightness", defaultBrightness), 0.2f, 1f);
            InteriorBrightness = Mathf.Clamp(
                PlayerPrefs.GetFloat("InteriorBrightness", defaultInteriorBrightness), 1f, 2.5f);
        }
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
                pair.Key.intensity = pair.Value * InteriorBrightness * SceneBrightnessScale;
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
        // Additive 로드는 이 시점에 새 씬이 Active Scene이 아닐 수 있으므로
        // 조명만 캐시하고, 환경광 기준값은 activeSceneChanged에서 갱신합니다.
        CacheSceneLights();
        ApplyInteriorBrightness();
    }

    private void OnActiveSceneChanged(UnityEngine.SceneManagement.Scene previousScene,
        UnityEngine.SceneManagement.Scene newScene)
    {
        // RenderSettings는 Active Scene 기준이므로 씬 활성화가 끝난 뒤 원본값을 저장합니다.
        _baseAmbientIntensity = RenderSettings.ambientIntensity;
        CacheSceneLights();
        ApplyInteriorBrightness();
    }

    private void OnDestroy()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        UnityEngine.SceneManagement.SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (Instance == this) Instance = null;
    }
}

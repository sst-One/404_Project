using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class PulseLight : MonoBehaviour
{
    [Header("Light Pulse Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 3.0f;
    public float pulseSpeed = 2.0f;

    [Header("Object Emission")]
    public Color emissionColor = Color.white;
    [Min(0f)] public float emissionMinIntensity = 0.8f;
    [Min(0f)] public float emissionMaxIntensity = 3.5f;

    private Light _targetLight;
    private Renderer[] _targetRenderers;
    private Material[][] _rendererMaterials;
    private Coroutine _pulseCoroutine;
    private MaterialPropertyBlock _propertyBlock;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        _targetLight = GetComponent<Light>();
        _targetRenderers = GetComponentsInChildren<Renderer>(true);
        _rendererMaterials = new Material[_targetRenderers.Length][];
        for (int i = 0; i < _targetRenderers.Length; i++)
            _rendererMaterials[i] = _targetRenderers[i] != null
                ? _targetRenderers[i].sharedMaterials
                : new Material[0];
        _propertyBlock = new MaterialPropertyBlock();

        if (_targetLight != null)
        {
            _targetLight.enabled = false;
            _targetLight.color = Color.white;
        }

        ApplyEmission(Color.black);
    }

    public void StartPulse()
    {
        if (_pulseCoroutine != null) return;

        if (_targetLight != null)
        {
            _targetLight.enabled = true;
            _pulseCoroutine = StartCoroutine(PulseRoutine());
        }
    }

    public void StopPulse()
    {
        if (_pulseCoroutine != null)
        {
            StopCoroutine(_pulseCoroutine);
            _pulseCoroutine = null;
        }

        if (_targetLight != null)
        {
            _targetLight.intensity = 0f;
            _targetLight.enabled = false;
        }

        ApplyEmission(Color.black);
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float lerp = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            _targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, lerp);

            float emissionIntensity = Mathf.Lerp(
                emissionMinIntensity,
                emissionMaxIntensity,
                lerp
            );
            ApplyEmission(emissionColor * emissionIntensity);

            yield return null;
        }
    }

    private void ApplyEmission(Color color)
    {
        if (_targetRenderers == null) return;

        for (int rendererIndex = 0; rendererIndex < _targetRenderers.Length; rendererIndex++)
        {
            Renderer targetRenderer = _targetRenderers[rendererIndex];
            if (targetRenderer == null) continue;

            Material[] materials = _rendererMaterials[rendererIndex];
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty(EmissionColorId))
                    continue;

                // Emission 키워드는 셰이더에 따라 필요합니다. 색상 자체는 PropertyBlock으로
                // 지정하여 다른 오브젝트의 머티리얼 색상까지 바꾸지 않습니다.
                if (!material.IsKeywordEnabled("_EMISSION"))
                    material.EnableKeyword("_EMISSION");
            }

            targetRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetColor(EmissionColorId, color);
            targetRenderer.SetPropertyBlock(_propertyBlock);
        }
    }
}

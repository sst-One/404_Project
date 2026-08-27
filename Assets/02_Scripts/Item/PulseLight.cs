using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class PulseLight : MonoBehaviour
{
    [Header("Light Pulse Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 3.0f;
    public float pulseSpeed = 2.0f;

    private Light _targetLight;
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        _targetLight = GetComponent<Light>();

        // [핵심 픽스] 인스펙터 세팅을 무시하고 무조건 강제로 꺼진 상태로 초기화합니다.
        if (_targetLight != null) _targetLight.enabled = false;
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
    }

    private IEnumerator PulseRoutine()
    {
        while (true)
        {
            float lerp = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            _targetLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, lerp);
            yield return null;
        }
    }
}
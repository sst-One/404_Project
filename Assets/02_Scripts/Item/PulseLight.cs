using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Light))]
public class PulseLight : MonoBehaviour
{
    [Header("Light Pulse Settings")]
    public float minIntensity = 0.5f;
    public float maxIntensity = 3.0f;
    public float pulseSpeed = 2.0f;

    [Header("Behavior Settings")]
    public bool playOnEnable = true;
    public bool stopOnInteract = true;

    private Light _targetLight;
    private Coroutine _pulseCoroutine;
    private InteractableItem _interactable;

    private void Awake()
    {
        _targetLight = GetComponent<Light>();
        // 부모 오브젝트에서 InteractableItem을 탐색하여 상호작용 여부 감지
        _interactable = GetComponentInParent<InteractableItem>();
    }

    private void Start()
    {
        if (stopOnInteract && PlayerController.Instance != null)
        {
            PlayerController.Instance.OnInteractTriggered += HandlePlayerInteraction;
        }

        if (playOnEnable)
        {
            StartPulse();
        }
    }

    private void OnDestroy()
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.OnInteractTriggered -= HandlePlayerInteraction;
        }
    }

    private void HandlePlayerInteraction(Transform targetObject)
    {
        if (targetObject == null) return;

        // 이 광원의 부모 또는 상위 오브젝트가 상호작용 대상일 경우 빛을 끕니다.
        if (transform.IsChildOf(targetObject) || targetObject.IsChildOf(transform.root))
        {
            StopPulse();
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.OnInteractTriggered -= HandlePlayerInteraction;
            }
        }
    }

    public void StartPulse()
    {
        if (_pulseCoroutine != null) return;
        if (_targetLight == null) return;

        _targetLight.enabled = true;
        _pulseCoroutine = StartCoroutine(PulseRoutine());
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
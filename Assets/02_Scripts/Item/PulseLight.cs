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

    [Header("Stage 3-4 Special Policy")]
    [Tooltip("체크 시, 상호작용 후 완전히 꺼지지 않고 깜빡임을 재시작합니다 (버튼 2회 연속 조작용).")]
    public bool restartOnInteract = false;

    private Light _targetLight;
    private Coroutine _pulseCoroutine;

    private void Awake()
    {
        _targetLight = GetComponent<Light>();
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

        // 이 광원의 부모 또는 상위 오브젝트가 상호작용 대상일 경우
        if (transform.IsChildOf(targetObject) || targetObject.IsChildOf(transform.root))
        {
            if (restartOnInteract)
            {
                // [핫픽스] Stage 3-4 정책: 두 번 눌러야 하므로 완전히 끄지 않고 잠시 껐다가 재개함
                StopPulse();
                StartCoroutine(RestartDelayRoutine());
            }
            else
            {
                // 일반 오브젝트: 완전히 소등 후 이벤트 구독 해제
                StopPulse();
                if (PlayerController.Instance != null)
                {
                    PlayerController.Instance.OnInteractTriggered -= HandlePlayerInteraction;
                }
            }
        }
    }

    private IEnumerator RestartDelayRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        StartPulse();
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
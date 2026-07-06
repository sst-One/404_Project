using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HeartbeatUI : MonoBehaviour
{
    public Image vignetteImage;
    private Coroutine _blinkCoroutine;

    private void Start()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnHeartbeatLevelChanged += UpdateHeartbeatUI;
        }
        SetAlpha(0f);
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnHeartbeatLevelChanged -= UpdateHeartbeatUI;
        }
    }

    private void UpdateHeartbeatUI(HeartbeatLevel level)
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }

        switch (level)
        {
            case HeartbeatLevel.Stable:
                SetAlpha(0f);
                break;
            case HeartbeatLevel.Rise:
                _blinkCoroutine = StartCoroutine(BlinkRoutine(0.3f, 1.0f));
                break;
            case HeartbeatLevel.High:
                _blinkCoroutine = StartCoroutine(BlinkRoutine(0.6f, 0.3f));
                break;
            case HeartbeatLevel.Overload:
                SetAlpha(1f);
                break;
        }
    }

    private IEnumerator BlinkRoutine(float maxAlpha, float interval)
    {
        WaitForSeconds wait = new WaitForSeconds(interval);
        bool isVisible = false;

        while (true)
        {
            SetAlpha(isVisible ? 0f : maxAlpha);
            isVisible = !isVisible;
            yield return wait;
        }
    }

    private void SetAlpha(float alpha)
    {
        if (vignetteImage != null)
        {
            Color c = vignetteImage.color;
            c.a = alpha;
            vignetteImage.color = c;
        }
    }
}
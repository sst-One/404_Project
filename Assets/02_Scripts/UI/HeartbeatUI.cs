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
                // [수정된 부분] 투명도를 0.3에서 0.15로 연하게 낮춤
                _blinkCoroutine = StartCoroutine(BlinkRoutine(0.15f, 1.0f));
                break;
            case HeartbeatLevel.High:
                // [수정된 부분] 투명도를 0.6에서 0.3으로 연하게 낮춤
                _blinkCoroutine = StartCoroutine(BlinkRoutine(0.3f, 0.3f));
                break;
            case HeartbeatLevel.Overload:
                // [수정된 부분] 투명도를 1.0(완전 불투명)에서 0.5(반투명)로 연하게 낮춤
                SetAlpha(0.5f);
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
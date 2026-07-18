using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("오브젝트 정보")]
    public string itemName = "Test Object";
    public bool destroyOnPickup = false;

    [Header("Grip 실행 이벤트")]
    public UnityEvent onInteractEvent;

    private MeshRenderer _renderer;
    private Color _originalColor;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    public void OnFocusEnter()
    {
        if (_renderer != null) _renderer.material.color = Color.yellow;
    }

    public void OnFocusExit()
    {
        if (_renderer != null) _renderer.material.color = _originalColor;
    }

    public void OnReadyStateReached()
    {
        if (_renderer != null) _renderer.material.color = Color.green;
    }

    public void OnInteract()
    {
        if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive)
        {
            return;
        }

        Debug.Log($"[InteractableItem] '{itemName}' Reach 상호작용 완수");

        // [핵심 연동] PARAM-013 규격에 맞춘 소음(Noise) 발생
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.15f); // Mid 진입을 유도하는 리스크 수치
        }

        onInteractEvent?.Invoke();

        if (destroyOnPickup)
        {
            OnFocusExit();
            Destroy(gameObject);
        }
    }

    public void OnLean()
    {
        // Object 타겟이므로 Lean 무시
    }
}
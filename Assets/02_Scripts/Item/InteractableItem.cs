using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("오브젝트 정보")]
    public string itemName = "Test Object";

    [Tooltip("체크 해제 시 Reach 상호작용을 무시합니다. (시선 감지는 유지됨)")]
    public bool isInteractable = true;

    [Tooltip("체크 시 한 번 Reach 상호작용을 완료하면 자동으로 isInteractable이 꺼집니다.")]
    public bool interactOnlyOnce = true;

    public bool destroyOnPickup = false;

    [Header("Grip 실행 이벤트")]
    public UnityEvent onInteractEvent;

    public bool IsFocused { get; private set; } = false;
    private bool _hasInteracted = false;

    public void OnFocusEnter()
    {
        IsFocused = true;
        // [핫픽스] 렌더러 색상 강제 변경(Color.yellow) 삭제
    }

    public void OnFocusExit()
    {
        IsFocused = false;
        // [핫픽스] 렌더러 원상 복구 로직 삭제
    }

    public void OnReadyStateReached()
    {
        // [핫픽스] 렌더러 색상 강제 변경(Color.green) 삭제
    }

    public void OnInteract()
    {
        if (!isInteractable) return;
        if (interactOnlyOnce && _hasInteracted) return;

        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
        {
            return;
        }

        _hasInteracted = true;
        Debug.Log($"[InteractableItem] '{itemName}' Reach 상호작용 완수");

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.15f);
        }

        onInteractEvent?.Invoke();

        if (interactOnlyOnce)
        {
            isInteractable = false;
            Debug.Log($"[InteractableItem] '{itemName}' 1회 사용 완료. 상호작용 영구 잠금됨.");
        }

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
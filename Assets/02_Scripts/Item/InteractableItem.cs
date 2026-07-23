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

    // 외부(PhoneController 등)에서 시선이 닿았는지 확인하기 위한 프로퍼티
    public bool IsFocused { get; private set; } = false;

    private MeshRenderer _renderer;
    private Color _originalColor;
    private bool _hasInteracted = false;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null) _originalColor = _renderer.material.color;
    }

    public void OnFocusEnter()
    {
        IsFocused = true; // 상호작용이 잠겨 있어도 시선(Gaze) 감지 자체는 계속 작동함

        if (!isInteractable) return; // 상호작용이 잠겼다면 노란색 하이라이트 피드백은 끔
        if (_renderer != null) _renderer.material.color = Color.yellow;
    }

    public void OnFocusExit()
    {
        IsFocused = false;

        if (_renderer != null) _renderer.material.color = _originalColor;
    }

    public void OnReadyStateReached()
    {
        if (!isInteractable) return;
        if (_renderer != null) _renderer.material.color = Color.green;
    }

    public void OnInteract()
    {
        // 1. 잠금 상태이거나 이미 1회 사용을 마쳤다면 즉시 차단
        if (!isInteractable) return;
        if (interactOnlyOnce && _hasInteracted) return;

        // 2. 자막(대사) 진행 중이면 백그라운드 상호작용 차단
        if (SubtitleController.Instance != null && SubtitleController.Instance.IsDialogueActive)
        {
            return;
        }

        _hasInteracted = true;
        Debug.Log($"[InteractableItem] '{itemName}' Reach 상호작용 완수");

        // 3. 상호작용에 의한 소음 발생 (PARAM-013)
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.15f);
        }

        // 4. 연결된 이벤트 실행 (예: 휴대폰 회수 코루틴 시작)
        onInteractEvent?.Invoke();

        // 5. 1회용 오브젝트일 경우 즉시 영구 잠금 처리
        if (interactOnlyOnce)
        {
            isInteractable = false;
            if (_renderer != null) _renderer.material.color = _originalColor; // 피드백 초기화
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
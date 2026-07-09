using UnityEngine;
using UnityEngine.Events; // 인스펙터에서 이벤트를 연결하기 위한 모듈

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("오브젝트 정보")]
    public string itemName = "Test Object";

    [Tooltip("Grip 상호작용 후 이 오브젝트를 씬에서 삭제할 것인가? (예: 보석 줍기, 폰 줍기)")]
    public bool destroyOnPickup = false;

    [Header("Grip 실행 이벤트")]
    [Tooltip("Grip(주먹 쥐기/우클릭) 성공 시 실행될 동작을 여기에 + 버튼을 눌러 연결하세요.")]
    public UnityEvent onInteractEvent;

    private MeshRenderer _renderer;
    private Color _originalColor;

    private void Awake()
    {
        _renderer = GetComponent<MeshRenderer>();
        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
    }

    public void OnFocusEnter()
    {
        // 포커스 되었을 때 노란색으로 시각적 피드백
        if (_renderer != null) _renderer.material.color = Color.yellow;
        Debug.Log($"[InteractableItem] '{itemName}'에 시선이 닿았습니다.");
    }

    public void OnFocusExit()
    {
        // 포커스 해제 시 원상 복구
        if (_renderer != null) _renderer.material.color = _originalColor;
    }

    public void OnReadyStateReached()
    {
        // 0.5초 이상 바라보아 Ready 상태가 되면 초록색으로 변경
        if (_renderer != null) _renderer.material.color = Color.green;
        Debug.Log($"[InteractableItem] '{itemName}' Ready 상태 도달! (Grip 가능)");
    }

    public void OnInteract()
    {
        Debug.Log($"[InteractableItem] '{itemName}' Grip(상호작용) 실행 완수!");

        // 1. 시스템 연동: 사물을 조작하면 소음(Noise)이 발생 (SYS-002)
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.1f);
        }

        // 2. 인스펙터에 연결된 커스텀 이벤트들(문 열기, UI 띄우기 등) 일괄 실행
        onInteractEvent?.Invoke();

        // 3. 아이템 획득(파괴) 처리
        if (destroyOnPickup)
        {
            // 파괴 전 포커스 상태 초기화를 위해 InteractionManager에 상실 통보가 필요하므로
            // OnFocusExit을 수동으로 한 번 호출해주는 것이 안전합니다.
            OnFocusExit();
            Destroy(gameObject);
        }
    }

    public void OnLean()
    {
        // 이 오브젝트는 바닥(이동 지점)이 아니므로 Lean 입력을 무시합니다.
        Debug.Log($"[InteractableItem] '{itemName}'은(는) 상호작용 대상이므로 이동할 수 없습니다.");
    }
}
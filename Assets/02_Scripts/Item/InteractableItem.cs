using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("오브젝트 정보")]
    public string itemName = "Test Object";
    public bool isInteractable = true;
    public bool interactOnlyOnce = true;
    public bool destroyOnPickup = false;

    [Header("에디터 연결용 이벤트 (일반 사물용)")]
    public UnityEvent onInteractEvent;

    public event Action onInteractAction;

    public bool IsFocused { get; private set; } = false;
    protected bool _hasInteracted = false;

    public virtual void OnFocusEnter() { IsFocused = true; }
    public virtual void OnFocusExit() { IsFocused = false; }
    public virtual void OnReadyStateReached() { }

    public virtual void OnInteract()
    {
        if (!isInteractable) return;
        if (interactOnlyOnce && _hasInteracted) return;

        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) return;

        _hasInteracted = true;

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.15f);

        // [핵심 픽스: 자가 정화] 내가 클릭되었으니 내 밑에 있는 불빛만 확실하게 끕니다. (타 객체 간섭 0%)
        PulseLight pulse = GetComponentInChildren<PulseLight>(true);
        if (pulse != null) pulse.StopPulse();

        onInteractEvent?.Invoke();
        onInteractAction?.Invoke();

        if (interactOnlyOnce) isInteractable = false;

        if (destroyOnPickup)
        {
            OnFocusExit();
            Destroy(gameObject);
        }
    }

    public virtual void OnLean() { }

    public virtual void ResetInteraction()
    {
        _hasInteracted = false;
        isInteractable = true;
    }

    // 하단 유틸리티 함수만 교체하십시오.
    public void EnableInteractionWithLight()
    {
        // [강력 방어] 혹시 이전에 클릭된 적이 있다면 강제로 리셋
        _hasInteracted = false;
        isInteractable = true;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        PulseLight pulse = GetComponentInChildren<PulseLight>(true);
        if (pulse != null) pulse.StartPulse();

        // [강력 방어] 혹시라도 이 오브젝트에 Animator가 붙어서 상태를 강제로 끄고 있다면 Animator를 무력화시킵니다.
        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class InteractableItem : MonoBehaviour, IInteractable
{
    [Header("오브젝트 정보")]
    public string itemName = "Test Object";
    public bool isInteractable = true;
    [Tooltip("엘리베이터 버튼처럼 거리 제한 없이 시선으로만 대상 지정해야 하는 경우에만 활성화합니다.")]
    public bool ignorePlayerDistanceLimit = false;
    public bool interactOnlyOnce = true;
    public bool destroyOnPickup = false;

    [Header("메시 자체 발광")]
    public Color interactionGlowColor = Color.white;
    [Min(0f)] public float glowMinIntensity = 0.65f;
    [Min(0f)] public float glowMaxIntensity = 2.2f;
    [Min(0.05f)] public float glowPulseSpeed = 1.8f;

    [Header("에디터 연결용 이벤트 (일반 사물용)")]
    public UnityEvent onInteractEvent;

    public event Action onInteractAction;

    public bool IsFocused { get; private set; } = false;
    protected bool _hasInteracted = false;

    private sealed class GlowMaterial
    {
        public Renderer renderer;
        public int materialIndex;
        public Color originalEmission;
    }

    private readonly List<GlowMaterial> _glowMaterials = new List<GlowMaterial>();
    // UnityEngine.Object 기반 객체는 MonoBehaviour 필드 초기화/생성자에서 만들면 안 됩니다.
    // Awake에서 런타임 초기화합니다.
    private MaterialPropertyBlock _glowBlock;
    private Coroutine _glowCoroutine;
    private bool _glowActive;
    // 스테이지 진행상 현재 목표로 지정된 경우에만 포커스 발광을 허용합니다.
    private bool _objectiveHighlightArmed;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        EnsureGlowBlock();
    }

    private void Start()
    {
        EnsureGlowBlock();
        CacheGlowMaterials();
        // 발광은 상호작용 대상이 실제로 포커스된 동안에만 켭니다.
        // isInteractable만으로 시작 시 발광하지 않습니다.
    }

    private void Update()
    {
        // 상호작용 가능 여부만으로는 발광하지 않습니다.
        // 스테이지 진행 스크립트가 현재 목표로 지정했고, 플레이어가 실제로 바라볼 때만 발광합니다.
        bool shouldGlow = _objectiveHighlightArmed && isInteractable;

        if (shouldGlow && !_glowActive)
            StartMeshGlow();
        else if (!shouldGlow && _glowActive)
            StopMeshGlow();
    }

    private void OnDisable()
    {
        StopMeshGlow();
    }

    public virtual void OnFocusEnter()
    {
        IsFocused = true;
        if (_objectiveHighlightArmed && isInteractable)
            StartMeshGlow();
    }

    public virtual void OnFocusExit()
    {
        IsFocused = false;
        // 목표 단계가 계속되는 동안에는 시선을 돌려도 발광을 유지합니다.
        // 목표 종료/상호작용 완료는 DisableObjectiveHighlight 또는 OnInteract에서 처리합니다.
    }
    public virtual void OnReadyStateReached() { }

    public virtual void OnInteract()
    {
        if (!isInteractable) return;
        if (interactOnlyOnce && _hasInteracted) return;
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()) return;

        _hasInteracted = true;
        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.15f);

        StopMeshGlow();
        _objectiveHighlightArmed = false;
        DisableChildPulseLights();

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
        _objectiveHighlightArmed = false;
        // 단순 재활성화는 목표 지정이 아니므로 발광하지 않습니다.
        StopMeshGlow();
    }

    public void EnableInteractionWithLight()
    {
        _hasInteracted = false;
        isInteractable = true;
        // 이 메서드는 스테이지 진행상 지금 상호작용해야 하는 목표를 지정할 때 호출합니다.
        _objectiveHighlightArmed = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            if (col != null) col.enabled = true;

        // 주변을 비추는 Point Light는 사용하지 않고, 상호작용 대상의 메시 자체를 발광시킵니다.
        DisableChildPulseLights();
        StopMeshGlow();

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
    }

    /// <summary>
    /// 현재 단계의 목표에서 제외합니다. 이후 포커스되어도 발광하지 않습니다.
    /// 상호작용 자체를 비활성화하지는 않으므로 필요하면 isInteractable을 별도로 설정하세요.
    /// </summary>
    public void DisableObjectiveHighlight()
    {
        _objectiveHighlightArmed = false;
        StopMeshGlow();
    }

    private void DisableChildPulseLights()
    {
        PulseLight[] pulseLights = GetComponentsInChildren<PulseLight>(true);
        foreach (PulseLight pulse in pulseLights)
            if (pulse != null) pulse.StopPulse();
    }

    private void EnsureGlowBlock()
    {
        if (_glowBlock == null)
            _glowBlock = new MaterialPropertyBlock();
    }

    private void CacheGlowMaterials()
    {
        EnsureGlowBlock();
        _glowMaterials.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);

        foreach (Renderer targetRenderer in renderers)
        {
            if (targetRenderer == null) continue;
            Material[] materials = targetRenderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty(EmissionColorId)) continue;

                if (!material.IsKeywordEnabled("_EMISSION"))
                    material.EnableKeyword("_EMISSION");

                Color baseColor = material.GetColor(EmissionColorId);
                _glowMaterials.Add(new GlowMaterial
                {
                    renderer = targetRenderer,
                    materialIndex = i,
                    originalEmission = baseColor
                });
            }
        }
    }

    private void StartMeshGlow()
    {
        EnsureGlowBlock();
        if (_glowActive) return;
        if (_glowMaterials.Count == 0) CacheGlowMaterials();

        _glowActive = true;
        if (_glowCoroutine == null && _glowMaterials.Count > 0)
            _glowCoroutine = StartCoroutine(MeshGlowRoutine());
    }

    private IEnumerator MeshGlowRoutine()
    {
        // StartCoroutine 호출 전에 초기화되지만, 런타임 재호출에도 안전하도록 방어합니다.
        EnsureGlowBlock();
        if (_glowBlock == null)
        {
            _glowCoroutine = null;
            yield break;
        }

        while (_glowActive)
        {
            float t = Mathf.PingPong(Time.time * glowPulseSpeed, 1f);
            float intensity = Mathf.Lerp(glowMinIntensity, glowMaxIntensity, t);
            Color emission = interactionGlowColor * intensity;

            for (int i = 0; i < _glowMaterials.Count; i++)
            {
                GlowMaterial entry = _glowMaterials[i];
                if (entry.renderer == null) continue;

                entry.renderer.GetPropertyBlock(_glowBlock, entry.materialIndex);
                _glowBlock.SetColor(EmissionColorId, emission);
                entry.renderer.SetPropertyBlock(_glowBlock, entry.materialIndex);
            }

            yield return null;
        }

        _glowCoroutine = null;
    }

    private void StopMeshGlow()
    {
        _glowActive = false;
        if (_glowCoroutine != null)
        {
            StopCoroutine(_glowCoroutine);
            _glowCoroutine = null;
        }

        // Awake 이전/비활성 상태 등에서 호출되더라도 null PropertyBlock을 Unity API에 전달하지 않습니다.
        if (_glowBlock == null) return;

        for (int i = 0; i < _glowMaterials.Count; i++)
        {
            GlowMaterial entry = _glowMaterials[i];
            if (entry.renderer == null) continue;

            entry.renderer.GetPropertyBlock(_glowBlock, entry.materialIndex);
            _glowBlock.SetColor(EmissionColorId, entry.originalEmission);
            entry.renderer.SetPropertyBlock(_glowBlock, entry.materialIndex);
        }
    }
}

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
    public bool interactOnlyOnce = true;
    public bool destroyOnPickup = false;

    [Header("상호작용 거리 설정")]
    [Tooltip("활성화하면 플레이어와의 거리 제한을 무시하고 화면 중앙 조준으로만 상호작용합니다. 엘리베이터 버튼 전용으로 사용합니다.")]
    public bool ignorePlayerDistanceLimit = false;

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
        public Material material;
        public Color originalEmission;
        public bool originalEmissionKeywordEnabled;
    }

    private readonly List<GlowMaterial> _glowMaterials = new List<GlowMaterial>();
    // UnityEngine.Object 기반 객체는 MonoBehaviour 필드 초기화/생성자에서 만들면 안 됩니다.
    // Awake에서 런타임 초기화합니다.
    private MaterialPropertyBlock _glowBlock;
    private Coroutine _glowCoroutine;
    private bool _glowActive;
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    // 상호작용 가능 여부와 강조 발광 여부는 별개의 상태입니다.
    // 문처럼 조작은 가능하지만 빛나면 안 되는 오브젝트는 파생 클래스에서 false로 지정합니다.
    protected virtual bool CanShowInteractionGlow => true;

    private void Awake()
    {
        EnsureGlowBlock();
    }

    private void Start()
    {
        EnsureGlowBlock();
        if (CanShowInteractionGlow && isInteractable) StartMeshGlow();
        else StopMeshGlow();
    }

    private void Update()
    {
        // 상호작용 상태와 발광 허용 상태를 모두 확인합니다.
        if (CanShowInteractionGlow && isInteractable && !_glowActive)
            StartMeshGlow();
        else if ((!CanShowInteractionGlow || !isInteractable) && _glowActive)
            StopMeshGlow();
    }

    private void OnDisable()
    {
        StopMeshGlow();
    }

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

        StopMeshGlow();
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
        StartMeshGlow();
    }

    /// <summary>상호작용 대상의 메시 발광과 자식 PulseLight 강조를 즉시 종료합니다.</summary>
    public void DisableInteractionGlow()
    {
        StopMeshGlow();
        DisableChildPulseLights();
    }

    public void EnableInteractionWithLight()
    {
        _hasInteracted = false;
        isInteractable = true;

        Collider[] colliders = GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
            if (col != null) col.enabled = true;

        // 주변을 비추는 Point Light는 사용하지 않고, 상호작용 대상의 메시 자체를 발광시킵니다.
        DisableChildPulseLights();
        StartMeshGlow();

        Animator anim = GetComponent<Animator>();
        if (anim != null) anim.enabled = false;
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
            // materials는 렌더러별 인스턴스를 반환하므로 한 오브젝트의 발광 설정이
            // 같은 Shared Material을 쓰는 다른 오브젝트에 번지지 않습니다.
            Material[] materials = targetRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null || !material.HasProperty(EmissionColorId)) continue;

                _glowMaterials.Add(new GlowMaterial
                {
                    renderer = targetRenderer,
                    materialIndex = i,
                    material = material,
                    originalEmission = material.GetColor(EmissionColorId),
                    originalEmissionKeywordEnabled = material.IsKeywordEnabled("_EMISSION")
                });
            }
        }
    }

    private void StartMeshGlow()
    {
        if (!CanShowInteractionGlow)
        {
            StopMeshGlow();
            return;
        }

        EnsureGlowBlock();
        if (_glowActive) return;
        if (_glowMaterials.Count == 0) CacheGlowMaterials();

        _glowActive = true;
        for (int i = 0; i < _glowMaterials.Count; i++)
        {
            Material material = _glowMaterials[i].material;
            if (material != null) material.EnableKeyword("_EMISSION");
        }

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

            // 강조 표시가 끝나면 원래 Emission Keyword 상태까지 되돌립니다.
            if (entry.material != null && !entry.originalEmissionKeywordEnabled)
                entry.material.DisableKeyword("_EMISSION");
        }
    }
}

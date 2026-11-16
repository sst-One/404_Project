using System.Collections.Generic;
using UnityEngine;

internal static class StageVisuals
{
    private static readonly HashSet<string> EffectMeshes = new HashSet<string>
    {
        "Cube.004", "Cube.012", "Cube.015", "Cube.016", "Cube.017", "Object_0.021",
        "Object_0.032", "Object_0.033", "Object_0.034", "Object_0.035", "Object_1.011", "Object_1.019"
    };

    private static readonly HashSet<string> EffectMaterials = new HashSet<string>
    {
        "Material.062", "Material.063", "Material.064", "Material.075", "Material.080", "Material.085",
        "Material.086", "Material.309", "blood1.001", "blood1.002", "blood2.002", "blood2.003",
        "material_0.005", "material_0.006", "material_0.007"
    };

    internal static void ReplaceGem(InteractableItem item)
    {
        GameObject prefab = Resources.Load<GameObject>("StageVisuals/Gem");
        if (item == null || prefab == null) return;
        Renderer[] oldRenderers = item.GetComponentsInChildren<Renderer>(true);
        if (!TryGetBounds(oldRenderers, item.transform, out Bounds target)) return;
        GameObject model = Object.Instantiate(prefab, item.transform, false);
        if (!TryGetBounds(model.GetComponentsInChildren<Renderer>(true), item.transform, out Bounds source))
        {
            Object.Destroy(model);
            return;
        }
        float sourceSize = Mathf.Max(source.size.x, source.size.y, source.size.z);
        if (sourceSize <= 0.0001f)
        {
            Object.Destroy(model);
            return;
        }
        float scale = Mathf.Max(target.size.x, target.size.y, target.size.z) / sourceSize;
        ScaleToBounds(model.transform, source, target, Vector3.one * scale);
        foreach (Renderer renderer in oldRenderers) renderer.enabled = false;
        foreach (Transform child in model.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = item.gameObject.layer;
        item.InvalidateGlowRenderers();
    }

    internal static GameObject CreateHallucination(Transform room)
    {
        GameObject prefab = Resources.Load<GameObject>("StageVisuals/Hallucination");
        if (room == null || prefab == null || !TryGetBounds(room.GetComponentsInChildren<Renderer>(true), room, out Bounds target))
            return null;

        GameObject model = Object.Instantiate(prefab, room, false);
        model.SetActive(false);
        var shared = new List<Renderer>();
        bool hasEffects = false;
        foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
        {
            bool effect = renderer.TryGetComponent(out MeshFilter filter) &&
                filter.sharedMesh != null && EffectMeshes.Contains(filter.sharedMesh.name);
            if (!effect)
                foreach (Material material in renderer.sharedMaterials)
                    if (material != null && EffectMaterials.Contains(material.name)) { effect = true; break; }
            renderer.enabled = effect;
            if (effect) hasEffects = true;
            else shared.Add(renderer);
        }
        if (!hasEffects || !TryGetBounds(shared, room, out Bounds source) ||
            source.size.x < 0.0001f || source.size.y < 0.0001f || source.size.z < 0.0001f)
        {
            Object.Destroy(model);
            return null;
        }
        ScaleToBounds(model.transform, source, target,
            new Vector3(target.size.x / source.size.x, target.size.y / source.size.y, target.size.z / source.size.z));
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
        return model;
    }

    private static void ScaleToBounds(Transform model, Bounds source, Bounds target, Vector3 scale)
    {
        Vector3 offset = model.localPosition - source.center;
        model.localScale = Vector3.Scale(model.localScale, scale);
        model.localPosition = target.center + Vector3.Scale(offset, scale);
    }

    private static bool TryGetBounds(IEnumerable<Renderer> renderers, Transform frame, out Bounds result)
    {
        result = default;
        bool found = false;
        foreach (Renderer renderer in renderers)
        {
            Bounds bounds;
            if (renderer is SkinnedMeshRenderer skin) bounds = skin.localBounds;
            else if (renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null) bounds = filter.sharedMesh.bounds;
            else continue;
            Matrix4x4 matrix = frame.worldToLocalMatrix * renderer.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 point = matrix.MultiplyPoint3x4(new Vector3(
                    (corner & 1) == 0 ? bounds.min.x : bounds.max.x,
                    (corner & 2) == 0 ? bounds.min.y : bounds.max.y,
                    (corner & 4) == 0 ? bounds.min.z : bounds.max.z));
                if (found) result.Encapsulate(point);
                else { result = new Bounds(point, Vector3.zero); found = true; }
            }
        }
        return found;
    }
}

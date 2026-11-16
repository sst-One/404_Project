using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LineRenderer))]
public class TutorialFloorRoute : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform hidingSpot;
    [Header("Appearance")]
    [SerializeField, Min(0.005f)] private float lineWidth = 0.07f;
    [SerializeField] private float floorOffset = 0.06f;
    [SerializeField] private Color routeColor = Color.white;
    [Header("Update")]
    [SerializeField, Min(0.05f)] private float updateInterval = 0.25f;

    private LineRenderer line;
    private NavMeshPath path;
    private float timer;
    private Material _routeMaterial;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        path = new NavMeshPath();
        line.useWorldSpace = true;
        line.alignment = LineAlignment.View;
        line.textureMode = LineTextureMode.Stretch;
        line.numCornerVertices = 4;
        line.numCapVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;
        line.sortingOrder = 20;
        line.startWidth = Mathf.Clamp(lineWidth, 0.035f, 0.065f);
        line.endWidth = line.startWidth * 0.8f;
        line.startColor = new Color(routeColor.r, routeColor.g, routeColor.b, 0.8f);
        line.endColor = new Color(routeColor.r, routeColor.g, routeColor.b, 1f);
        line.positionCount = 0;
        line.enabled = false;
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader != null)
        {
            _routeMaterial = new Material(shader);
            if (_routeMaterial.HasProperty("_Color")) _routeMaterial.SetColor("_Color", Color.white);
            line.sharedMaterial = _routeMaterial;
        }
    }

    private void OnDestroy()
    {
        if (_routeMaterial != null) Destroy(_routeMaterial);
    }

    private void Update()
    {
        if (!line.enabled) return;
        timer += Time.deltaTime;
        if (timer >= updateInterval) { timer = 0f; RefreshRoute(); }
    }

    public void SetTargets(Transform playerTransform, Transform hidingSpotTransform)
    {
        player = playerTransform;
        hidingSpot = hidingSpotTransform;
        if (line != null && line.enabled) RefreshRoute();
    }

    public void ShowRoute()
    {
        if (player == null && PlayerController.Instance != null) player = PlayerController.Instance.transform;
        timer = updateInterval;
        line.enabled = true;
        RefreshRoute();
    }

    public void HideRoute()
    {
        if (line == null) return;
        line.enabled = false;
        line.positionCount = 0;
    }

    private void RefreshRoute()
    {
        if (line == null || !line.enabled) return;
        if (player == null && PlayerController.Instance != null) player = PlayerController.Instance.transform;
        if (player == null || hidingSpot == null) { line.positionCount = 0; return; }
        NavMeshHit startHit, endHit;
        bool startOnMesh = NavMesh.SamplePosition(player.position, out startHit, 2f, NavMesh.AllAreas);
        bool endOnMesh = NavMesh.SamplePosition(hidingSpot.position, out endHit, 2f, NavMesh.AllAreas);
        if (startOnMesh && endOnMesh && NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path) &&
            path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
        {
            Vector3[] corners = path.corners;
            if (corners.Length > 1)
                corners[0] = Vector3.MoveTowards(corners[0], corners[1], 0.20f);
            line.positionCount = corners.Length;
            for (int i = 0; i < corners.Length; i++)
            {
                line.SetPosition(i, OnFloor(corners[i], LayerMask.GetMask("Walkable")));
            }
        }
        else
        {
            DrawVisibleFloorSegment();
        }
    }

    private void DrawVisibleFloorSegment()
    {
        int walkable = LayerMask.GetMask("Walkable");
        int obstacles = LayerMask.GetMask("Obstacle");
        Vector3 start = Vector3.MoveTowards(player.position, hidingSpot.position, 0.20f);
        Vector3 end = hidingSpot.position;
        const int samples = 12;
        Vector3 previous = default;
        int count = 0;
        line.positionCount = samples;
        for (int i = 0; i < samples; i++)
        {
            Vector3 probe = Vector3.Lerp(start, end, i / (float)(samples - 1));
            Vector3 position = OnFloor(probe, walkable);
            if (count >= 2 && Physics.Linecast(previous + Vector3.up * 0.12f, position + Vector3.up * 0.12f, obstacles, QueryTriggerInteraction.Ignore))
                break;
            line.SetPosition(count++, position);
            previous = position;
        }
        line.positionCount = count >= 2 ? count : 0;
    }

    private Vector3 OnFloor(Vector3 probe, int walkable)
    {
        if (Physics.Raycast(probe + Vector3.up * 0.35f, Vector3.down, out RaycastHit floor, 0.85f, walkable, QueryTriggerInteraction.Ignore))
            probe = floor.point;
        return probe + Vector3.up * Mathf.Clamp(floorOffset, 0.025f, 0.065f);
    }
}

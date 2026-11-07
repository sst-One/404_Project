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

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        path = new NavMeshPath();
        line.useWorldSpace = true;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;
        line.startColor = routeColor;
        line.endColor = routeColor;
        line.positionCount = 0;
        line.enabled = false;
        if (line.sharedMaterial == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null) line.material = new Material(shader);
        }
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
        if (player == null || hidingSpot == null) { line.positionCount = 0; return; }
        NavMeshHit startHit, endHit;
        bool startOnMesh = NavMesh.SamplePosition(player.position, out startHit, 2f, NavMesh.AllAreas);
        bool endOnMesh = NavMesh.SamplePosition(hidingSpot.position, out endHit, 2f, NavMesh.AllAreas);
        if (startOnMesh && endOnMesh && NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path) &&
            path.status == NavMeshPathStatus.PathComplete && path.corners.Length > 1)
        {
            line.positionCount = path.corners.Length;
            for (int i = 0; i < path.corners.Length; i++)
            {
                Vector3 p = path.corners[i]; p.y += floorOffset; line.SetPosition(i, p);
            }
        }
        else
        {
            Vector3 a = player.position; Vector3 b = hidingSpot.position;
            a.y += floorOffset; b.y += floorOffset;
            line.positionCount = 2; line.SetPosition(0, a); line.SetPosition(1, b);
        }
    }
}

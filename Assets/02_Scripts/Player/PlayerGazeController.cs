using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerGazeController : MonoBehaviour
{
    [Header("Dependencies")]
    public Camera mainCamera;

    [Header("Raycast Settings")]
    public float maxGazeDistance = 1.5f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Gaze Rules (PARAM-002)")]
    public float readyTimeThreshold = 0.5f;

    [HideInInspector] public bool IsFloorValid;
    [HideInInspector] public Vector3 CurrentFloorHitPoint;
    [HideInInspector] public Transform CurrentHoverTarget;
    [HideInInspector] public bool IsTargetReady;

    [HideInInspector] public Vector3 RayOrigin;
    [HideInInspector] public Vector3 RayEndPoint;

    private PlayerInputProvider inputProvider;
    private float currentReadyTimer = 0f;

    private void Awake()
    {
        inputProvider = GetComponent<PlayerInputProvider>();
    }

    private void Update()
    {
        if (mainCamera == null) return;
        ProcessGazeRaycast();
    }

    private void ProcessGazeRaycast()
    {
        IsFloorValid = false;

        // 핫픽스: UI가 화면을 가리고 있을 때는 Gaze 레이캐스트 연산을 중단하고 타겟 초기화
        if (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking())
        {
            ResetObjectTarget();
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(inputProvider.GazeScreenPosition);
        RayEndPoint = ray.origin + ray.direction * maxGazeDistance;

        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxGazeDistance, targetMask | obstacleMask))
        {
            RayEndPoint = hit.point;

            if ((obstacleMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                ResetObjectTarget();
                return;
            }

            int walkableLayer = LayerMask.NameToLayer("Walkable");
            int hidingSpotLayer = LayerMask.NameToLayer("HidingSpot");

            if (hit.transform.gameObject.layer == walkableLayer || hit.transform.gameObject.layer == hidingSpotLayer)
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                {
                    CurrentFloorHitPoint = navHit.position;
                    IsFloorValid = true;
                }
            }
            else if ((targetMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                if (CurrentHoverTarget == hit.transform)
                {
                    if (!IsTargetReady)
                    {
                        currentReadyTimer += Time.deltaTime;
                        if (currentReadyTimer >= readyTimeThreshold) IsTargetReady = true;
                    }
                }
                else
                {
                    CurrentHoverTarget = hit.transform;
                    currentReadyTimer = 0f;
                    IsTargetReady = false;
                }
            }
        }
        else
        {
            ResetObjectTarget();
        }
    }

    private void ResetObjectTarget()
    {
        CurrentHoverTarget = null;
        currentReadyTimer = 0f;
        IsTargetReady = false;
        IsFloorValid = false;
    }
}
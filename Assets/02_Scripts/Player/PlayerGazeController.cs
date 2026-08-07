using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerGazeController : MonoBehaviour
{
    [Header("Dependencies")]
    public Camera mainCamera;

    [Header("Raycast Settings")]
    public float maxGazeDistance = 5.0f;

    [Tooltip("시선 판정의 두께. 너무 두꺼우면 엉뚱한 곳이 잡힙니다. (기존 0.3 -> 0.05 대폭 하향)")]
    public float gazeRadius = 0.05f;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Gaze Rules (PARAM-002)")]
    public float readyTimeThreshold = 0.5f;

    [HideInInspector] public bool IsFloorValid;
    [HideInInspector] public Vector3 CurrentFloorHitPoint;
    [HideInInspector] public Transform CurrentHoverTarget;
    [HideInInspector] public bool IsTargetReady;
    [HideInInspector] public Vector3 RayEndPoint;

    private PlayerInputProvider inputProvider;
    private float currentReadyTimer = 0f;

    private void Awake()
    {
        inputProvider = GetComponent<PlayerInputProvider>();
    }

    private void Update()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (SystemMenuController.Instance != null && SystemMenuController.Instance.IsPaused)
        {
            ResetObjectTarget();
            return;
        }

        ProcessGazeRaycast();
    }

    private void ProcessGazeRaycast()
    {
        IsFloorValid = false;

        Ray ray = mainCamera.ScreenPointToRay(inputProvider.GazeScreenPosition);
        RayEndPoint = ray.origin + ray.direction * maxGazeDistance;

        RaycastHit hit;
        if (Physics.SphereCast(ray, gazeRadius, out hit, maxGazeDistance, targetMask | obstacleMask))
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
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerGazeController : MonoBehaviour
{
    [Header("Dependencies")]
    public Camera mainCamera;

    [Header("Raycast Settings")]
    public float maxGazeDistance = 15f;
    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [Header("Gaze Rules (PARAM-002)")]
    public float readyTimeThreshold = 0.5f;

    [HideInInspector] public bool IsFloorValid;
    [HideInInspector] public Vector3 CurrentFloorHitPoint;
    [HideInInspector] public Transform CurrentHoverTarget;
    [HideInInspector] public bool IsTargetReady;

    // UI 인디케이터(선) 렌더링을 위한 좌표 데이터
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

            // [수정] 이동 가능 지점(Walkable) 또는 은신처(HidingSpot) 판정
            int walkableLayer = LayerMask.NameToLayer("Walkable");
            int hidingSpotLayer = LayerMask.NameToLayer("HidingSpot");

            if (hit.transform.gameObject.layer == walkableLayer || hit.transform.gameObject.layer == hidingSpotLayer)
            {
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 3.0f, NavMesh.AllAreas))
                {
                    CurrentFloorHitPoint = navHit.position;
                    IsFloorValid = true; // 이제 두 레이어 모두 이동 가능 지점으로 판정됨
                }
            }
            else if ((targetMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                // 상호작용 오브젝트 로직 유지
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
        IsFloorValid = false; // [수정] 포커스 리셋 시 이동 불가 처리
    }
}
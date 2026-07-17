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
        RayOrigin = ray.origin - new Vector3(0, 0.2f, 0); // 화면 중앙(눈)보다 살짝 아래(어깨/손)에서 선이 나가도록 보정
        RayEndPoint = ray.origin + ray.direction * maxGazeDistance; // 기본값은 최대 사거리 (허공)

        RaycastHit hit;
        // targetMask와 obstacleMask를 합쳐서 쏴야 모든 지형지물에 선이 막힙니다.
        if (Physics.Raycast(ray, out hit, maxGazeDistance, targetMask | obstacleMask))
        {
            RayEndPoint = hit.point; // 물체에 닿은 실제 좌표로 끝점 갱신

            if ((obstacleMask & (1 << hit.transform.gameObject.layer)) != 0)
            {
                ResetObjectTarget();
                return;
            }

            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Floor"))
            {
                ResetObjectTarget();
                if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
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
    }
}
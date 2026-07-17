using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 역할: 플레이어의 시선(Raycast) 판정 및 0.5초 응시(Ready) 상태를 계산하는 모듈.
/// 결과값은 UI 컨트롤러와 이동/상호작용 매니저가 가져다 사용합니다.
/// </summary>
[RequireComponent(typeof(PlayerInputProvider))]
public class PlayerGazeController : MonoBehaviour
{
    [Header("Dependencies")]
    public Camera mainCamera;

    [Header("Raycast Settings")]
    public float maxGazeDistance = 15f;
    public LayerMask targetMask;   // Interactable, Floor, Threat 등 선택
    public LayerMask obstacleMask; // 시야를 가리는 Obstacle 선택

    [Header("Gaze Rules (PARAM-002)")]
    public float readyTimeThreshold = 0.5f;

    // --- [외부에서 읽어갈 결과 데이터 (Read Only)] ---

    [HideInInspector] public bool IsFloorValid;          // 바닥(NavMesh)을 바라보고 있는가?
    [HideInInspector] public Vector3 CurrentFloorHitPoint; // 이동 가능한 바닥의 최종 좌표

    [HideInInspector] public Transform CurrentHoverTarget; // 현재 응시 중인 오브젝트 (Interactable/Threat)
    [HideInInspector] public bool IsTargetReady;           // 해당 오브젝트를 0.5초 이상 응시했는가?

    // -------------------------------------------------

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
        // 1. 매 프레임 상태 초기화 (Raycast가 빗나갈 경우를 대비)
        IsFloorValid = false;

        // 입력부에서 마우스 스크린 좌표 가져오기
        Ray ray = mainCamera.ScreenPointToRay(inputProvider.GazeScreenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, maxGazeDistance, targetMask))
        {
            Vector3 dirToTarget = hit.point - mainCamera.transform.position;

            // 2. 장애물(Obstacle)에 가려졌는지 검증
            if (!Physics.Raycast(mainCamera.transform.position, dirToTarget.normalized, dirToTarget.magnitude, obstacleMask))
            {
                // 3. 타겟이 바닥(Floor)인 경우: NavMesh 검증
                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Floor"))
                {
                    ResetObjectTarget(); // 바닥을 볼 땐 오브젝트 타겟팅 해제

                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
                    {
                        CurrentFloorHitPoint = navHit.position;
                        IsFloorValid = true;
                    }
                }
                // 4. 타겟이 오브젝트(Interactable, Threat 등)인 경우: Ready 타이머 계산
                else
                {
                    if (CurrentHoverTarget == hit.transform)
                    {
                        // 계속 같은 오브젝트를 보고 있다면 타이머 증가
                        if (!IsTargetReady)
                        {
                            currentReadyTimer += Time.deltaTime;
                            if (currentReadyTimer >= readyTimeThreshold)
                            {
                                IsTargetReady = true;
                            }
                        }
                    }
                    else
                    {
                        // 새로운 오브젝트를 보게 되면 타이머 리셋
                        CurrentHoverTarget = hit.transform;
                        currentReadyTimer = 0f;
                        IsTargetReady = false;
                    }
                }
            }
            else
            {
                ResetObjectTarget(); // 장애물에 가려짐
            }
        }
        else
        {
            ResetObjectTarget(); // 아무것도 안 봄
        }
    }

    private void ResetObjectTarget()
    {
        CurrentHoverTarget = null;
        currentReadyTimer = 0f;
        IsTargetReady = false;
    }
}
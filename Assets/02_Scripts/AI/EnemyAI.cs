using System.Collections;
using UnityEngine;
using UnityEngine.AI;

// [수정 1] Suspect(의심) 상태 신규 추가
public enum EnemyState { Patrol, Suspect, Investigate, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Patrol;
    public Transform[] patrolPoints;

    [Header("FOV Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float fovTickRate = 0.2f;

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _fovCoroutine;
    private Coroutine _suspectCoroutine; // 의심 상태 제어용 코루틴

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>(); // 컴포넌트 재확인

        if (Camera.main != null)
        {
            _playerTransform = Camera.main.transform.root;
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnNoiseLevelChanged += HandleNoiseLevel;
        }

        // ====================================================================
        // [TPM 핫픽스] 런타임 자동 안착(Warp) 시스템 (Boundary Lock 방지)
        // ====================================================================
        // 적 AI의 현재 위치 기준 반경 5m 이내에서 가장 가까운 파란색 NavMesh 영역을 찾습니다.
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            // 에이전트를 해당 위치로 강제 순간이동 시켜 안착시킵니다.
            _agent.Warp(hit.position);
            Debug.Log($"[EnemyAI] 적 AI가 내비메쉬 유효 영역({hit.position})에 안전하게 강제 안착되었습니다.");
        }
        else
        {
            Debug.LogError("[EnemyAI] 치명적 오류: 적 주변 5미터 이내에 파란색 내비메쉬 길이 없습니다! 에디터 배치를 확인하세요.");
        }

        // 안착이 완료된 후 순찰 및 시야 감지 가동
        MoveToNextPatrolPoint();
        _fovCoroutine = StartCoroutine(FOVRoutine());
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnNoiseLevelChanged -= HandleNoiseLevel;

            if (currentState == EnemyState.Chase)
            {
                StateManager.Instance.RemoveThreat();
            }
        }
        if (_fovCoroutine != null) StopCoroutine(_fovCoroutine);
        if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
    }

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                // [TPM 핫픽스] 에이전트가 NavMesh 위에 정상적으로 안착했을 때만 거리 계산 수행
                if (_agent.isOnNavMesh)
                {
                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        MoveToNextPatrolPoint();
                    }
                }
                break;

            case EnemyState.Suspect:
                // SuspectRoutine 코루틴에서 제어하므로 Update에선 대기
                break;

            case EnemyState.Investigate:
                if (_agent.isOnNavMesh)
                {
                    if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                    {
                        Debug.Log("[EnemyAI] 수색 완료. 단서를 찾지 못해 순찰로 복귀합니다.");
                        ChangeState(EnemyState.Patrol);
                        MoveToNextPatrolPoint();
                    }
                }
                break;

            case EnemyState.Chase:
                if (_playerTransform != null && _agent.isOnNavMesh)
                {
                    _agent.SetDestination(_playerTransform.position);
                }
                break;
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        if (currentState == EnemyState.Chase && StateManager.Instance != null)
        {
            StateManager.Instance.RemoveThreat();
        }

        if (newState == EnemyState.Chase && StateManager.Instance != null)
        {
            StateManager.Instance.AddThreat();
        }

        currentState = newState;
    }

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(fovTickRate);
        while (true)
        {
            yield return wait;
            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets()
    {
        bool canSeePlayer = false;
        float currentViewRadius = viewRadius;
        bool isPlayerFreezing = false;

        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            isPlayerFreezing = true;
            currentViewRadius = viewRadius * 0.3f;
        }

        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, currentViewRadius, playerMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    canSeePlayer = true;
                    _lastKnownPlayerPosition = target.position;
                }
            }
        }

        if (canSeePlayer)
        {
            if (currentState != EnemyState.Chase)
            {
                Debug.Log($"[EnemyAI] 플레이어 발각! (Freeze: {isPlayerFreezing}) - 추격 시작!");
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                ChangeState(EnemyState.Chase);
            }
        }
        else
        {
            if (currentState == EnemyState.Chase)
            {
                Debug.Log("[EnemyAI] 시야 상실: 마지막 목격 지점 탐색으로 전환합니다.");
                ChangeState(EnemyState.Investigate);
                if (_agent.isOnNavMesh) _agent.SetDestination(_lastKnownPlayerPosition);
            }
        }
    }

    // [수정 2] 소음 이벤트 핸들러 (위치 기반 지능적 수색)
    private void HandleNoiseLevel(NoiseLevel level, Vector3 noisePosition)
    {
        // 추격(Chase) 중일 때는 시야와 목표물이 최우선이므로 소음에 다른 반응을 하지 않음
        if (currentState == EnemyState.Chase) return;

        switch (level)
        {
            case NoiseLevel.Low:
                // 무시 (순찰 유지)
                break;

            case NoiseLevel.Mid:
                // 순찰이나 수색 중일 때 제자리에 멈춰서 의심(Suspect)
                if (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate)
                {
                    Debug.Log("[EnemyAI] 중간 소음 감지: 제자리에 멈춰 주변을 살핍니다.");
                    if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                    _suspectCoroutine = StartCoroutine(SuspectRoutine());
                }
                break;

            case NoiseLevel.High:
                // 소음이 발생한 '해당 좌표'로 수색(Investigate) 이동
                Debug.Log($"[EnemyAI] 큰 소음 감지: 진원지({noisePosition})로 수색을 시작합니다.");
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                if (_agent.isOnNavMesh) _agent.isStopped = false;

                ChangeState(EnemyState.Investigate);
                if (_agent.isOnNavMesh) _agent.SetDestination(noisePosition);
                break;

            case NoiseLevel.Critical:
                // 즉시 발각 및 추격(Chase) 시작
                Debug.Log("[EnemyAI] 치명적 소음 감지: 즉시 추격을 시작합니다!");
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                if (_agent.isOnNavMesh) _agent.isStopped = false;

                _lastKnownPlayerPosition = noisePosition; // 소음원 기준점 저장
                ChangeState(EnemyState.Chase);
                break;
        }
    }

    private IEnumerator SuspectRoutine()
    {
        ChangeState(EnemyState.Suspect);
        if (_agent.isOnNavMesh) _agent.isStopped = true; // 이동 강제 정지

        // 3초간 두리번거림 대기 (향후 애니메이션 연결부)
        yield return new WaitForSeconds(3f);

        // 의심이 풀리면 다시 순찰로 복귀
        if (currentState == EnemyState.Suspect)
        {
            Debug.Log("[EnemyAI] 이상 없음: 순찰로 복귀합니다.");
            if (_agent.isOnNavMesh) _agent.isStopped = false;
            ChangeState(EnemyState.Patrol);
            MoveToNextPatrolPoint();
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        }
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }
}
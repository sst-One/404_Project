using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Patrol, Investigate, Chase }

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

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            _playerTransform = Camera.main.transform;
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnNoiseLevelChanged += HandleNoiseLevel;
        }

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
        if (_fovCoroutine != null)
        {
            StopCoroutine(_fovCoroutine);
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Patrol:
                if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                {
                    MoveToNextPatrolPoint();
                }
                break;

            case EnemyState.Investigate:
                if (!_agent.pathPending && _agent.remainingDistance < 0.5f)
                {
                    Debug.Log("탐색 완료. 순찰로 복귀합니다.");
                    ChangeState(EnemyState.Patrol);
                    MoveToNextPatrolPoint();
                }
                break;

            case EnemyState.Chase:
                if (_playerTransform != null)
                {
                    _agent.SetDestination(_playerTransform.position);
                }
                break;
        }
    }

    // 신규 추가: 중앙 통제형 상태 변경 함수 (위협 카운트 누수 방지)
    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        // 기존 상태가 Chase였다면 벗어날 때 무조건 위협 해제
        if (currentState == EnemyState.Chase)
        {
            if (StateManager.Instance != null)
            {
                StateManager.Instance.RemoveThreat();
            }
        }

        // 새로운 상태가 Chase라면 무조건 위협 추가
        if (newState == EnemyState.Chase)
        {
            if (StateManager.Instance != null)
            {
                StateManager.Instance.AddThreat();
            }
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

        // [핵심 연동] SYS-005 & FEAT-007: Freeze 상태에 따른 시각적 감지 반경 동적 축소
        float currentViewRadius = viewRadius;
        bool isPlayerFreezing = false;

        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            isPlayerFreezing = true;
            currentViewRadius = viewRadius * 0.3f; // Freeze 성공 시 감지 반경 70% 감소 (근접이 아니면 발각 불가)
        }

        // 축소된 반경(currentViewRadius)을 기준으로 플레이어 탐색
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, currentViewRadius, playerMask);

        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - transform.position).normalized;

            // 시야각(FOV) 내에 존재하는지 확인
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, target.position);
                // 장애물에 가려져 있는지 레이캐스트로 확인
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
                Debug.Log($"[EnemyAI] 플레이어 발각! (Freeze 상태: {isPlayerFreezing}) - 추격을 시작합니다!");
                ChangeState(EnemyState.Chase);
            }
        }
        else
        {
            if (currentState == EnemyState.Chase)
            {
                Debug.Log("[EnemyAI] 시야 상실: 마지막 목격 지점 탐색으로 전환합니다.");
                ChangeState(EnemyState.Investigate);

                if (_agent.isOnNavMesh)
                {
                    _agent.SetDestination(_lastKnownPlayerPosition);
                }
            }
        }
    }

    private void HandleNoiseLevel(NoiseLevel level)
    {
        if (level == NoiseLevel.High || level == NoiseLevel.Critical)
        {
            if (currentState != EnemyState.Chase)
            {
                ChangeState(EnemyState.Investigate);
                if (_playerTransform != null)
                {
                    _agent.SetDestination(_playerTransform.position);
                    Debug.Log("큰 소음 감지: 해당 위치로 탐색 이동합니다.");
                }
            }
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }
}
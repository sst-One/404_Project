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
                    currentState = EnemyState.Patrol;
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
        Collider[] targetsInViewRadius = Physics.OverlapSphere(transform.position, viewRadius, playerMask);

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
                Debug.Log("플레이어 시야 확보: 추격 시작!");
                currentState = EnemyState.Chase;
            }
        }
        else
        {
            if (currentState == EnemyState.Chase)
            {
                Debug.Log("시야 상실: 마지막 목격 지점 탐색으로 전환합니다.");
                currentState = EnemyState.Investigate;
                _agent.SetDestination(_lastKnownPlayerPosition);
            }
        }
    }

    private void HandleNoiseLevel(NoiseLevel level)
    {
        if (level == NoiseLevel.High || level == NoiseLevel.Critical)
        {
            currentState = EnemyState.Investigate;
            if (_playerTransform != null)
            {
                _agent.SetDestination(_playerTransform.position);
                Debug.Log("큰 소음 감지: 해당 위치로 탐색 이동합니다.");
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
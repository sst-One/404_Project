using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Patrol, Investigate, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    public EnemyState currentState = EnemyState.Patrol;
    public Transform[] patrolPoints;

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Transform _playerTransform;

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
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.OnNoiseLevelChanged -= HandleNoiseLevel;
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
        if (patrolPoints.Length == 0) return;

        _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }
}
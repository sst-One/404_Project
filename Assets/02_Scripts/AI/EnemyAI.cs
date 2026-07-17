using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Patrol, Suspect, Investigate, Chase }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Narrative Settings")]
    public bool isNarrativeMode = false;

    public EnemyState currentState = EnemyState.Patrol;
    public Transform[] patrolPoints;

    [Header("FOV Settings")]
    public float viewRadius = 10f;
    [Range(0, 360)]
    public float viewAngle = 90f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float fovTickRate = 0.2f;

    [Header("Detection Settings (SYS-005)")]
    public float criticalDetectionDistance = 2.0f; // 이 거리 이내면 Freeze 무시 강제 발각

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _fovCoroutine;
    private Coroutine _suspectCoroutine;

    private void Awake() { _agent = GetComponent<NavMeshAgent>(); }

    private void Start()
    {
        // [수정 핵심] 카메라 루트 대신 우리가 만든 PlayerMovement 인스턴스를 직접 추적
        if (PlayerMovement.Instance != null)
        {
            _playerTransform = PlayerMovement.Instance.transform;
        }
        else if (Camera.main != null)
        {
            _playerTransform = Camera.main.transform.root;
        }

        if (StateManager.Instance != null) StateManager.Instance.OnNoiseLevelChanged += HandleNoiseLevel;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
        }

        MoveToNextPatrolPoint();
        _fovCoroutine = StartCoroutine(FOVRoutine());
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null) StateManager.Instance.OnNoiseLevelChanged -= HandleNoiseLevel;
    }

    private void Update()
    {
        // [수정] 내러티브 모드일 경우 순찰/추격 로직을 원천 차단하고 제자리 대기
        if (isNarrativeMode)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = true;
            return;
        }

        if (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate)
        {
            if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 0.5f)
            {
                if (currentState == EnemyState.Investigate) ChangeState(EnemyState.Patrol);
                MoveToNextPatrolPoint();
            }
        }
        else if (currentState == EnemyState.Chase)
        {
            if (_playerTransform != null && _agent.isOnNavMesh) _agent.SetDestination(_playerTransform.position);
        }
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        if (currentState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        if (newState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.AddThreat();
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
        if (isNarrativeMode) return;

        bool canSeePlayer = false;
        bool isPlayerFreezing = (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing);
        float currentDistanceToPlayer = float.MaxValue;

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
                    currentDistanceToPlayer = dstToTarget;
                }
            }
        }

        if (canSeePlayer)
        {
            if (isPlayerFreezing && Vector3.Distance(transform.position, _playerTransform.position) > criticalDetectionDistance)
            {
                if (currentState == EnemyState.Chase) ChangeState(EnemyState.Investigate);
                return;
            }

            if (currentState != EnemyState.Chase)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                ChangeState(EnemyState.Chase);
            }
        }
        else if (currentState == EnemyState.Chase)
        {
            ChangeState(EnemyState.Investigate);
            if (_agent.isOnNavMesh) _agent.SetDestination(_lastKnownPlayerPosition);
        }
    }

    private void HandleNoiseLevel(NoiseLevel level, Vector3 noisePosition)
    {
        if (isNarrativeMode || currentState == EnemyState.Chase) return;

        if (level == NoiseLevel.Mid && (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate))
        {
            if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
            _suspectCoroutine = StartCoroutine(SuspectRoutine(isVisualDetection: false));
        }
        else if (level == NoiseLevel.High || level == NoiseLevel.Critical)
        {
            if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
            if (_agent.isOnNavMesh) _agent.isStopped = false;

            if (level == NoiseLevel.Critical)
            {
                _lastKnownPlayerPosition = noisePosition;
                ChangeState(EnemyState.Chase);
            }
            else
            {
                ChangeState(EnemyState.Investigate);
                if (_agent.isOnNavMesh) _agent.SetDestination(noisePosition);
            }
        }
    }

    private IEnumerator SuspectRoutine(bool isVisualDetection)
    {
        ChangeState(EnemyState.Suspect);
        if (_agent.isOnNavMesh) _agent.isStopped = true;

        // 시야에 의한 의심일 경우 플레이어 방향을 응시하며 3초 대기
        if (isVisualDetection)
        {
            Vector3 lookDir = (_lastKnownPlayerPosition - transform.position).normalized;
            lookDir.y = 0;
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        yield return new WaitForSeconds(3f);

        if (currentState == EnemyState.Suspect)
        {
            Debug.Log("[EnemyAI] 의심 종료. 위협 요소 없음.");
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
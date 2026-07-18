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
    public float viewRadius = 3f;
    [Range(0, 360)]
    public float viewAngle = 50f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float fovTickRate = 0.2f;

    [Header("Speed Settings")]
    public float patrolSpeed = 0.8f;
    public float chaseSpeed = 1.1f;

    [Header("Detection Settings (SYS-005)")]
    public float criticalDetectionDistance = 1.0f; // 이 거리 이내면 Freeze 무시 강제 발각

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _fovCoroutine;
    private Coroutine _suspectCoroutine;

    private float _lastNoiseReactionTime = 0f;

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

        // [수정됨] 상태에 따른 이동 속도 동적 변경 적용
        if (_agent != null)
        {
            if (currentState == EnemyState.Chase)
            {
                _agent.speed = chaseSpeed;
            }
            else
            {
                _agent.speed = patrolSpeed;
            }
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
            // 1. 임계 거리 이내면 Freeze 여부 상관없이 즉시 추격 (강제 발각)
            if (currentDistanceToPlayer <= criticalDetectionDistance)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                ChangeState(EnemyState.Chase);
                return;
            }

            // 2. Freeze 상태를 유지 중이라면 못 본 척 무시 (또는 Investigate로 강등)
            if (isPlayerFreezing)
            {
                if (currentState == EnemyState.Chase) ChangeState(EnemyState.Investigate);
                return;
            }

            // 3. [핵심 수정] Freeze 실패(움직임) 상태로 시야에 들어왔을 때의 공정성 처리
            if (currentState == EnemyState.Patrol)
            {
                // 순찰 중이었다면 즉시 추격하지 않고 경고(Suspect) 부여
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                _suspectCoroutine = StartCoroutine(SuspectRoutine(_lastKnownPlayerPosition));
            }
            else if (currentState == EnemyState.Suspect || currentState == EnemyState.Investigate)
            {
                // 이미 의심/조사 중인데 눈에 띄었다면 추격 시작
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

        // [핵심 밸런스 수정] 적이 소음에 반응한 직후 3초 동안은 자잘한 소음(Critical 미만)을 무시하여 플레이어에게 도망갈 틈을 줌
        if (Time.time - _lastNoiseReactionTime < 3.0f && level != NoiseLevel.Critical)
        {
            return;
        }

        if (level == NoiseLevel.Mid && currentState == EnemyState.Patrol)
        {
            _lastNoiseReactionTime = Time.time;
            if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
            _suspectCoroutine = StartCoroutine(SuspectRoutine(noisePosition));
        }
        else if (level == NoiseLevel.High || level == NoiseLevel.Critical)
        {
            _lastNoiseReactionTime = Time.time;
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

    // [수정] bool 매개변수 대신 의심스러운 좌표를 직접 받음
    private IEnumerator SuspectRoutine(Vector3 targetPos)
    {
        ChangeState(EnemyState.Suspect);
        if (_agent.isOnNavMesh) _agent.isStopped = true;

        // [수정] 소리가 났거나 시야에 포착된 곳을 향해 몸을 회전
        Vector3 lookDir = (targetPos - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDir);
        }

        // 3초 대기 (이때 플레이어가 추가 소음을 내거나 시야에 노출되면 상태가 덮어씌워짐)
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
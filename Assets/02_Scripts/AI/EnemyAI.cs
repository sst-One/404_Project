using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public enum EnemyState { Idle, Patrol, Suspect, Investigate, Chase, Attack }

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Narrative Settings")]
    public bool isNarrativeMode = false;
    public EnemyState currentState = EnemyState.Idle;
    public Transform[] patrolPoints;

    [Header("FOV Settings")]
    public float viewRadius = 3f;
    [Range(0, 360)]
    public float viewAngle = 50f;
    public LayerMask playerMask;
    public LayerMask obstacleMask;
    public float fovTickRate = 0.2f;

    [Header("Speed Settings (Manual Control)")]
    public float patrolSpeed = 0.8f;
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 3.0f;

    [Header("Detection Settings (SYS-005)")]
    public float criticalDetectionDistance = 1.0f;
    public float patrolWaitTime = 2.0f;
    public float catchDistance = 1.2f;

    public event System.Action<Transform> OnPlayerCaught;

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Animator _animator;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _fovCoroutine;
    private Coroutine _suspectCoroutine;
    private Coroutine _patrolWaitCoroutine;
    private float _lastNoiseReactionTime = 0f;

    [Header("Audio Settings")]
    public AudioSource footstepSource;
    public AudioSource breathSource;
    public AudioSource actionSource;

    [Header("Audio Clip Names")]
    public string smallFootWalkClipName = "";
    public string bigFootWalkClipName = "";
    public string enemyBreathCloseClipName = "";

    private AudioClip _smallFootClip;
    private AudioClip _bigFootClip;
    private AudioClip _breathClip;

    // [완전 개조] Idle을 포함한 모든 이동 상태를 Bool 기반 루프로 제어
    private int _isIdleHash;
    private int _isWalkHash;
    private int _isRunHash;
    private int _isChaseHash;
    private int _attackHash;
    private string _currentAnimState = "";

    private WaitForSeconds _fovWait;
    private WaitForSeconds _suspectWait;
    private WaitForSeconds _patrolWait;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _mainCamera = Camera.main;

        if (_agent != null) _agent.enabled = true;

        _isIdleHash = Animator.StringToHash("IsIdle");
        _isWalkHash = Animator.StringToHash("IsWalk");
        _isRunHash = Animator.StringToHash("IsRun");
        _isChaseHash = Animator.StringToHash("IsChase");
        _attackHash = Animator.StringToHash("Attack");

        _fovWait = new WaitForSeconds(fovTickRate);
        _suspectWait = new WaitForSeconds(3f);
        _patrolWait = new WaitForSeconds(patrolWaitTime);
    }

    private void Start()
    {
        if (PlayerController.Instance != null) _playerTransform = PlayerController.Instance.transform;
        else if (_mainCamera != null) _playerTransform = _mainCamera.transform.root;

        if (AudioManager.Instance != null)
        {
            _smallFootClip = AudioManager.Instance.GetClip(smallFootWalkClipName);
            _bigFootClip = AudioManager.Instance.GetClip(bigFootWalkClipName);
            _breathClip = AudioManager.Instance.GetClip(enemyBreathCloseClipName);
        }

        if (StateManager.Instance != null) StateManager.Instance.OnNoiseLevelChanged += HandleNoiseLevel;

        if (isNarrativeMode) return;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
        {
            _agent.Warp(hit.position);
        }

        ChangeState(EnemyState.Patrol);
        MoveToNextPatrolPoint();

        _fovCoroutine = StartCoroutine(FOVRoutine());
    }

    private void OnDestroy()
    {
        if (StateManager.Instance != null) StateManager.Instance.OnNoiseLevelChanged -= HandleNoiseLevel;
    }

    public void TriggerNarrativeAnimation(string stateName)
    {
        SetAnimationState(stateName);
    }

    private void SetAnimationState(string stateName)
    {
        if (_animator == null) return;

        // 모든 루프형 애니메이션 Bool을 일단 차단 후 지정된 것만 활성화
        _animator.SetBool(_isIdleHash, false);
        _animator.SetBool(_isWalkHash, false);
        _animator.SetBool(_isRunHash, false);
        _animator.SetBool(_isChaseHash, false);

        switch (stateName)
        {
            case "Idle":
                _animator.SetBool(_isIdleHash, true);
                break;
            case "Walk":
                _animator.SetBool(_isWalkHash, true);
                break;
            case "Run":
                _animator.SetBool(_isRunHash, true);
                break;
            case "Chase":
                _animator.SetBool(_isChaseHash, true);
                break;
            case "Attack":
                _animator.SetTrigger(_attackHash);
                break;
        }

        _currentAnimState = stateName;
    }

    private void Update()
    {
        if (currentState == EnemyState.Attack || isNarrativeMode || (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()))
        {
            if (_agent != null && _agent.isOnNavMesh && !_agent.isStopped)
            {
                _agent.isStopped = true;
                _agent.velocity = Vector3.zero;
            }
            if (currentState != EnemyState.Attack)
            {
                ChangeState(EnemyState.Idle);
            }
            return;
        }

        if (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate)
        {
            if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 0.5f)
            {
                if (currentState == EnemyState.Investigate)
                {
                    ReturnToNearestWaypoint();
                    ChangeState(EnemyState.Patrol);
                }

                if (_patrolWaitCoroutine == null && currentState == EnemyState.Patrol)
                {
                    _patrolWaitCoroutine = StartCoroutine(WaitAtPatrolPointRoutine());
                }
            }
        }
        else if (currentState == EnemyState.Chase)
        {
            if (_playerTransform != null && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_playerTransform.position);

                float distSq = (transform.position - _playerTransform.position).sqrMagnitude;
                if (distSq <= catchDistance * catchDistance)
                {
                    TriggerCatchPlayer();
                    return;
                }
            }
        }

        bool isChasing = (currentState == EnemyState.Chase);
        bool isCloseToPlayer = false;

        if (_playerTransform != null)
        {
            float distanceSq = (transform.position - _playerTransform.position).sqrMagnitude;
            if (distanceSq <= 6.25f) isCloseToPlayer = true;
        }

        UpdateAudioState(isChasing, isCloseToPlayer);
    }

    private void TriggerCatchPlayer()
    {
        ChangeState(EnemyState.Attack);
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.velocity = Vector3.zero;
        }

        Vector3 lookDir = (_playerTransform.position - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        OnPlayerCaught?.Invoke(this.transform);
    }

    public void ResetEnemy()
    {
        if (patrolPoints != null && patrolPoints.Length > 0 && patrolPoints[0] != null)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.Warp(patrolPoints[0].position);
                _agent.isStopped = false;
            }
            _currentPatrolIndex = 0;
        }

        ChangeState(EnemyState.Patrol);
        MoveToNextPatrolPoint();
    }

    private void ReturnToNearestWaypoint()
    {
        if (patrolPoints == null || patrolPoints.Length == 0) return;

        float minDistance = float.MaxValue;
        int nearestIndex = 0;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            if (patrolPoints[i] == null) continue;
            float dist = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearestIndex = i;
            }
        }

        _currentPatrolIndex = nearestIndex;
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        }
    }

    private IEnumerator WaitAtPatrolPointRoutine()
    {
        ChangeState(EnemyState.Idle);

        yield return _patrolWait;

        if (currentState == EnemyState.Idle)
        {
            ChangeState(EnemyState.Patrol);
            MoveToNextPatrolPoint();
        }
        _patrolWaitCoroutine = null;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        if (_patrolWaitCoroutine != null && newState != EnemyState.Idle && newState != EnemyState.Patrol)
        {
            StopCoroutine(_patrolWaitCoroutine);
            _patrolWaitCoroutine = null;
        }

        if (currentState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        if (newState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.AddThreat();

        currentState = newState;

        if (_agent != null && _agent.isOnNavMesh)
        {
            switch (currentState)
            {
                case EnemyState.Idle:
                    _agent.isStopped = true;
                    _agent.speed = 0f;
                    SetAnimationState("Idle");
                    break;
                case EnemyState.Patrol:
                    _agent.isStopped = false;
                    _agent.speed = patrolSpeed;
                    SetAnimationState("Walk");
                    break;
                case EnemyState.Investigate:
                    _agent.isStopped = false;
                    _agent.speed = investigateSpeed;
                    SetAnimationState("Run");
                    break;
                case EnemyState.Chase:
                    _agent.isStopped = false;
                    _agent.speed = chaseSpeed;
                    SetAnimationState("Chase");
                    break;
                case EnemyState.Suspect:
                    _agent.isStopped = true;
                    _agent.speed = 0f;
                    SetAnimationState("Idle");
                    break;
                case EnemyState.Attack:
                    _agent.isStopped = true;
                    _agent.speed = 0f;
                    SetAnimationState("Attack");
                    break;
            }
        }
    }

    private IEnumerator FOVRoutine()
    {
        while (true)
        {
            yield return _fovWait;
            FindVisibleTargets();
        }
    }

    private void FindVisibleTargets()
    {
        if (isNarrativeMode || _mainCamera == null || _playerTransform == null || currentState == EnemyState.Attack) return;

        bool canSeePlayer = false;
        bool isPlayerFreezing = (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive);
        float currentDistanceToPlayerSq = float.MaxValue;

        Vector3 targetPos = _mainCamera.transform.position;
        Vector3 dirToTarget = (targetPos - transform.position).normalized;

        float distanceToTargetSq = (transform.position - _playerTransform.position).sqrMagnitude;
        float viewRadiusSq = viewRadius * viewRadius;

        if (distanceToTargetSq <= viewRadiusSq)
        {
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle * 0.5f)
            {
                float dstToTarget = Mathf.Sqrt(distanceToTargetSq);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    canSeePlayer = true;
                    _lastKnownPlayerPosition = _playerTransform.position;
                    currentDistanceToPlayerSq = distanceToTargetSq;
                }
            }
        }

        if (canSeePlayer)
        {
            float criticalDistSq = criticalDetectionDistance * criticalDetectionDistance;
            if (currentDistanceToPlayerSq <= criticalDistSq)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                ChangeState(EnemyState.Chase);
                return;
            }

            if (isPlayerFreezing)
            {
                if (currentState == EnemyState.Chase) ChangeState(EnemyState.Investigate);
                return;
            }

            if (currentState == EnemyState.Patrol || currentState == EnemyState.Idle)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                _suspectCoroutine = StartCoroutine(SuspectRoutine(_lastKnownPlayerPosition));
            }
            else if (currentState == EnemyState.Suspect || currentState == EnemyState.Investigate)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
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
        if (isNarrativeMode || currentState == EnemyState.Chase || currentState == EnemyState.Attack) return;
        if (Time.time - _lastNoiseReactionTime < 3.0f && level != NoiseLevel.Critical) return;

        if (level == NoiseLevel.Mid && (currentState == EnemyState.Patrol || currentState == EnemyState.Idle))
        {
            _lastNoiseReactionTime = Time.time;
            if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
            _suspectCoroutine = StartCoroutine(SuspectRoutine(noisePosition));
        }
        else if (level == NoiseLevel.High || level == NoiseLevel.Critical)
        {
            _lastNoiseReactionTime = Time.time;
            if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);

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

    private IEnumerator SuspectRoutine(Vector3 targetPos)
    {
        ChangeState(EnemyState.Suspect);

        Vector3 lookDir = (targetPos - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        yield return _suspectWait;

        if (currentState == EnemyState.Suspect)
        {
            ReturnToNearestWaypoint();
            ChangeState(EnemyState.Patrol);
        }
    }

    private void MoveToNextPatrolPoint()
    {
        if (isNarrativeMode || patrolPoints == null || patrolPoints.Length == 0) return;
        if (_currentPatrolIndex >= patrolPoints.Length || patrolPoints[_currentPatrolIndex] == null)
        {
            _currentPatrolIndex = 0;
            if (patrolPoints.Length == 0 || patrolPoints[0] == null) return;
        }

        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = false;
            _agent.SetDestination(patrolPoints[_currentPatrolIndex].position);
        }
        _currentPatrolIndex = (_currentPatrolIndex + 1) % patrolPoints.Length;
    }

    private void UpdateAudioState(bool isChasing, bool isCloseToPlayer)
    {
        AudioClip targetFootstep = isChasing ? _bigFootClip : _smallFootClip;

        if (footstepSource != null && targetFootstep != null)
        {
            if (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate || currentState == EnemyState.Chase)
            {
                if (footstepSource.clip != targetFootstep)
                {
                    footstepSource.clip = targetFootstep;
                    footstepSource.Play();
                }
                else if (!footstepSource.isPlaying) footstepSource.Play();
            }
            else
            {
                if (footstepSource.isPlaying) footstepSource.Stop();
            }
        }

        if (breathSource != null && _breathClip != null)
        {
            if (isCloseToPlayer)
            {
                if (breathSource.clip != _breathClip) breathSource.clip = _breathClip;
                if (!breathSource.isPlaying) breathSource.Play();
            }
            else
            {
                if (breathSource.isPlaying) breathSource.Stop();
            }
        }
    }
}
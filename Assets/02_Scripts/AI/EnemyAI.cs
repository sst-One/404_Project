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

    [Header("Speed Settings (Animation Sync)")]
    public float patrolSpeed = 0.8f;
    public float investigateSpeed = 1.5f;
    public float chaseSpeed = 2.5f;

    [Header("Patrol Settings")]
    public float patrolWaitTime = 2.0f;

    [Header("Detection Settings (SYS-005)")]
    public float criticalDetectionDistance = 1.0f;

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

    private int _speedHash;
    private WaitForSeconds _fovWait;
    private WaitForSeconds _suspectWait;
    private WaitForSeconds _patrolWait;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _animator = GetComponentInChildren<Animator>();
        _mainCamera = Camera.main;

        _speedHash = Animator.StringToHash("Speed");
        _fovWait = new WaitForSeconds(fovTickRate);
        _suspectWait = new WaitForSeconds(3f);
        _patrolWait = new WaitForSeconds(patrolWaitTime);
    }

    private void Start()
    {
        if (PlayerController.Instance != null) _playerTransform = PlayerController.Instance.transform;
        else if (_mainCamera != null) _playerTransform = _mainCamera.transform.root;

        if (StateManager.Instance != null) StateManager.Instance.OnNoiseLevelChanged += HandleNoiseLevel;

        // [핫픽스 1] 컷신 전용 AI일 경우 여기서 즉시 스크립트 실행을 종료하여 Null 에러 원천 차단
        if (isNarrativeMode) return;

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
        if (_animator != null && _agent != null)
        {
            _animator.SetFloat(_speedHash, _agent.velocity.magnitude);
        }

        if (isNarrativeMode || (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()))
        {
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            return;
        }

        if (currentState == EnemyState.Patrol || currentState == EnemyState.Investigate)
        {
            if (_agent.isOnNavMesh && !_agent.pathPending && _agent.remainingDistance < 0.5f)
            {
                if (currentState == EnemyState.Investigate) ChangeState(EnemyState.Patrol);

                if (_patrolWaitCoroutine == null)
                {
                    _patrolWaitCoroutine = StartCoroutine(WaitAtPatrolPointRoutine());
                }
            }
        }
        else if (currentState == EnemyState.Chase)
        {
            if (_playerTransform != null && _agent.isOnNavMesh) _agent.SetDestination(_playerTransform.position);
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

    private IEnumerator WaitAtPatrolPointRoutine()
    {
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.speed = 0f;
        }

        yield return _patrolWait;

        if (currentState == EnemyState.Patrol)
        {
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = false;
                _agent.speed = patrolSpeed;
            }
            MoveToNextPatrolPoint();
        }
        _patrolWaitCoroutine = null;
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;

        if (_patrolWaitCoroutine != null)
        {
            StopCoroutine(_patrolWaitCoroutine);
            _patrolWaitCoroutine = null;
        }

        if (currentState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        if (newState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.AddThreat();

        currentState = newState;

        if (_agent != null)
        {
            switch (currentState)
            {
                case EnemyState.Patrol: _agent.speed = patrolSpeed; break;
                case EnemyState.Investigate: _agent.speed = investigateSpeed; break;
                case EnemyState.Chase: _agent.speed = chaseSpeed; break;
                case EnemyState.Suspect: _agent.speed = 0f; break;
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
        if (isNarrativeMode || _mainCamera == null || _playerTransform == null) return;

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
                if (_agent.isOnNavMesh) _agent.isStopped = false;
                ChangeState(EnemyState.Chase);
                return;
            }

            if (isPlayerFreezing)
            {
                if (currentState == EnemyState.Chase) ChangeState(EnemyState.Investigate);
                return;
            }

            if (currentState == EnemyState.Patrol)
            {
                if (_suspectCoroutine != null) StopCoroutine(_suspectCoroutine);
                _suspectCoroutine = StartCoroutine(SuspectRoutine(_lastKnownPlayerPosition));
            }
            else if (currentState == EnemyState.Suspect || currentState == EnemyState.Investigate)
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
        if (Time.time - _lastNoiseReactionTime < 3.0f && level != NoiseLevel.Critical) return;

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

    private IEnumerator SuspectRoutine(Vector3 targetPos)
    {
        ChangeState(EnemyState.Suspect);
        if (_agent.isOnNavMesh) _agent.isStopped = true;

        Vector3 lookDir = (targetPos - transform.position).normalized;
        lookDir.y = 0;
        if (lookDir != Vector3.zero) transform.rotation = Quaternion.LookRotation(lookDir);

        yield return _suspectWait;

        if (currentState == EnemyState.Suspect)
        {
            if (_agent.isOnNavMesh) _agent.isStopped = false;
            ChangeState(EnemyState.Patrol);
            MoveToNextPatrolPoint();
        }
    }

    private void MoveToNextPatrolPoint()
    {
        // [핫픽스 1] 순찰 배열 방어 코드 완벽 강화
        if (isNarrativeMode || patrolPoints == null || patrolPoints.Length == 0) return;
        if (_currentPatrolIndex >= patrolPoints.Length || patrolPoints[_currentPatrolIndex] == null)
        {
            _currentPatrolIndex = 0; // 예외 발생 시 0으로 초기화
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
        if (AudioManager.Instance == null) return;

        string targetClipName = isChasing ? bigFootWalkClipName : smallFootWalkClipName;
        AudioClip targetFootstep = string.IsNullOrEmpty(targetClipName) ? null : AudioManager.Instance.GetClip(targetClipName);

        if (footstepSource != null && targetFootstep != null)
        {
            if (footstepSource.clip != targetFootstep)
            {
                footstepSource.clip = targetFootstep;
                footstepSource.Play();
            }
            else if (!footstepSource.isPlaying) footstepSource.Play();
        }

        AudioClip targetBreath = string.IsNullOrEmpty(enemyBreathCloseClipName) ? null : AudioManager.Instance.GetClip(enemyBreathCloseClipName);
        if (breathSource != null && targetBreath != null)
        {
            if (isCloseToPlayer)
            {
                if (breathSource.clip != targetBreath) breathSource.clip = targetBreath;
                if (!breathSource.isPlaying) breathSource.Play();
            }
            else
            {
                if (breathSource.isPlaying) breathSource.Stop();
            }
        }
    }
}
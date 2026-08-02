// 2. EnemyAI.cs
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
    public float criticalDetectionDistance = 1.0f;

    private int _currentPatrolIndex;
    private NavMeshAgent _agent;
    private Transform _playerTransform;
    private Camera _mainCamera;
    private Vector3 _lastKnownPlayerPosition;

    private Coroutine _fovCoroutine;
    private Coroutine _suspectCoroutine;
    private float _lastNoiseReactionTime = 0f;
    private PlayerInputProvider _playerInput;

    [Header("오디오 설정 (AudioSources)")]
    public AudioSource footstepSource;
    public AudioSource breathSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string smallFootWalkClipName = "";
    public string bigFootWalkClipName = "";
    public string enemyBreathCloseClipName = "";

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _mainCamera = Camera.main;
    }

    private void Start()
    {
        if (PlayerMovement.Instance != null) _playerTransform = PlayerMovement.Instance.transform;
        else if (_mainCamera != null) _playerTransform = _mainCamera.transform.root;

        _playerInput = FindObjectOfType<PlayerInputProvider>();

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
        if (isNarrativeMode || (UIManager.Instance != null && UIManager.Instance.IsAnyUIBlocking()))
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

        bool isChasing = (currentState == EnemyState.Chase);
        bool isCloseToPlayer = false;

        if (_playerTransform != null)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, _playerTransform.position);
            if (distanceToPlayer <= 2.5f) isCloseToPlayer = true;
        }

        UpdateAudioState(isChasing, isCloseToPlayer);
    }

    private void ChangeState(EnemyState newState)
    {
        if (currentState == newState) return;
        if (currentState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        if (newState == EnemyState.Chase && StateManager.Instance != null) StateManager.Instance.AddThreat();

        currentState = newState;

        if (_agent != null)
        {
            _agent.speed = (currentState == EnemyState.Chase) ? chaseSpeed : patrolSpeed;
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
        if (isNarrativeMode || _mainCamera == null) return;

        bool canSeePlayer = false;
        bool isPlayerFreezing = (_playerInput != null && _playerInput.IsFreezeActive);
        float currentDistanceToPlayer = float.MaxValue;

        Vector3 targetPos = _mainCamera.transform.position;
        Vector3 dirToTarget = (targetPos - transform.position).normalized;

        if (Vector3.Distance(transform.position, _playerTransform.position) <= viewRadius)
        {
            if (Vector3.Angle(transform.forward, dirToTarget) < viewAngle / 2)
            {
                float dstToTarget = Vector3.Distance(transform.position, targetPos);
                if (!Physics.Raycast(transform.position, dirToTarget, dstToTarget, obstacleMask))
                {
                    canSeePlayer = true;
                    _lastKnownPlayerPosition = _playerTransform.position;
                    currentDistanceToPlayer = dstToTarget;
                }
            }
        }

        if (canSeePlayer)
        {
            if (currentDistanceToPlayer <= criticalDetectionDistance)
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
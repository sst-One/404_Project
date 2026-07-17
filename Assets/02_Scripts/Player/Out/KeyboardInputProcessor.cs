using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class KeyboardInputProcessor : MonoBehaviour, IPlayerInput
{
    [Header("UI Feedback")]
    public GameObject moveUIIndicator;

    private Camera _mainCamera;
    private Transform _currentHoverTarget;
    private Vector3 _currentHitPoint;
    private Vector3 _currentHitNormal;
    private float _currentReadyTimer = 0f;
    private bool _isReadyTriggered = false;

    private float readyTimeThreshold = 0.5f;
    private LayerMask _targetMask;
    private LayerMask _obstacleMask;

    public KeyboardInputProcessor() { }

    private void Start()
    {
        if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
    }

    // [핵심 수정] 매 프레임 무조건 레이캐스트 연산 및 UI 업데이트 수행
    private void Update()
    {
        UpdateMouseRaycast();
    }

    public Vector3 GetHandAimTarget()
    {
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    }

    public bool IsGripToggled() { return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame; }
    public bool IsLeanCommitted() { return Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame; }
    public bool IsFreezing() { return Keyboard.current != null && Keyboard.current.spaceKey.isPressed; }

    // [핵심 수정] 연산 로직을 제거하고 캐싱된 데이터만 안전하게 반환
    public Transform GetHoveredTarget() { return _currentHoverTarget; }
    public Vector3 GetHoveredPoint() { return _currentHitPoint; }
    public Vector3 GetHoveredNormal() { return _currentHitNormal; }
    public bool IsTargetReady() { return _isReadyTriggered; }

    public void CalibrateBaseline() { }

    private void UpdateMouseRaycast()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        if (_targetMask == 0) _targetMask = LayerMask.GetMask("Interactable", "Floor", "Threat");
        if (_obstacleMask == 0) _obstacleMask = LayerMask.GetMask("Obstacle");

        Ray ray = _mainCamera.ScreenPointToRay(GetHandAimTarget());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 15f, _targetMask))
        {
            Vector3 dirToTarget = hit.point - _mainCamera.transform.position;

            if (!Physics.Raycast(_mainCamera.transform.position, dirToTarget.normalized, dirToTarget.magnitude, _obstacleMask))
            {
                if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Floor"))
                {
                    if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
                    {
                        _currentHitPoint = navHit.position;

                        if (moveUIIndicator != null)
                        {
                            moveUIIndicator.transform.position = _currentHitPoint + Vector3.up * 0.05f;
                            moveUIIndicator.SetActive(true);
                        }
                    }
                    else
                    {
                        ResetTarget();
                        return;
                    }
                }
                else
                {
                    _currentHitPoint = hit.point;
                    if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
                }

                _currentHitNormal = hit.normal;

                if (_currentHoverTarget == hit.transform)
                {
                    if (!_isReadyTriggered)
                    {
                        _currentReadyTimer += Time.deltaTime;
                        if (_currentReadyTimer >= readyTimeThreshold) _isReadyTriggered = true;
                    }
                }
                else
                {
                    _currentHoverTarget = hit.transform;
                    _currentReadyTimer = 0f;
                    _isReadyTriggered = false;
                }
            }
            else { ResetTarget(); }
        }
        else { ResetTarget(); }
    }

    private void ResetTarget()
    {
        _currentHoverTarget = null;
        _currentReadyTimer = 0f;
        _isReadyTriggered = false;
        if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
    }
}
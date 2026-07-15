using UnityEngine;
using UnityEngine.InputSystem;

public class KeyboardInputProcessor : MonoBehaviour, IPlayerInput
{
    private Camera _mainCamera;
    private Transform _currentHoverTarget;
    private Vector3 _currentHitPoint;
    private Vector3 _currentHitNormal;
    private float _currentReadyTimer = 0f;
    private bool _isReadyTriggered = false;

    private float readyTimeThreshold = 0.5f;
    private LayerMask _targetMask;
    private LayerMask _obstacleMask;

    public KeyboardInputProcessor()
    {
    }

    public Vector3 GetHandAimTarget()
    {
        if (Mouse.current != null) return Mouse.current.position.ReadValue();
        return new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
    }

    public bool IsGripToggled()
    {
        if (Mouse.current != null) return Mouse.current.leftButton.wasPressedThisFrame;
        return false;
    }

    public bool IsLeanCommitted()
    {
        if (Keyboard.current != null) return Keyboard.current.wKey.wasPressedThisFrame;
        return false;
    }

    public bool IsFreezing()
    {
        if (Keyboard.current != null) return Keyboard.current.spaceKey.isPressed;
        return false;
    }

    public Transform GetHoveredTarget()
    {
        UpdateMouseRaycast();
        return _currentHoverTarget;
    }

    public Vector3 GetHoveredPoint() { return _currentHitPoint; }
    public Vector3 GetHoveredNormal() { return _currentHitNormal; }
    public bool IsTargetReady() { return _isReadyTriggered; }

    private void UpdateMouseRaycast()
    {
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        if (_targetMask == 0)
        {
            // MovePoint 대신 Floor 레이어를 타겟으로 포함
            _targetMask = LayerMask.GetMask("Interactable", "Floor", "Threat");
        }
        if (_obstacleMask == 0)
        {
            _obstacleMask = LayerMask.GetMask("Obstacle");
        }

        Ray ray = _mainCamera.ScreenPointToRay(GetHandAimTarget());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 15f, _targetMask))
        {
            Vector3 dirToTarget = hit.point - _mainCamera.transform.position;

            // 2차 판정: 장애물(Obstacle)이 시야를 가리는지 확인
            if (!Physics.Raycast(_mainCamera.transform.position, dirToTarget.normalized, dirToTarget.magnitude, _obstacleMask))
            {
                _currentHitPoint = hit.point;
                _currentHitNormal = hit.normal;

                if (_currentHoverTarget == hit.transform)
                {
                    if (!_isReadyTriggered)
                    {
                        _currentReadyTimer += Time.deltaTime;
                        if (_currentReadyTimer >= readyTimeThreshold)
                        {
                            _isReadyTriggered = true;
                            Debug.Log("[Keyboard Fallback] 타겟 Ready (0.5s): " + hit.transform.name);
                        }
                    }
                }
                else
                {
                    _currentHoverTarget = hit.transform;
                    _currentReadyTimer = 0f;
                    _isReadyTriggered = false;
                }
            }
            else
            {
                // Obstacle에 가려짐
                ResetTarget();
            }
        }
        else
        {
            // TargetMask를 벗어남
            ResetTarget();
        }
    }

    private void ResetTarget()
    {
        if (_currentHoverTarget != null)
        {
            _currentHoverTarget = null;
            _currentReadyTimer = 0f;
            _isReadyTriggered = false;
        }
    }
}
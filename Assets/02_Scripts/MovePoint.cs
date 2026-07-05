using UnityEngine;

public class MovePoint : MonoBehaviour, IInteractable
{
    private MeshRenderer _meshRenderer;
    private Color _originalColor;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        if (_meshRenderer != null)
        {
            _originalColor = _meshRenderer.material.color;
        }
    }

    public void OnFocusEnter()
    {
        ChangeColor(Color.yellow);
    }

    public void OnFocusExit()
    {
        ChangeColor(_originalColor);
    }

    public void OnReadyStateReached()
    {
        ChangeColor(Color.cyan);
        Debug.Log("MovePoint Ready: 이동하려면 Lean(W키)을 입력하십시오.");
    }

    public void OnInteract()
    {
        // 이동 지점은 Grip(좌클릭)에 반응하지 않습니다.
    }

    public void OnLean()
    {
        if (MovementManager.Instance != null && !MovementManager.Instance.IsMoving)
        {
            ChangeColor(Color.magenta);
            MovementManager.Instance.MoveTo(transform.position);
        }
    }

    private void ChangeColor(Color color)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = color;
        }
    }
}
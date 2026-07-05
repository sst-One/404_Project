using UnityEngine;

public class TestInteractableCube : MonoBehaviour, IInteractable
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
        // Hand Aim이 큐브에 닿았을 때: 노란색으로 변경 (인식 피드백)
        ChangeColor(Color.yellow);
        Debug.Log("Focus Enter: 대상 탐지됨. Ready 타이머 시작.");
    }

    public void OnFocusExit()
    {
        // Hand Aim이 큐브를 벗어났을 때: 원래 색상으로 복구 (초기화)
        ChangeColor(_originalColor);
        Debug.Log("Focus Exit: 시선 벗어남. 타이머 초기화.");
    }

    public void OnReadyStateReached()
    {
        // 일정 시간(기본 0.5초) 시선 유지 성공 시: 녹색으로 변경 (상호작용 가능 피드백)
        ChangeColor(Color.green);
        Debug.Log("Ready State: 상호작용 준비 완료. Grip(좌클릭) 대기 중.");
    }

    public void OnInteract()
    {
        // Ready 상태에서 Grip(좌클릭) 발생 시: 빨간색으로 변경 및 크기 축소 (실행 피드백)
        ChangeColor(Color.red);
        transform.localScale = Vector3.one * 0.8f;
        Debug.Log("Interact: 상호작용 성공! 트리거 작동.");
    }

    public void OnLean()
    {

    }

    private void ChangeColor(Color color)
    {
        if (_meshRenderer != null)
        {
            _meshRenderer.material.color = color;
        }
    }
}
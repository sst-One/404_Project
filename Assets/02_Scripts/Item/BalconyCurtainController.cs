using UnityEngine;

/// <summary>
/// 발코니 커튼 모델의 루트(또는 시선 판정용 Collider가 있는 오브젝트)에 붙입니다.
/// 실제 커튼 모델이 추가되면 curtainTransform과 openLocalOffset을 인스펙터에서 지정하세요.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BalconyCurtainController : MonoBehaviour, IInteractable
{
    [Header("Curtain Target")]
    public Transform curtainTransform;
    public Vector3 openLocalOffset = new Vector3(1.4f, 0f, 0f);
    [Min(0.1f)] public float openSpeed = 1.8f;
    public bool allowCloseAfterOpen = false;
    public bool isInteractable = true;

    public bool IsFocused { get; private set; }
    public bool IsOpen { get; private set; }

    private Vector3 _closedLocalPosition;
    private Vector3 _targetLocalPosition;
    private bool _isMoving;

    private void Awake()
    {
        if (curtainTransform == null)
            curtainTransform = transform;

        _closedLocalPosition = curtainTransform.localPosition;
        _targetLocalPosition = _closedLocalPosition;
    }

    private void Update()
    {
        if (!_isMoving || curtainTransform == null)
            return;

        curtainTransform.localPosition = Vector3.MoveTowards(
            curtainTransform.localPosition,
            _targetLocalPosition,
            openSpeed * Time.deltaTime
        );

        if (Vector3.SqrMagnitude(curtainTransform.localPosition - _targetLocalPosition) < 0.0001f)
        {
            curtainTransform.localPosition = _targetLocalPosition;
            _isMoving = false;
        }
    }

    public void OnFocusEnter() => IsFocused = true;
    public void OnFocusExit() => IsFocused = false;
    public void OnReadyStateReached() { }
    public void OnLean() { }

    public void OnInteract()
    {
        if (!isInteractable || curtainTransform == null)
            return;

        if (IsOpen && !allowCloseAfterOpen)
            return;

        IsOpen = !IsOpen;
        _targetLocalPosition = IsOpen
            ? _closedLocalPosition + openLocalOffset
            : _closedLocalPosition;
        _isMoving = true;
    }
}

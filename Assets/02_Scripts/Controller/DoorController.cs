using System.Collections;
using UnityEngine;

public enum DoorType { Swinging, Sliding }

[RequireComponent(typeof(InteractableItem))]
[RequireComponent(typeof(Collider))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public DoorType doorType = DoorType.Swinging;
    [Tooltip("에디터에서 나란히 배치한 빈 오브젝트(Hinge)를 할당하세요. 런타임에 자동으로 부모로 병합됩니다.")]
    public Transform doorTargetTransform;
    public AudioSource doorAudioSource;
    public float moveDuration = 0.5f;

    [Header("Swinging (여닫이) Settings")]
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0, 90f, 0);

    [Header("Sliding (미닫이) Settings")]
    public Vector3 closedPosition = Vector3.zero;
    public Vector3 openPosition = new Vector3(1.2f, 0, 0);

    [Header("Audio Clip Names")]
    public string openClipName = "SND-018_DoorOpen_OneShot";
    public string closeClipName = "SND-019_DoorClose_OneShot";

    private InteractableItem _interactableItem;
    private Collider _doorCollider;
    private bool _isOpen = false;
    private bool _isMoving = false;

    private void Start()
    {
        _interactableItem = GetComponent<InteractableItem>();
        _doorCollider = GetComponent<Collider>();

        if (_interactableItem != null)
        {
            _interactableItem.interactOnlyOnce = false;
            _interactableItem.onInteractEvent.RemoveAllListeners();
            _interactableItem.onInteractEvent.AddListener(OnDoorInteracted);
        }

        // [핵심 핫픽스] 런타임 동적 부모 할당 (에셋 프리팹 잠금 완벽 우회)
        if (doorType == DoorType.Swinging && doorTargetTransform != null && doorTargetTransform != transform)
        {
            // 월드 좌표를 유지한 채로 Hinge 오브젝트의 자식으로 강제 편입
            transform.SetParent(doorTargetTransform, true);
        }
        else if (doorTargetTransform == null)
        {
            doorTargetTransform = transform;
        }
    }

    private void OnDoorInteracted()
    {
        if (_isMoving) return;
        StartCoroutine(ToggleDoorRoutine());
    }

    private IEnumerator ToggleDoorRoutine()
    {
        _isMoving = true;
        _isOpen = !_isOpen;

        // 회전 중 물리 간섭 방지
        if (_doorCollider != null) _doorCollider.enabled = false;
        if (_interactableItem != null) _interactableItem.isInteractable = false;

        if (doorAudioSource != null && AudioManager.Instance != null)
        {
            string clipName = _isOpen ? openClipName : closeClipName;
            AudioClip clip = AudioManager.Instance.GetClip(clipName);
            if (clip != null) doorAudioSource.PlayOneShot(clip);
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.15f, transform.position);
        }

        float timer = 0f;

        if (doorType == DoorType.Swinging)
        {
            Quaternion startRot = doorTargetTransform.localRotation;
            Quaternion endRot = Quaternion.Euler(_isOpen ? openRotation : closedRotation);

            while (timer < moveDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);
                doorTargetTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }
            doorTargetTransform.localRotation = endRot;
        }
        else if (doorType == DoorType.Sliding)
        {
            Vector3 startPos = doorTargetTransform.localPosition;
            Vector3 endPos = _isOpen ? openPosition : closedPosition;

            while (timer < moveDuration)
            {
                timer += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);
                doorTargetTransform.localPosition = Vector3.Lerp(startPos, endPos, t);
                yield return null;
            }
            doorTargetTransform.localPosition = endPos;
        }

        // 이동 완료 후 조작 권한 원복
        if (_doorCollider != null) _doorCollider.enabled = true;
        if (_interactableItem != null) _interactableItem.isInteractable = true;

        _isMoving = false;
    }
}
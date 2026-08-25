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

        if (doorType == DoorType.Swinging && doorTargetTransform != null && doorTargetTransform != transform)
        {
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

        if (_doorCollider != null) _doorCollider.enabled = false;
        if (_interactableItem != null) _interactableItem.isInteractable = false;

        // [최적화] AudioSource 제거. AudioManager 글로벌 동적 생성 재생 위임
        if (AudioManager.Instance != null)
        {
            string clipName = _isOpen ? openClipName : closeClipName;
            if (!string.IsNullOrEmpty(clipName))
            {
                AudioManager.Instance.PlayGlobal2D(clipName, AudioManager.Instance.sfxMixerGroup);
            }
        }

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.15f, transform.position);

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

        if (_doorCollider != null) _doorCollider.enabled = true;
        if (_interactableItem != null) _interactableItem.isInteractable = true;

        _isMoving = false;
    }
}
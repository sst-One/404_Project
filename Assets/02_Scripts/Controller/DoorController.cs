using System.Collections;
using UnityEngine;

[RequireComponent(typeof(InteractableItem))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    [Tooltip("에디터에서 나란히 배치한 빈 오브젝트(Hinge)를 할당하세요. 런타임에 자동으로 부모로 병합됩니다.")]
    public Transform doorTargetTransform;
    public float moveDuration = 0.5f;

    [Header("Swinging Settings")]
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0, 90f, 0);

    [Header("Audio Clip Names")]
    public string openClipName = "SND-018_DoorOpen_OneShot";
    public string closeClipName = "SND-019_DoorClose_OneShot";

    private InteractableItem _interactableItem;
    private Collider[] _doorColliders;
    private bool _isOpen = false;
    private Coroutine _moveCoroutine;

    private void Start()
    {
        _interactableItem = GetComponent<InteractableItem>();
        _doorColliders = GetComponents<Collider>();

        if (_interactableItem != null)
        {
            _interactableItem.interactOnlyOnce = false;
            _interactableItem.onInteractEvent.RemoveAllListeners();
            _interactableItem.onInteractEvent.AddListener(OnDoorInteracted);
        }

        if (doorTargetTransform != null && doorTargetTransform != transform)
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
        if (_isOpen) CloseDoor();
        else OpenDoor();
    }

    public void OpenDoor()
    {
        if (_isOpen) return;
        _isOpen = true;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveDoorRoutine(openRotation, openClipName));
    }

    public void CloseDoor()
    {
        if (!_isOpen) return;
        _isOpen = false;
        if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
        _moveCoroutine = StartCoroutine(MoveDoorRoutine(closedRotation, closeClipName));
    }

    private IEnumerator MoveDoorRoutine(Vector3 targetRot, string clipName)
    {
        if (_doorColliders != null)
        {
            foreach (Collider col in _doorColliders)
            {
                if (!col.isTrigger) col.enabled = false;
            }
        }

        if (_interactableItem != null) _interactableItem.isInteractable = false;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(clipName))
        {
            AudioManager.Instance.PlayGlobal2D(clipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.15f, transform.position);

        Quaternion startRot = doorTargetTransform.localRotation;
        Quaternion endRot = Quaternion.Euler(targetRot);
        float timer = 0f;

        while (timer < moveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / moveDuration);
            doorTargetTransform.localRotation = Quaternion.Slerp(startRot, endRot, t);
            yield return null;
        }
        doorTargetTransform.localRotation = endRot;

        if (_doorColliders != null)
        {
            foreach (Collider col in _doorColliders)
            {
                if (!col.isTrigger) col.enabled = true;
            }
        }

        if (_interactableItem != null) _interactableItem.isInteractable = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            OpenDoor();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy"))
        {
            CloseDoor();
        }
    }
}
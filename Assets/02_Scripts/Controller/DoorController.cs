using System.Collections;
using UnityEngine;

// InteractableItem 상속
public class DoorController : InteractableItem
{
    [Header("Door Settings")]
    public Transform doorTargetTransform;
    public float moveDuration = 0.5f;

    [Header("Swinging Settings")]
    public Vector3 closedRotation = Vector3.zero;
    public Vector3 openRotation = new Vector3(0, 90f, 0);

    [Header("Audio Clip Names")]
    public string openClipName = "SND-018_DoorOpen_OneShot";
    public string closeClipName = "SND-019_DoorClose_OneShot";

    private Collider[] _doorColliders;
    private bool _isOpen = false;
    private Coroutine _moveCoroutine;

    private void Start()
    {
        interactOnlyOnce = false;
        _doorColliders = GetComponents<Collider>();

        if (doorTargetTransform != null && doorTargetTransform != transform)
        {
            transform.SetParent(doorTargetTransform, true);
        }
        else if (doorTargetTransform == null)
        {
            doorTargetTransform = transform;
        }
    }

    public override void OnInteract()
    {
        base.OnInteract();
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

        isInteractable = false;

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

        isInteractable = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Enemy")) OpenDoor();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Enemy")) CloseDoor();
    }
}
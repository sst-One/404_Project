using System.Collections;
using UnityEngine;

public enum DoorType { Swinging, Sliding }

[RequireComponent(typeof(InteractableItem))]
public class DoorController : MonoBehaviour
{
    [Header("Door Settings")]
    public DoorType doorType = DoorType.Swinging;
    public Transform doorTargetTransform; // 회전축(Hinge) 또는 이동할 3D 모델
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

    private InteractableItem interactableItem;
    private bool isOpen = false;
    private bool isMoving = false;

    private void Start()
    {
        interactableItem = GetComponent<InteractableItem>();
        if (interactableItem != null)
        {
            interactableItem.onInteractEvent.RemoveAllListeners();
            interactableItem.onInteractEvent.AddListener(OnDoorInteracted);
        }

        if (doorTargetTransform == null)
            doorTargetTransform = transform;
    }

    private void OnDoorInteracted()
    {
        if (isMoving) return;
        StartCoroutine(ToggleDoorRoutine());
    }

    private IEnumerator ToggleDoorRoutine()
    {
        isMoving = true;
        isOpen = !isOpen;

        // 사운드 재생
        if (doorAudioSource != null && AudioManager.Instance != null)
        {
            string clipName = isOpen ? openClipName : closeClipName;
            AudioClip clip = AudioManager.Instance.GetClip(clipName);
            if (clip != null) doorAudioSource.PlayOneShot(clip);
        }

        // 소음 발생 (추격전 중 적에게 위치 노출)
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.15f, transform.position);
        }

        float timer = 0f;

        if (doorType == DoorType.Swinging)
        {
            Quaternion startRot = doorTargetTransform.localRotation;
            Quaternion endRot = Quaternion.Euler(isOpen ? openRotation : closedRotation);

            while (timer < moveDuration)
            {
                timer += Time.deltaTime;
                doorTargetTransform.localRotation = Quaternion.Slerp(startRot, endRot, timer / moveDuration);
                yield return null;
            }
            doorTargetTransform.localRotation = endRot;
        }
        else if (doorType == DoorType.Sliding)
        {
            Vector3 startPos = doorTargetTransform.localPosition;
            Vector3 endPos = isOpen ? openPosition : closedPosition;

            while (timer < moveDuration)
            {
                timer += Time.deltaTime;
                doorTargetTransform.localPosition = Vector3.Lerp(startPos, endPos, timer / moveDuration);
                yield return null;
            }
            doorTargetTransform.localPosition = endPos;
        }

        isMoving = false;
    }
}
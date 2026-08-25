using System.Collections;
using UnityEngine;

public class ElevatorDoorController : MonoBehaviour
{
    [Header("Door Transforms")]
    public Transform leftDoor;
    public Transform rightDoor;

    [Header("Animation Settings")]
    public Vector3 leftDoorOpenOffset = new Vector3(-1.2f, 0, 0);
    public Vector3 rightDoorOpenOffset = new Vector3(1.2f, 0, 0);
    public float animationDuration = 4f;

    [Header("Sound Settings (SND-xxx)")]
    public string doorOpenClipName = "SND-018_DoorOpen_OneShot";
    public string doorCloseClipName = "SND-019_DoorClose_OneShot";
    public float doorCloseSoundDelay = 0f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private Coroutine currentAnimation;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializePositions();
    }

    private void InitializePositions()
    {
        if (isInitialized) return;

        if (leftDoor != null && rightDoor != null)
        {
            leftClosedPos = leftDoor.localPosition;
            rightClosedPos = rightDoor.localPosition;

            leftOpenPos = leftClosedPos + leftDoorOpenOffset;
            rightOpenPos = rightClosedPos + rightDoorOpenOffset;

            isInitialized = true;
        }
    }

    public void SetDoorsOpenImmediately()
    {
        InitializePositions();
        if (!isInitialized) return;

        leftDoor.localPosition = leftOpenPos;
        rightDoor.localPosition = rightOpenPos;
    }

    public void OpenDoors()
    {
        if (!isInitialized) return;

        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // 사운드 길이에 맞추는 오버라이드 삭제, 무조건 animationDuration 속도로 통일
        currentAnimation = StartCoroutine(DoorAnimationRoutine(true, animationDuration));

        if (!string.IsNullOrEmpty(doorOpenClipName) && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(doorOpenClipName, AudioManager.Instance.sfxMixerGroup);
        }
    }

    public void CloseDoors()
    {
        if (!isInitialized) return;

        if (currentAnimation != null) StopCoroutine(currentAnimation);

        // 사운드 길이에 맞추는 오버라이드 삭제, 무조건 animationDuration 속도로 통일
        currentAnimation = StartCoroutine(DoorAnimationRoutine(false, animationDuration));

        if (!string.IsNullOrEmpty(doorCloseClipName))
        {
            StartCoroutine(PlayDoorSoundRoutine());
        }
    }

    private IEnumerator PlayDoorSoundRoutine()
    {
        yield return new WaitForSeconds(doorCloseSoundDelay);
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(doorCloseClipName))
        {
            AudioManager.Instance.PlayGlobal2D(doorCloseClipName, AudioManager.Instance.sfxMixerGroup);
        }
    }

    private IEnumerator DoorAnimationRoutine(bool isOpening, float duration)
    {
        Vector3 leftStartPos = leftDoor.localPosition;
        Vector3 rightStartPos = rightDoor.localPosition;

        Vector3 leftTargetPos = isOpening ? leftOpenPos : leftClosedPos;
        Vector3 rightTargetPos = isOpening ? rightOpenPos : rightClosedPos;

        float distanceRatio = Vector3.Distance(leftStartPos, leftTargetPos) / Vector3.Distance(leftClosedPos, leftOpenPos);
        float currentDuration = duration * distanceRatio;

        float elapsedTime = 0f;

        if (currentDuration > 0.01f)
        {
            while (elapsedTime < currentDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = Mathf.Clamp01(elapsedTime / currentDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                leftDoor.localPosition = Vector3.Lerp(leftStartPos, leftTargetPos, smoothT);
                rightDoor.localPosition = Vector3.Lerp(rightStartPos, rightTargetPos, smoothT);

                yield return null;
            }
        }

        leftDoor.localPosition = leftTargetPos;
        rightDoor.localPosition = rightTargetPos;
    }
}
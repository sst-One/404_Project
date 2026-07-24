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
    public float animationDuration = 1.5f;

    private Vector3 leftClosedPos;
    private Vector3 rightClosedPos;
    private Vector3 leftOpenPos;
    private Vector3 rightOpenPos;

    private Coroutine currentAnimation;
    private bool isInitialized = false;

    public AudioSource doorAudioSource;
    public float doorCloseSoundDelay = 4f;

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
        currentAnimation = StartCoroutine(DoorAnimationRoutine(true));
    }

    public void CloseDoors()
    {
        if (!isInitialized) return;
        if (currentAnimation != null) StopCoroutine(currentAnimation);
        currentAnimation = StartCoroutine(DoorAnimationRoutine(false));

        if (doorAudioSource != null)
        {
            doorAudioSource.Play();
        }

        StartCoroutine(PlayDoorSoundRoutine());
    }

    private IEnumerator PlayDoorSoundRoutine()
    {
        // 지정된 시간만큼 대기 후 사운드 재생
        yield return new WaitForSeconds(doorCloseSoundDelay);
        if (doorAudioSource != null)
        {
            doorAudioSource.Play();
        }
    }

    private IEnumerator DoorAnimationRoutine(bool isOpening)
    {
        // 현재 위치를 출발점으로 설정 (중간에 끊겼을 때를 대비)
        Vector3 leftStartPos = leftDoor.localPosition;
        Vector3 rightStartPos = rightDoor.localPosition;

        Vector3 leftTargetPos = isOpening ? leftOpenPos : leftClosedPos;
        Vector3 rightTargetPos = isOpening ? rightOpenPos : rightClosedPos;

        // 전체 이동 거리 대비 남은 거리를 계산하여 애니메이션 속도 일정 유지
        float distanceRatio = Vector3.Distance(leftStartPos, leftTargetPos) / Vector3.Distance(leftClosedPos, leftOpenPos);
        float currentDuration = animationDuration * distanceRatio;

        float elapsedTime = 0f;

        // currentDuration이 0에 수렴할 경우 대비
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
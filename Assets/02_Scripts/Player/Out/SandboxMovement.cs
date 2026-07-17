using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(SandboxInput))]
public class SandboxMovement : MonoBehaviour
{
    public float moveSpeed = 3.0f;
    private bool isMoving = false;

    private CharacterController cc;
    private SandboxInput inputProcessor;

    private void Start()
    {
        cc = GetComponent<CharacterController>();
        inputProcessor = GetComponent<SandboxInput>();
    }

    private void Update()
    {
        if (isMoving) return;

        // 타겟이 유효하고 W키가 눌렸을 때 이동 코루틴 실행
        if (inputProcessor.hasValidPoint && inputProcessor.IsMoveTriggered())
        {
            StartCoroutine(MoveRoutine(inputProcessor.validHitPoint));
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        isMoving = true;

        // 이동 중 물리 엔진 간섭 차단
        cc.enabled = false;

        Vector3 startPos = transform.position;
        // Y축(높이) 강제 락킹
        Vector3 endPos = new Vector3(targetPosition.x, startPos.y, targetPosition.z);

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;
        float elapsedTime = 0f;

        if (duration > 0.01f)
        {
            while (elapsedTime < duration)
            {
                float t = Mathf.Clamp01(elapsedTime / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        transform.position = endPos;

        // 이동 종료 후 물리 엔진 복구
        cc.enabled = true;
        isMoving = false;

        Debug.Log("[Sandbox] 목표 지점 이동 완료");
    }
}
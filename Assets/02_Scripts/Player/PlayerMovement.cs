using System.Collections;
using UnityEngine;

/// <summary>
/// 역할: 입력 신호(Lean)와 목적지(Gaze) 데이터를 조합하여 물리 충돌 없이 플레이어를 이동시키는 모듈.
/// </summary>
[RequireComponent(typeof(PlayerInputProvider))]
[RequireComponent(typeof(PlayerGazeController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;

    private bool isMoving = false;

    private PlayerInputProvider inputProvider;
    private PlayerGazeController gazeController;
    private CharacterController cc;

    private void Awake()
    {
        inputProvider = GetComponent<PlayerInputProvider>();
        gazeController = GetComponent<PlayerGazeController>();
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (isMoving) return;

        // 전역 Freeze 상태라면 이동 불가 (기존 FreezeManager 연동)
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing) return;

        // 1. 유효한 바닥을 보고 있고, 2. 이동(Lean/W키) 입력이 들어왔다면
        if (gazeController.IsFloorValid && inputProvider.IsLeanTriggered)
        {
            StartCoroutine(MoveRoutine(gazeController.CurrentFloorHitPoint));
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        isMoving = true;

        // 이동 중 물리 엔진(CharacterController) 충돌 차단
        if (cc != null) cc.enabled = false;

        Vector3 startPos = transform.position;
        // Y축 높이를 현재 플레이어 높이로 강제 고정하여 지형 파고들기 차단
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

        // 이동 완료 후 물리 엔진 복원
        if (cc != null) cc.enabled = true;
        isMoving = false;

        // 이동 완료 소음 발생 (기존 StateManager 연동)
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.2f);
        }

        Debug.Log("[PlayerMovement] 안전한 이동 완료");
    }
}
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputProvider))]
[RequireComponent(typeof(PlayerGazeController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // [수정] EnemyAI가 추적할 수 있도록 싱글톤 보장
    public static PlayerMovement Instance { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 3.0f;

    // [수정] PlayerUIController에서 에러 없이 접근 가능하도록 대문자 IsMoving 프로퍼티 하나로 통일
    public bool IsMoving { get; private set; }

    private PlayerInputProvider inputProvider;
    private PlayerGazeController gazeController;
    private CharacterController cc;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        inputProvider = GetComponent<PlayerInputProvider>();
        gazeController = GetComponent<PlayerGazeController>();
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (IsMoving) return;
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing) return;

        if (gazeController.IsFloorValid && inputProvider.IsLeanTriggered)
        {
            StartCoroutine(MoveRoutine(gazeController.CurrentFloorHitPoint));
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        IsMoving = true;

        if (cc != null) cc.enabled = false;

        Vector3 startPos = transform.position;
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

        if (cc != null) cc.enabled = true;

        IsMoving = false;

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.2f);
    }
}
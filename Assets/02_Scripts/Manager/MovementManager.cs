using System.Collections;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance { get; private set; }

    public bool IsMoving { get; private set; }
    public float moveSpeed = 3.0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void MoveTo(Vector3 targetPosition)
    {
        if (IsMoving) return;
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing) return;

        StartCoroutine(MoveRoutine(targetPosition));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        IsMoving = true;
        Transform playerTransform = Camera.main.transform.root;

        // 샌드박스 검증 로직: 물리 충돌 차단
        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        Vector3 startPos = playerTransform.position;
        // Y축 락킹
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

                playerTransform.position = Vector3.Lerp(startPos, endPos, smoothT);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        playerTransform.position = endPos;

        // 이동 완료 후 물리 엔진 복원
        if (cc != null) cc.enabled = true;
        IsMoving = false;

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.2f);
        }
    }
}
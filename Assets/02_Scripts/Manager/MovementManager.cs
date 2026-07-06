using System.Collections;
using UnityEngine;

public class MovementManager : MonoBehaviour
{
    public static MovementManager Instance { get; private set; }

    public bool IsMoving { get; private set; }
    public float moveDuration = 1.5f;

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
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            Debug.Log("Freeze 상태에서는 이동할 수 없습니다.");
            return;
        }

        StartCoroutine(MoveRoutine(targetPosition));
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        IsMoving = true;

        // 샌드박스 검증용: 카메라를 직접 이동 (본 게임에서는 플레이어 루트 객체로 변경 필요)
        Transform playerTransform = Camera.main.transform;

        // 플레이어가 바닥에 파묻히지 않도록 기존 Y축 높이 유지
        Vector3 startPos = playerTransform.position;
        Vector3 endPos = new Vector3(targetPosition.x, startPos.y, targetPosition.z);

        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            playerTransform.position = Vector3.Lerp(startPos, endPos, elapsedTime / moveDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        playerTransform.position = endPos;
        IsMoving = false;

        // 이동 완료 후 시스템 연동: 소음(Noise) 증가
        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.2f);
        }

        Debug.Log("이동 완료: 소음이 발생했습니다.");
    }
}
using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;

public class PlayerInputMapper : MonoBehaviour
{
    [Header("시각적 피드백")]
    public RectTransform handCursor;

    [Header("3D 상호작용 (문 열기, 사물 조사)")]
    public Camera mainCamera;
    public LayerMask interactableLayer;

    // [TPM 핫픽스: 스레드 동기화를 위한 우체통 변수]
    private Vector2 normalizedPos; // AI가 넘겨준 0.0 ~ 1.0 비율 좌표
    private bool hasNewData = false;
    private readonly object lockObj = new object(); // 데이터 충돌 방지용 자물쇠

    private void OnEnable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked += ReceiveHandData;
    }

    private void OnDisable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked -= ReceiveHandData;
    }

    // ❌ 이 함수는 MediaPipe의 [백그라운드 스레드]에서 실행됩니다! (유니티 API 사용 절대 금지)
    private void ReceiveHandData(HandLandmarkerResult result)
    {
        if (result.handLandmarks == null || result.handLandmarks.Count == 0) return;

        var indexFinger = result.handLandmarks[0].landmarks[8];

        // 자물쇠를 걸고 순수 숫자 데이터만 우체통에 몰래 넣어둡니다.
        lock (lockObj)
        {
            normalizedPos = new Vector2(indexFinger.x, indexFinger.y);
            hasNewData = true;
        }
    }

    // 🟢 이 함수는 유니티의 [메인 스레드]에서 매 프레임 안전하게 실행됩니다.
    private void Update()
    {
        // 새로운 데이터가 안 들어왔으면 스킵
        if (!hasNewData) return;

        Vector2 targetNormPos;

        // 자물쇠를 풀고 데이터를 안전하게 꺼내옵니다.
        lock (lockObj)
        {
            targetNormPos = normalizedPos;
            hasNewData = false;
        }

        // 여기서부터는 메인 스레드이므로 Screen, UI, Transform 등 유니티 기능을 마음껏 써도 됩니다!
        float screenX = targetNormPos.x * Screen.width;
        float screenY = (1.0f - targetNormPos.y) * Screen.height;
        Vector2 screenPos = new Vector2(screenX, screenY);

        // [핵심 시스템 1] UI 커서를 내 손가락 위치로 즉각 이동
        if (handCursor != null)
        {
            handCursor.position = screenPos;
        }

        // [핵심 시스템 2] 손동작을 통한 3D 사물 조사 (Raycast)
        if (mainCamera != null)
        {
            Ray ray = mainCamera.ScreenPointToRay(screenPos);

            if (Physics.Raycast(ray, out RaycastHit hit, 10f, interactableLayer))
            {
                Debug.Log($"[Project 404] 손이 닿은 오브젝트: {hit.collider.gameObject.name}");
            }
        }
    }
}
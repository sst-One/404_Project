using UnityEngine;

/// <summary>
/// 역할: PlayerGazeController의 판정 결과를 바탕으로 시각적 UI(인디케이터 등)만 업데이트하는 모듈.
/// </summary>
[RequireComponent(typeof(PlayerGazeController))]
public class PlayerUIController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("바닥 이동 가능 구역을 표시할 인디케이터 오브젝트 (콜라이더 제거 필수)")]
    public GameObject moveUIIndicator;

    private PlayerGazeController gazeController;

    private void Awake()
    {
        gazeController = GetComponent<PlayerGazeController>();
    }

    private void Start()
    {
        if (moveUIIndicator != null) moveUIIndicator.SetActive(false);
    }

    private void Update()
    {
        UpdateFloorIndicator();
    }

    private void UpdateFloorIndicator()
    {
        if (moveUIIndicator == null) return;

        // GazeController가 "지금 보는 곳이 유효한 바닥이다"라고 판정했다면
        if (gazeController.IsFloorValid)
        {
            // 인디케이터를 활성화하고, 판정된 바닥 좌표보다 아주 살짝 위(Z-Fighting 방지)에 렌더링
            moveUIIndicator.transform.position = gazeController.CurrentFloorHitPoint + Vector3.up * 0.05f;

            if (!moveUIIndicator.activeSelf)
            {
                moveUIIndicator.SetActive(true);
            }
        }
        else
        {
            // 허공을 보거나 오브젝트를 보면 즉시 끔
            if (moveUIIndicator.activeSelf)
            {
                moveUIIndicator.SetActive(false);
            }
        }
    }
}
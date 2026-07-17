using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class SandboxInput : MonoBehaviour
{
    [Header("Dependencies")]
    public Camera mainCamera;
    public LayerMask floorLayer;
    public GameObject indicatorUI;

    [HideInInspector] public Vector3 validHitPoint;
    [HideInInspector] public bool hasValidPoint = false;

    private void Update()
    {
        if (mainCamera == null) return;

        // 마우스 시선 기반 레이캐스트 발사
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 15f, floorLayer))
        {
            // NavMesh 위인지 검증
            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 0.5f, NavMesh.AllAreas))
            {
                validHitPoint = navHit.position;
                hasValidPoint = true;

                if (indicatorUI != null)
                {
                    indicatorUI.SetActive(true);
                    // 바닥 파고들지 않게 UI 살짝 띄움
                    indicatorUI.transform.position = validHitPoint + Vector3.up * 0.05f;
                }
            }
            else
            {
                DisableIndicator();
            }
        }
        else
        {
            DisableIndicator();
        }
    }

    private void DisableIndicator()
    {
        hasValidPoint = false;
        if (indicatorUI != null) indicatorUI.SetActive(false);
    }

    // W키 입력 반환
    public bool IsMoveTriggered()
    {
        return Keyboard.current != null && Keyboard.current.wKey.wasPressedThisFrame;
    }
}
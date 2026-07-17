using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.AI;

public class MovementDiagnosticTool : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask floorLayer;

    private void Update()
    {
        if (mainCamera == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = mainCamera.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 20f, floorLayer))
        {
            // 1. 레이캐스트가 닿은 순수 위치 (빨간색 선)
            Debug.DrawLine(mainCamera.transform.position, hit.point, Color.red);

            if (NavMesh.SamplePosition(hit.point, out NavMeshHit navHit, 1.0f, NavMesh.AllAreas))
            {
                // 2. NavMesh가 인식한 실제 이동 가능 바닥 위치 (녹색 선)
                Debug.DrawLine(hit.point, navHit.position + Vector3.up * 0.5f, Color.green);

                if (Keyboard.current.wKey.wasPressedThisFrame)
                {
                    Debug.Log("마우스 스크린 좌표: " + mousePos);
                    Debug.Log("레이캐스트 충돌 좌표: " + hit.point);
                    Debug.Log("NavMesh 보정 좌표: " + navHit.position);
                }
            }
        }
        else
        {
            // 바닥을 인식하지 못할 경우 허공으로 나가는 레이캐스트 (노란색 선)
            Debug.DrawRay(mainCamera.transform.position, ray.direction * 20f, Color.yellow);
        }
    }
}
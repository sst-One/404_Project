using UnityEngine;
using UnityEngine.InputSystem;

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("회전 대상 설정")]
    public Transform playerBody;

    [Header("마우스 회전 감도 (우클릭 드래그 시)")]
    public float mouseSensitivity = 10f;
    private float _xRotation = 0f;

    private void Start()
    {
        // 마우스를 창 내부에 엄격히 가두되, 커서는 보이게 설정
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
    }

    private void Update()
    {
        float rotX = 0f;
        float rotY = 0f;

        // [핵심 수정] 엣지 패닝 자동 회전 로직 전면 삭제. 
        // 오직 명시적 조작(우클릭 드래그) 시에만 시야가 회전하도록 제한.
        if (Mouse.current != null && Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            rotX = mouseDelta.y * mouseSensitivity * Time.deltaTime;
            rotY = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        }

        // Freeze 상태 시 회전 속도 보정
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            rotX *= 0.2f;
            rotY *= 0.2f;
        }

        // 카메라 상하 회전 (X축)
        _xRotation -= rotX;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // 플레이어 몸통 좌우 회전 (Y축)
        if (playerBody != null && rotY != 0f)
        {
            playerBody.Rotate(Vector3.up * rotY);
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        // 에디터/창 모드 전환 시 커서 락킹 유실 방지
        if (hasFocus) Cursor.lockState = CursorLockMode.Confined;
    }
}
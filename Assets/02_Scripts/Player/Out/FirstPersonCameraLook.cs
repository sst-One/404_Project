using UnityEngine;
using UnityEngine.InputSystem; // [TPM 핫픽스: 신규 Input System 네임스페이스 추가]

public class FirstPersonCameraLook : MonoBehaviour
{
    [Header("회전 대상 설정")]
    [Tooltip("좌우 회전을 적용할 플레이어 몸통(Root) 객체")]
    public Transform playerBody;

    [Header("마우스 테스트 모드 (우클릭 드래그로 시야 회전)")]
    [Tooltip("신규 Input System의 Delta 픽셀값을 사용하므로 감도를 낮게(예: 5~15) 설정하세요.")]
    public float mouseSensitivity = 10f; // 기본값 대폭 하향 조정

    [Header("비전 AI 엣지 패닝 (손이 화면 끝에 갈 때 회전)")]
    public float edgePanThreshold = 0.15f;
    public float aiEdgePanSpeed = 70f;

    private float _xRotation = 0f;

    private void Update()
    {
        if (InputManager.Instance == null) return;

        float rotX = 0f;
        float rotY = 0f;

        // 1. 현재 입력 모드가 Vision AI인지 확인
        bool isVisionAI = InputManager.Instance.GetInput() is PlayerInputMapper;

        if (!isVisionAI)
        {
            // [마우스 테스트 모드 - 신규 Input System 적용]
            // 마우스 객체가 존재하고 우클릭(rightButton)이 눌려 있는지 확인
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
            {
                // 마우스 이동 픽셀 델타값 읽기
                Vector2 mouseDelta = Mouse.current.delta.ReadValue();

                rotX = mouseDelta.y * mouseSensitivity * Time.deltaTime;
                rotY = mouseDelta.x * mouseSensitivity * Time.deltaTime;
            }
        }
        else
        {
            // [비전 AI 모드] 카메라 기반 엣지 패닝 로직 (보존)
            Vector3 handPos = InputManager.Instance.GetInput().GetHandAimTarget();
            float normalizedX = handPos.x / Screen.width;
            float normalizedY = handPos.y / Screen.height;

            // 좌우 회전
            if (normalizedX < edgePanThreshold) rotY = -aiEdgePanSpeed * Time.deltaTime;
            else if (normalizedX > 1f - edgePanThreshold) rotY = aiEdgePanSpeed * Time.deltaTime;

            // 상하 회전
            if (normalizedY < edgePanThreshold) rotX = aiEdgePanSpeed * Time.deltaTime;
            else if (normalizedY > 1f - edgePanThreshold) rotX = -aiEdgePanSpeed * Time.deltaTime;
        }

        // 2. Freeze 상태일 때 시야 회전 제한
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing)
        {
            rotX *= 0.2f;
            rotY *= 0.2f;
        }

        // 3. 카메라 상하 회전 (X축) 처리 및 제한 (-90도 ~ 90도)
        _xRotation -= rotX;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);

        // 4. 몸통 좌우 회전 (Y축) 처리
        if (playerBody != null && rotY != 0f)
        {
            playerBody.Rotate(Vector3.up * rotY);
        }
    }
}
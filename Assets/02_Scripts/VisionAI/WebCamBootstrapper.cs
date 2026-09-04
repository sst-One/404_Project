using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using UnityEngine;

public class WebCamBootstrapper : MonoBehaviour
{
    private void Start()
    {
        // Awake에서 자동 실행하지 않고, VisionTrackingManager가 준비되면 이벤트를 구독
        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.OnCameraConfirmed += InitializeCamera;
        }
    }

    private void OnDestroy()
    {
        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.OnCameraConfirmed -= InitializeCamera;
        }
    }

    private void InitializeCamera(string selectedDeviceName)
    {
        Debug.Log($"[WebCamBootstrapper] 유저가 선택한 카메라 초기화 시작: {selectedDeviceName}");

        WebCamDevice[] devices = WebCamTexture.devices;
        int targetIndex = -1;

        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].name == selectedDeviceName)
            {
                targetIndex = i;
                break;
            }
        }

        if (targetIndex == -1)
        {
            Debug.LogError($"[WebCamBootstrapper] 기기를 찾을 수 없어 0번 인덱스로 Fallback합니다.");
            targetIndex = 0;
        }

        var dummyResolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(1280, 720, 30)
        };

        WebCamSource webCamSource = new WebCamSource(1280, dummyResolutions);
        webCamSource.SelectSource(targetIndex);

        ImageSourceProvider.ImageSource = webCamSource;
        Debug.Log("[WebCamBootstrapper] MediaPipe ImageSourceProvider에 카메라 할당 완료.");
    }
}
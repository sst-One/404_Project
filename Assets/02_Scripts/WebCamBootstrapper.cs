using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using UnityEngine;

public class WebCamBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        Debug.Log("[WebCamBootstrapper] 본 씬 웹캠 부트스트래핑 시작 (OBS 브릿지 연동)");

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[WebCamBootstrapper] 인식된 카메라가 없습니다. OBS 가상 카메라가 켜져 있는지 확인하십시오.");
            return;
        }

        int targetIndex = -1;

        // OBS 가상 카메라 장치명 정밀 탐색
        for (int i = 0; i < devices.Length; i++)
        {
            string lowerName = devices[i].name.ToLower();
            if (lowerName.Contains("obs") || lowerName.Contains("virtual"))
            {
                targetIndex = i;
                Debug.Log($"[WebCamBootstrapper] OBS 가상 카메라 매칭 성공 [{i}번 슬롯]: {devices[i].name}");
                break;
            }
        }

        if (targetIndex == -1)
        {
            Debug.LogError($"[WebCamBootstrapper] OBS 가상 카메라를 찾을 수 없습니다. 현재 인식된 1순위 장치: {devices[0].name}");
            return;
        }

        // [핵심 핫픽스] 해상도를 강제 주입하지 않고 네이티브 위임 방식을 사용하는 기본 생성자 호출
        var dummyResolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(1280, 720, 30)
        };

        WebCamSource webCamSource = new WebCamSource(1280, dummyResolutions);

        // 탐색된 정확한 OBS 인덱스를 MediaPipe에 강제 주입
        webCamSource.SelectSource(targetIndex);

        // 전역 프로바이더에 할당하여 Vision AI 입력망 가동
        ImageSourceProvider.ImageSource = webCamSource;
        Debug.Log("[WebCamBootstrapper] MediaPipe ImageSourceProvider에 OBS 브릿지 할당 완료. 비전 AI 입력망이 활성화됩니다.");
    }
}
using UnityEngine;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;

public class WebCamBootstrapper : MonoBehaviour
{
    private void Awake()
    {
        // 1. 웹캠 해상도 강제 지정 (성능 방어 및 프레임 확보를 위해 640x480 30fps 세팅)
        var resolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(640, 480, 30)
        };

        // 2. MonoBehaviour가 아닌 순수 C# 객체 메모리 할당
        WebCamSource webCamSource = new WebCamSource(640, resolutions);
        
        // 3. MediaPipe 플러그인이 참조하는 전역 변수에 웹캠 소스 강제 주입
        ImageSourceProvider.ImageSource = webCamSource;

        Debug.Log("[WebCamBootstrapper] 웹캠 소스 메모리 생성 및 전역 할당 완료.");
    }
}
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using UnityEngine;
using System.Collections;

public class WebCamBootstrapper : MonoBehaviour
{
    private void Start()
    {
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
        StartCoroutine(SwitchCameraRoutine(selectedDeviceName));
    }

    private IEnumerator SwitchCameraRoutine(string selectedDeviceName)
    {
        Debug.Log($"[WebCamBootstrapper] 유저가 선택한 카메라 초기화 시작: {selectedDeviceName}");

        // 1. 기존 카메라 강제 종료 (메모리 해제 및 먹통 데드락 방지 핵심)
        if (ImageSourceProvider.ImageSource != null && ImageSourceProvider.ImageSource.isPlaying)
        {
            ImageSourceProvider.ImageSource.Stop();
            Debug.Log("[WebCamBootstrapper] 기존 카메라 프로세스 종료 완료.");
        }

        // 2. 장치 검색
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
            Debug.LogWarning($"[WebCamBootstrapper] 기기를 찾을 수 없어 0번 인덱스로 Fallback합니다.");
            targetIndex = 0;
        }

        // 3. 기획자님 원본 정답 코드 복구: C# 순수 객체로 WebCamSource 인스턴스화
        var dummyResolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(1280, 720, 30)
        };

        WebCamSource webCamSource = new WebCamSource(1280, dummyResolutions);

        // 플러그인 내부의 강제 OBS 하드코딩을 덮어쓰기 위해 Index 할당
        webCamSource.SelectSource(targetIndex);

        // 4. Provider 갱신
        ImageSourceProvider.ImageSource = webCamSource;
        Debug.Log("[WebCamBootstrapper] MediaPipe ImageSourceProvider에 새 카메라 할당 완료.");

        // 5. 엔진에 플레이 명령 강제 하달 (Mac/Windows 미송출 해결의 핵심)
        yield return ImageSourceProvider.ImageSource.Play();
        Debug.Log("[WebCamBootstrapper] 물리 렌즈 가동 성공!");
    }
}
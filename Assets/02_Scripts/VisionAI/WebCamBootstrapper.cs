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

        if (ImageSourceProvider.ImageSource != null && ImageSourceProvider.ImageSource.isPlaying)
        {
            ImageSourceProvider.ImageSource.Stop();
            Debug.Log("[WebCamBootstrapper] 기존 카메라 프로세스 종료 완료.");
        }

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

        var dummyResolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(1280, 720, 30)
        };

        WebCamSource webCamSource = new WebCamSource(1280, dummyResolutions);
        webCamSource.SelectSource(targetIndex);

        ImageSourceProvider.ImageSource = webCamSource;
        Debug.Log("[WebCamBootstrapper] MediaPipe ImageSourceProvider에 새 카메라 할당 완료.");

        // 렌즈 가동
        yield return ImageSourceProvider.ImageSource.Play();
        Debug.Log("[WebCamBootstrapper] 물리 렌즈 가동 성공!");

        yield return new WaitForSeconds(0.5f); // 텍스처 버퍼 안정화를 위한 미세 대기

        // [핵심 픽스] 잠들어 있는 AI 분석 엔진(Solution)을 찾아 강제로 깨웁니다.
        var allScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script.GetType().Name.EndsWith("Solution"))
            {
                var playMethod = script.GetType().GetMethod("Play");
                if (playMethod != null)
                {
                    playMethod.Invoke(script, null);
                    Debug.Log($"[WebCamBootstrapper] AI 분석 엔진({script.GetType().Name}) 가동 완료! 트래킹 시작!");
                }
            }
        }
    }
}
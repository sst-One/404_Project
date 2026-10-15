using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

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
        Debug.Log($"[WebCamBootstrapper] 카메라 전환 프로세스 시작: {selectedDeviceName}");

        List<MonoBehaviour> activeAIEngines = new List<MonoBehaviour>();
        var allScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            string scriptName = script.GetType().Name;
            if (scriptName.EndsWith("Solution") || scriptName.EndsWith("Runner"))
            {
                activeAIEngines.Add(script);
            }
        }

        // 모든 엔진 Stop
        foreach (var engine in activeAIEngines)
        {
            try
            {
                var stopMethod = engine.GetType().GetMethod("Stop");
                stopMethod?.Invoke(engine, null);
            }
            catch (Exception) { }
        }
        yield return new WaitForSeconds(0.3f);

        // 하드웨어 Stop
        try
        {
            if (ImageSourceProvider.ImageSource != null && ImageSourceProvider.ImageSource.isPlaying)
            {
                ImageSourceProvider.ImageSource.Stop();
            }
        }
        catch (Exception) { }
        yield return new WaitForSeconds(0.2f);

        // 물리 기기 인덱스 할당
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
        if (targetIndex == -1) targetIndex = 0;

        // 순수 객체 주입 (수동 Play 절대 금지)
        var dummyResolutions = new ImageSource.ResolutionStruct[] {
            new ImageSource.ResolutionStruct(1280, 720, 30)
        };
        WebCamSource webCamSource = new WebCamSource(1280, dummyResolutions);
        webCamSource.SelectSource(targetIndex);
        ImageSourceProvider.ImageSource = webCamSource;
        Debug.Log($"[WebCamBootstrapper] 하드웨어 인덱스({targetIndex}) 주입 완료. (AI 주도 부팅 대기)");

        // [핵심 픽스] 메인 AI 단독 선행 기동 및 서브 AI 순차 합류 아키텍처
        if (activeAIEngines.Count > 0)
        {
            // 1. 첫 번째 AI에게 카메라 기동 및 텍스처 장악 권한 위임 (트래킹 정상화의 핵심)
            var firstEngine = activeAIEngines[0];
            var playMethod1 = firstEngine.GetType().GetMethod("Play");
            playMethod1?.Invoke(firstEngine, null);
            Debug.Log($"[WebCamBootstrapper] 메인 AI 엔진({firstEngine.GetType().Name}) 선행 가동. 카메라 제어 위임.");

            // 2. 메인 AI가 카메라를 성공적으로 켜고 텍스처를 할당할 때까지 대기 (크래시 방어)
            yield return new WaitUntil(() =>
                ImageSourceProvider.ImageSource != null &&
                ImageSourceProvider.ImageSource.GetCurrentTexture() != null &&
                ImageSourceProvider.ImageSource.GetCurrentTexture().width > 16
            );
            yield return new WaitForSeconds(0.5f); // 텍스처 버퍼 완벽 안정화 대기

            // 3. 나머지 AI 순차 합류
            for (int i = 1; i < activeAIEngines.Count; i++)
            {
                var subEngine = activeAIEngines[i];
                var playMethodSub = subEngine.GetType().GetMethod("Play");
                playMethodSub?.Invoke(subEngine, null);
                Debug.Log($"[WebCamBootstrapper] 서브 AI 엔진({subEngine.GetType().Name}) 합류 가동 완료.");
                yield return new WaitForSeconds(0.5f);
            }
        }
        else
        {
            yield return ImageSourceProvider.ImageSource.Play();
        }
    }
}
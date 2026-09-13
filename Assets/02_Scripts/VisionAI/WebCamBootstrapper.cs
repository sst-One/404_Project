using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using System.Collections;
using UnityEngine;

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

        // 1. 잠들어 있는 AI 분석 엔진(Solution)을 먼저 찾습니다.
        MonoBehaviour solutionScript = null;
        var allScripts = FindObjectsOfType<MonoBehaviour>();
        foreach (var script in allScripts)
        {
            if (script.GetType().Name.EndsWith("Solution"))
            {
                solutionScript = script;
                break;
            }
        }

        // 2. 기존 프로세스 완벽 종료 (데드락 방지)
        if (solutionScript != null)
        {
            var stopMethod = solutionScript.GetType().GetMethod("Stop");
            stopMethod?.Invoke(solutionScript, null);
            yield return new WaitForSeconds(0.2f); // 엔진 쿨다운
        }
        else if (ImageSourceProvider.ImageSource != null && ImageSourceProvider.ImageSource.isPlaying)
        {
            ImageSourceProvider.ImageSource.Stop();
        }

        // 3. 유저가 선택한 물리 기기 인덱스 탐색
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

        // 4. [가장 핵심] 유령 객체(new WebCamSource) 생성 코드를 완전히 삭제했습니다!
        // 팀원이 찾아낸 정답대로, 씬에 안전하게 등록되어 있는 진짜 ImageSource를 직접 통제합니다.
        if (ImageSourceProvider.ImageSource != null)
        {
            ImageSourceProvider.ImageSource.SelectSource(targetIndex);
            Debug.Log($"[WebCamBootstrapper] 기존 ImageSource에 실제 카메라 인덱스({targetIndex}) 덮어쓰기 완료.");
        }
        else
        {
            Debug.LogError("[WebCamBootstrapper] 씬에 ImageSource가 없습니다! MediaPipe 설정을 확인하세요.");
            yield break; // 파이프라인 단절 시 안전하게 실행 취소
        }

        // 5. 물리 카메라 렌즈 단독 가동이 아닌, AI 두뇌를 가동하여 렌즈와 트래킹을 동시 시작시킵니다.
        if (solutionScript != null)
        {
            var playMethod = solutionScript.GetType().GetMethod("Play");
            if (playMethod != null)
            {
                playMethod.Invoke(solutionScript, null);
                Debug.Log($"[WebCamBootstrapper] AI 분석 엔진({solutionScript.GetType().Name}) 가동 완료! 트래킹 정상화!");
            }
        }
        else
        {
            // Fallback: 솔루션을 못 찾을 경우 렌즈라도 강제 가동
            yield return ImageSourceProvider.ImageSource.Play();
        }
    }
}
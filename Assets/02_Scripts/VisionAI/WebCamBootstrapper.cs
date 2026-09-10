using UnityEngine;
using System.Collections;

public class WebCamBootstrapper : MonoBehaviour
{
    [Header("카메라 제어")]
    [Tooltip("씬에 배치된 진짜 WebCamSource 스크립트를 여기에 끌어다 놓으세요.")]
    public MonoBehaviour targetWebCamSource;

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
        if (targetWebCamSource == null)
        {
            Debug.LogError("[WebCamBootstrapper] 인스펙터에 타겟 카메라(WebCamSource)가 연결되지 않았습니다!");
            return;
        }

        StartCoroutine(SwitchCameraRoutine(selectedDeviceName));
    }

    private IEnumerator SwitchCameraRoutine(string selectedDeviceName)
    {
        Debug.Log($"[WebCamBootstrapper] 유저가 선택한 카메라 초기화 시작: {selectedDeviceName}");

        // 1. 기존에 잘못 물리거나 먹통인 카메라가 있다면 확실하게 죽입니다.
        var isPlayingProp = targetWebCamSource.GetType().GetProperty("isPlaying");
        if (isPlayingProp != null && (bool)isPlayingProp.GetValue(targetWebCamSource))
        {
            var stopMethod = targetWebCamSource.GetType().GetMethod("Stop");
            stopMethod?.Invoke(targetWebCamSource, null);
            yield return new WaitUntil(() => !(bool)isPlayingProp.GetValue(targetWebCamSource));
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
            Debug.LogError($"[WebCamBootstrapper] 기기를 찾을 수 없어 0번 인덱스로 Fallback합니다.");
            targetIndex = 0;
        }

        // 2. 타겟 컴포넌트에 새 기기 번호(Index)를 주입합니다.
        var selectSourceMethod = targetWebCamSource.GetType().GetMethod("SelectSource");
        if (selectSourceMethod != null)
        {
            selectSourceMethod.Invoke(targetWebCamSource, new object[] { targetIndex });
            Debug.Log("[WebCamBootstrapper] 타겟 카메라 디바이스 인덱스 할당 완료.");
        }

        // 3. 엔진에 플레이 명령 강제 하달
        var playMethod = targetWebCamSource.GetType().GetMethod("Play");
        if (playMethod != null)
        {
            IEnumerator playCoroutine = playMethod.Invoke(targetWebCamSource, null) as IEnumerator;
            if (playCoroutine != null)
            {
                yield return StartCoroutine(playCoroutine);
                Debug.Log("[WebCamBootstrapper] 물리 렌즈 가동 성공!");
            }
        }
    }
}
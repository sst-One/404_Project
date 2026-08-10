using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class OBSWebCamTest : MonoBehaviour
{
    [Header("UI 렌더 타겟")]
    public RawImage display;

    private WebCamTexture camTex;

    private IEnumerator Start()
    {
        Debug.Log("[OBS 격리 테스트] OBS 가상 카메라 탐색 시작");

        WebCamDevice[] devices = WebCamTexture.devices;
        if (devices.Length == 0)
        {
            Debug.LogError("[OBS 격리 테스트] 장치 없음. 운영체제에서 카메라를 인식하지 못했습니다.");
            yield break;
        }

        string targetDeviceName = "";
        for (int i = 0; i < devices.Length; i++)
        {
            string lowerName = devices[i].name.ToLower();
            if (lowerName.Contains("obs") || lowerName.Contains("virtual"))
            {
                targetDeviceName = devices[i].name;
                break;
            }
        }

        if (string.IsNullOrEmpty(targetDeviceName))
        {
            Debug.LogError("[OBS 격리 테스트] OBS 가상 카메라를 찾을 수 없습니다. OBS에서 '가상 카메라 시작'이 켜져 있는지 확인하십시오.");
            yield break;
        }

        Debug.Log($"[OBS 격리 테스트] 연결 대상 확정: {targetDeviceName}");

        // 해상도와 프레임레이트를 강제하는 파라미터를 완전히 삭제합니다.
        // OBS가 송출하는 네이티브 스트림 규격을 유니티가 자동으로 동기화하여 수신합니다.
        camTex = new WebCamTexture(targetDeviceName);
        display.texture = camTex;
        camTex.Play();

        int waitFrameCount = 0;

        while (!camTex.didUpdateThisFrame)
        {
            waitFrameCount++;

            if (waitFrameCount > 300)
            {
                Debug.LogError("[OBS 격리 테스트] 5초 경과. 연결은 성공했으나 OBS로부터 영상 데이터를 받지 못했습니다.");
                yield break;
            }

            yield return new WaitForEndOfFrame();
        }

        Debug.Log($"[OBS 격리 테스트] 영상 수신 성공! 실제 렌더링 해상도: {camTex.width} x {camTex.height}");
    }

    private void OnDestroy()
    {
        if (camTex != null && camTex.isPlaying)
        {
            camTex.Stop();
        }
    }
}
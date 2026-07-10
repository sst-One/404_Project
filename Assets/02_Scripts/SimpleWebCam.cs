using UnityEngine;
using UnityEngine.UI;

public class SimpleWebCam : MonoBehaviour
{
    [Header("화면을 출력할 RawImage를 여기에 드래그하세요")]
    public RawImage display;

    void Start()
    {
        // PC에 연결된 모든 카메라 장치 목록을 가져옵니다.
        WebCamDevice[] devices = WebCamTexture.devices;

        if (devices.Length == 0)
        {
            Debug.LogError("[WebCamTest] 연결된 카메라 장치가 단 하나도 없습니다! 윈도우 OS 권한을 확인하세요.");
            return;
        }

        for (int i = 0; i < devices.Length; i++)
        {
            Debug.Log("[WebCamTest] 유니티가 인식한 장치명: " + devices[i].name);

            // 이름에 "Iriun"이 포함된 가상 웹캠을 찾으면 즉시 해당 장치로 연결합니다.
            if (devices[i].name.Contains("Iriun"))
            {
                WebCamTexture tex = new WebCamTexture(devices[i].name);
                if (display != null)
                {
                    display.texture = tex;
                }
                tex.Play();
                Debug.Log("[WebCamTest] Iriun 가상 웹캠 강제 연결 및 재생 성공!");
                return;
            }
        }

        Debug.LogWarning("[WebCamTest] Iriun 웹캠을 찾지 못했습니다. 목록의 첫 번째 카메라를 강제로 켭니다.");
        WebCamTexture fallbackTex = new WebCamTexture(devices[0].name);
        if (display != null) display.texture = fallbackTex;
        fallbackTex.Play();
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Mediapipe.Unity.Tutorial
{
    public class FaceMeshLegacy : MonoBehaviour
    {
        [SerializeField] private TextAsset configAsset;
        [SerializeField] private RawImage screen;
        private WebCamTexture webCamTexture;

        private IEnumerator Start()
        {
            Debug.Log("[TPM 핫픽스] Unity Remote 5 파이프라인 가동. OS 드라이버를 우회합니다.");

            // 유니티 리모트가 장치 0번을 강제로 하이재킹하므로, 이름 검사 없이 기본값으로 초기화합니다.
            webCamTexture = new WebCamTexture();
            screen.texture = webCamTexture;
            webCamTexture.Play();

            // 화면 출력 대기
            yield return new WaitUntil(() => webCamTexture.width > 16);

            screen.rectTransform.sizeDelta = new Vector2(webCamTexture.width, webCamTexture.height);
            Debug.Log($"[TPM 핫픽스] Unity Remote 5 연결 대성공! 해상도: {webCamTexture.width}x{webCamTexture.height}");
        }

        private void OnDestroy()
        {
            if (webCamTexture != null) webCamTexture.Stop();
        }
    }
}
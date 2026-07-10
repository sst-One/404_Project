using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video; // 비디오 플레이어 기능 추가

namespace Mediapipe.Unity.Tutorial
{
    public class FaceLandmarkerRunner : MonoBehaviour
    {
        [SerializeField] private TextAsset configAsset;
        [SerializeField] private RawImage screen;

        [Header("테스트용 동영상 파일을 여기에 넣으세요")]
        [SerializeField] private VideoClip mockVideo;

        private VideoPlayer videoPlayer;

        private IEnumerator Start()
        {
            Debug.Log("[TPM 결단] 하드웨어 통신을 포기하고 Plan C (가짜 데이터 주입)로 선회합니다.");

            if (mockVideo == null)
            {
                Debug.LogError("[Plan C 블로커] Inspector 창에서 mockVideo 칸에 동영상 파일을 드래그해서 넣어주세요!");
                yield break;
            }

            // 비디오 플레이어 컴포넌트를 코드로 생성
            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = mockVideo;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            videoPlayer.isLooping = true; // 무한 반복 재생

            // 비디오 메모리 로딩 대기
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
            {
                yield return null;
            }

            // 비디오 프레임을 화면(RawImage)에 직결
            screen.texture = videoPlayer.texture;
            screen.rectTransform.sizeDelta = new Vector2(videoPlayer.texture.width, videoPlayer.texture.height);

            // 재생 시작
            videoPlayer.Play();
            Debug.Log($"[Plan C 성공] 비디오 스트림 가동! 해상도: {videoPlayer.texture.width}x{videoPlayer.texture.height}");
        }

        private void OnDestroy()
        {
            if (videoPlayer != null) videoPlayer.Stop();
        }
    }
}
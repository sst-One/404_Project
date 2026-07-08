// Copyright (c) 2023 homuler
// ... (라이선스 생략)

using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Video; // [TPM 추가: 비디오 플레이어 모듈]

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
    public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
    {
        [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;

        [Header("테스트용 동영상 (Plan C)")]
        [SerializeField] private VideoClip mockVideo; // [TPM 추가: 인스펙터 노출]

        private VideoPlayer videoPlayer; // [TPM 추가]
        private Experimental.TextureFramePool _textureFramePool;

        public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
            if (videoPlayer != null) { videoPlayer.Stop(); }
        }

        protected override IEnumerator Run()
        {
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);
            var options = config.GetHandLandmarkerOptions(config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnHandLandmarkDetectionOutput : null);
            taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

            // ====== [TPM 핫픽스: 카메라 셧다운 및 비디오 플레이어 강제 할당] ======
            if (mockVideo == null)
            {
                Debug.LogError("[Plan C 블로커] 인스펙터의 Mock Video 칸에 동영상을 넣어주세요!");
                yield break;
            }

            videoPlayer = gameObject.AddComponent<VideoPlayer>();
            videoPlayer.playOnAwake = false;
            videoPlayer.clip = mockVideo;
            videoPlayer.renderMode = VideoRenderMode.APIOnly;
            videoPlayer.isLooping = true;
            videoPlayer.Prepare();

            while (!videoPlayer.isPrepared) yield return null;
            videoPlayer.Play();
            // =============================================================

            _textureFramePool = new Experimental.TextureFramePool((int)videoPlayer.width, (int)videoPlayer.height, TextureFormat.RGBA32, 10);

            // ====== [TPM 핫픽스: 화면 출력 및 상하반전(Flip) 해결] ======
            var rawImage = screen.GetComponent<UnityEngine.UI.RawImage>();
            if (rawImage != null)
            {
                rawImage.texture = videoPlayer.texture;
                rawImage.rectTransform.sizeDelta = new Vector2(videoPlayer.width, videoPlayer.height);
                rawImage.rectTransform.localScale = new Vector3(1, -1, 1); // Y축을 -1로 뒤집어 똑바로 세움
            }
            // =============================================================

            var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);
            AsyncGPUReadbackRequest req = default;
            var waitUntilReqDone = new WaitUntil(() => req.done);
            var result = HandLandmarkerResult.Alloc(options.numHands);

            while (true)
            {
                if (isPaused) yield return new WaitWhile(() => isPaused);
                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                // 비디오 프레임을 텍스처로 읽어와 CPU 메모리에 올림 (CPUAsync 모드 강제 적용)
                req = textureFrame.ReadTextureAsync(videoPlayer.texture, false, false);
                yield return waitUntilReqDone;

                if (req.hasError)
                {
                    Debug.LogWarning("프레임 읽기 실패");
                    continue;
                }

                Image image = textureFrame.BuildCPUImage();
                textureFrame.Release();

                // 미디어파이프 뇌(AI)로 데이터 전송
                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageProcessingOptions, ref result)) _handLandmarkerResultAnnotationController.DrawNow(result);
                        else _handLandmarkerResultAnnotationController.DrawNow(default);
                        break;
                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result)) _handLandmarkerResultAnnotationController.DrawNow(result);
                        else _handLandmarkerResultAnnotationController.DrawNow(default);
                        break;
                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                        break;
                }
            }
        }

        private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
        {
            _handLandmarkerResultAnnotationController.DrawLater(result);
        }
    }
}
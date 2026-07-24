using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.FaceLandmarker;

public class MediaPipeIntegrationBridge : MonoBehaviour
{
    [Header("좌표 정규화 스케일 (테스트 시 조절 필요)")]
    public float positionMultiplier = 10f;
    public float depthMultiplier = -10f;

    private void OnEnable()
    {
        // MediaPipe Runner 클래스들에 해당 이벤트가 선언되어 있어야 합니다.
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked += OnHandDataReceived;
        Mediapipe.Unity.Sample.FaceLandmarkDetection.FaceLandmarkerRunner.OnFaceTracked += OnFaceDataReceived;
    }

    private void OnDisable()
    {
        Mediapipe.Unity.Sample.HandLandmarkDetection.HandLandmarkerRunner.OnHandTracked -= OnHandDataReceived;
        Mediapipe.Unity.Sample.FaceLandmarkDetection.FaceLandmarkerRunner.OnFaceTracked -= OnFaceDataReceived;
    }

    private void OnHandDataReceived(HandLandmarkerResult result)
    {
        if (VisionTrackingManager.Instance == null) return;

        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            // CS0021 핫픽스: .landmarks 속성을 통해 배열에 접근합니다.
            var handLandmarksList = result.handLandmarks[0].landmarks;
            if (handLandmarksList != null && handLandmarksList.Count > 9)
            {
                var handLandmark = handLandmarksList[9];
                Vector3 convertedPosition = new Vector3(
                    handLandmark.x * positionMultiplier,
                    -handLandmark.y * positionMultiplier,
                    handLandmark.z * depthMultiplier
                );
                VisionTrackingManager.Instance.currentHandPosition = convertedPosition;
                VisionTrackingManager.Instance.isTracking = true;
            }
        }
    }

    private void OnFaceDataReceived(FaceLandmarkerResult result)
    {
        if (VisionTrackingManager.Instance == null) return;

        if (result.faceLandmarks != null && result.faceLandmarks.Count > 0)
        {
            // CS0021 핫픽스: .landmarks 속성을 통해 배열에 접근합니다.
            var faceLandmarksList = result.faceLandmarks[0].landmarks;
            if (faceLandmarksList != null && faceLandmarksList.Count > 1)
            {
                var faceLandmark = faceLandmarksList[1];
                Vector3 convertedPosition = new Vector3(
                    faceLandmark.x * positionMultiplier,
                    -faceLandmark.y * positionMultiplier,
                    faceLandmark.z * depthMultiplier
                );
                VisionTrackingManager.Instance.currentHeadPosition = convertedPosition;
                VisionTrackingManager.Instance.isTracking = true;
            }
        }
    }
}
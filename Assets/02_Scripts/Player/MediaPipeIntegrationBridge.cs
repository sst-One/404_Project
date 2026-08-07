using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.FaceLandmarker;

public class MediaPipeIntegrationBridge : MonoBehaviour
{
    [Header("좌표 정규화 스케일")]
    public float positionMultiplier = 10f;
    public float depthMultiplier = -10f;

    private void OnEnable()
    {
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
            var landmarks = result.handLandmarks[0].landmarks;
            if (landmarks != null && landmarks.Count > 9)
            {
                var lm = landmarks[9]; // 중지 손가락 기준점
                Vector3 pos = new Vector3(lm.x * positionMultiplier, -lm.y * positionMultiplier, lm.z * depthMultiplier);
                VisionTrackingManager.Instance.UpdateHandData(pos);
            }
        }
    }

    private void OnFaceDataReceived(FaceLandmarkerResult result)
    {
        if (VisionTrackingManager.Instance == null) return;

        if (result.faceLandmarks != null && result.faceLandmarks.Count > 0)
        {
            var landmarks = result.faceLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count <= 1) return;

            // 움직임(Lean/Freeze) 판정용 코끝 좌표 (1번)
            var nose = landmarks[1];
            Vector3 headPos = new Vector3(nose.x * positionMultiplier, -nose.y * positionMultiplier, nose.z * depthMultiplier);

            Vector3 gazePos = headPos;

            // 시선(Gaze) 커서용 동공 좌표 (468, 473번)
            if (landmarks.Count > 474)
            {
                var leftIris = landmarks[468];
                var rightIris = landmarks[473];

                float gazeX = (leftIris.x + rightIris.x) / 2f;
                float gazeY = (leftIris.y + rightIris.y) / 2f;
                float gazeZ = (leftIris.z + rightIris.z) / 2f;

                gazePos = new Vector3(gazeX * positionMultiplier, -gazeY * positionMultiplier, gazeZ * depthMultiplier);
            }

            // 시선과 코끝 좌표를 동시에 전달
            VisionTrackingManager.Instance.UpdateFaceData(gazePos, headPos);
        }
    }
}
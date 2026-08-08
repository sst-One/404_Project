using UnityEngine;
using Mediapipe.Tasks.Vision.HandLandmarker;
using Mediapipe.Tasks.Vision.FaceLandmarker;

public class MediaPipeIntegrationBridge : MonoBehaviour
{
    [Header("좌표 정규화 스케일")]
    public float positionMultiplier = 10f;
    public float depthMultiplier = -10f;

    private float _smoothedZ = 0f;
    private readonly float zSmoothFactor = 10f;

    private readonly object _dataLock = new object();

    private bool _hasNewFaceData = false;
    private Vector3 _rawGazePos;
    private Vector3 _rawNosePos;
    private float _rawFaceZ;
    private Vector3 _rawHeadRot;

    private bool _hasNewHandData = false;
    private Vector3 _rawHandPos;

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

    private void Update()
    {
        if (VisionTrackingManager.Instance == null) return;

        lock (_dataLock)
        {
            if (_hasNewHandData)
            {
                VisionTrackingManager.Instance.UpdateHandData(_rawHandPos);
                _hasNewHandData = false;
            }

            if (_hasNewFaceData)
            {
                _smoothedZ = Mathf.Lerp(_smoothedZ, _rawFaceZ, Time.deltaTime * zSmoothFactor);
                Vector3 finalHeadPos = new Vector3(_rawNosePos.x, _rawNosePos.y, _smoothedZ);

                VisionTrackingManager.Instance.UpdateFaceData(_rawGazePos, finalHeadPos, _rawHeadRot);
                _hasNewFaceData = false;
            }
        }
    }

    private void OnHandDataReceived(HandLandmarkerResult result)
    {
        if (result.handLandmarks != null && result.handLandmarks.Count > 0)
        {
            var landmarks = result.handLandmarks[0].landmarks;
            if (landmarks != null && landmarks.Count > 9)
            {
                var lm = landmarks[9];
                Vector3 pos = new Vector3(lm.x * positionMultiplier, -lm.y * positionMultiplier, lm.z * depthMultiplier);

                lock (_dataLock)
                {
                    _rawHandPos = pos;
                    _hasNewHandData = true;
                }
            }
        }
    }

    private void OnFaceDataReceived(FaceLandmarkerResult result)
    {
        if (result.faceLandmarks != null && result.faceLandmarks.Count > 0)
        {
            var landmarks = result.faceLandmarks[0].landmarks;
            if (landmarks == null || landmarks.Count <= 1) return;

            var nose = landmarks[1];
            Vector3 gazePos = new Vector3(nose.x * positionMultiplier, -nose.y * positionMultiplier, nose.z * depthMultiplier);

            if (landmarks.Count > 474)
            {
                var leftIris = landmarks[468];
                var rightIris = landmarks[473];

                float gazeX = (leftIris.x + rightIris.x) / 2f;
                float gazeY = (leftIris.y + rightIris.y) / 2f;
                float gazeZ = (leftIris.z + rightIris.z) / 2f;

                gazePos = new Vector3(gazeX * positionMultiplier, -gazeY * positionMultiplier, gazeZ * depthMultiplier);
            }

            Vector3 headRot = Vector3.zero;
            float rawZ = 0f;

            if (landmarks.Count > 263)
            {
                var leftEye = landmarks[33];
                var rightEye = landmarks[263];
                var top = landmarks[10];
                var bottom = landmarks[152];

                Vector3 pLeft = new Vector3(leftEye.x, -leftEye.y, leftEye.z);
                Vector3 pRight = new Vector3(rightEye.x, -rightEye.y, rightEye.z);
                Vector3 pTop = new Vector3(top.x, -top.y, top.z);
                Vector3 pBottom = new Vector3(bottom.x, -bottom.y, bottom.z);

                Vector3 rightVec = (pRight - pLeft).normalized;
                Vector3 upVec = (pTop - pBottom).normalized;
                Vector3 forwardVec = Vector3.Cross(rightVec, upVec).normalized;

                Quaternion rot = Quaternion.LookRotation(forwardVec, upVec);
                headRot = rot.eulerAngles;
                headRot.x = NormalizeAngle(headRot.x);
                headRot.y = NormalizeAngle(headRot.y);
                headRot.z = NormalizeAngle(headRot.z);

                float faceHeight = Vector3.Distance(pTop, pBottom);
                float faceWidth = Vector3.Distance(pLeft, pRight);
                rawZ = ((faceHeight + faceWidth) / 2f) * 30f;
            }

            Vector3 nosePos = new Vector3(nose.x * positionMultiplier, -nose.y * positionMultiplier, 0f);

            lock (_dataLock)
            {
                _rawGazePos = gazePos;
                _rawNosePos = nosePos;
                _rawFaceZ = rawZ;
                _rawHeadRot = headRot;
                _hasNewFaceData = true;
            }
        }
    }

    private float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }
}
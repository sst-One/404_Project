using System;
using System.Collections;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.FaceLandmarkDetection;
using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;

public class WebCamBootstrapper : MonoBehaviour
{
    private const float CameraStartupTimeout = 10f;

    private VisionTrackingManager _trackingManager;
    private bool _isSwitching;
    private bool _hasPendingSelection;
    private string _pendingDeviceName;
    private FaceLandmarkerRunner _faceRunner;
    private HandLandmarkerRunner _handRunner;
    private WebCamSource _ownedSource;
    private string _streamingDeviceName;
    private float _lastFrameTime;

    private void Update()
    {
        if (_streamingDeviceName == null || _isSwitching) return;
        if (_ownedSource != null && IsCameraReady(_ownedSource))
        {
            _lastFrameTime = Time.unscaledTime;
            return;
        }
        if (Time.unscaledTime - _lastFrameTime >= 3f)
        {
            if (_trackingManager != null) _trackingManager.SetCameraResult(_streamingDeviceName, null);
            StopSharedCamera();
        }
    }

    private void Awake()
    {
        _faceRunner = GetComponentInChildren<FaceLandmarkerRunner>(true);
        _handRunner = GetComponentInChildren<HandLandmarkerRunner>(true);
        if (_faceRunner == null) _faceRunner = FindFirstObjectByType<FaceLandmarkerRunner>();
        if (_handRunner == null) _handRunner = FindFirstObjectByType<HandLandmarkerRunner>();
        if (_faceRunner != null) _faceRunner.UsesSharedImageSource = true;
        if (_handRunner != null) _handRunner.UsesSharedImageSource = true;
    }

    private void Start()
    {
        _trackingManager = VisionTrackingManager.Instance;
        if (_trackingManager != null)
            _trackingManager.OnCameraConfirmed += InitializeCamera;
    }

    private void OnDestroy()
    {
        if (_trackingManager != null)
            _trackingManager.OnCameraConfirmed -= InitializeCamera;
        StopSharedCamera();
    }

    private void InitializeCamera(string selectedDeviceName)
    {
        _pendingDeviceName = selectedDeviceName;
        _hasPendingSelection = true;
        if (_isSwitching) return;

        _isSwitching = true;
        StartCoroutine(ProcessCameraSelections());
    }

    private IEnumerator ProcessCameraSelections()
    {
        try
        {
            while (_hasPendingSelection)
            {
                string deviceName = _pendingDeviceName;
                _hasPendingSelection = false;
                yield return SwitchCameraRoutine(deviceName);
            }
        }
        finally
        {
            _isSwitching = false;
        }
    }

    private IEnumerator SwitchCameraRoutine(string selectedDeviceName)
    {
        if (WebCamTexture.devices.Length == 0)
        {
            if (_trackingManager != null) _trackingManager.SetCameraResult(selectedDeviceName, null);
            Debug.LogWarning("No webcam is available.", this);
            yield break;
        }

        StopSharedCamera();
        yield return new WaitForSecondsRealtime(0.2f);

        var resolutions = new[] { new ImageSource.ResolutionStruct(1280, 720, 30) };
        var source = new WebCamSource(1280, resolutions);
        string[] deviceNames = source.sourceCandidateNames;
        int index = deviceNames == null ? -1 : Array.IndexOf(deviceNames, selectedDeviceName);
        if (index < 0)
        {
            if (_trackingManager != null) _trackingManager.SetCameraResult(selectedDeviceName, null);
            Debug.LogWarning("The webcam disconnected during camera selection.", this);
            yield break;
        }
        source.SelectSource(index);
        _ownedSource = source;
        ImageSourceProvider.ImageSource = source;
        Coroutine sourceStartup = StartCoroutine(source.Play());

        float deadline = Time.realtimeSinceStartup + CameraStartupTimeout;
        while (!_hasPendingSelection && !IsCameraReady(source) && Time.realtimeSinceStartup < deadline)
            yield return null;

        if (_hasPendingSelection || !IsCameraReady(source))
        {
            StopCoroutine(sourceStartup);
            StopSharedCamera();
            if (!_hasPendingSelection)
            {
                if (_trackingManager != null) _trackingManager.SetCameraResult(selectedDeviceName, null);
                Debug.LogWarning($"Webcam '{selectedDeviceName}' did not produce a frame within {CameraStartupTimeout} seconds.", this);
            }
            yield break;
        }

        _streamingDeviceName = selectedDeviceName;
        if (_trackingManager != null) _trackingManager.SetCameraResult(selectedDeviceName, source.GetCurrentTexture());

        float initializationDeadline = Time.realtimeSinceStartup + CameraStartupTimeout;
        while (!_hasPendingSelection && !AreRunnersInitialized() && Time.realtimeSinceStartup < initializationDeadline)
            yield return null;
        if (_hasPendingSelection) yield break;
        _lastFrameTime = Time.unscaledTime;
        if (!AreRunnersInitialized())
        {
            Debug.LogWarning("The webcam opened, but the MediaPipe models were not initialized.", this);
            yield break;
        }

        // Bootstrap may have set its sample input while the selected camera was opening.
        ImageSourceProvider.ImageSource = source;
        if (_faceRunner != null) TryStartRunner(_faceRunner);
        if (_handRunner != null) TryStartRunner(_handRunner);
    }

    private bool AreRunnersInitialized()
    {
        return (_faceRunner == null || _faceRunner.IsInitialized) &&
            (_handRunner == null || _handRunner.IsInitialized);
    }

    private static bool IsCameraReady(WebCamSource source)
    {
        return source.GetCurrentTexture() is WebCamTexture texture &&
            texture.isPlaying && texture.didUpdateThisFrame && texture.width > 16 && texture.height > 16;
    }

    private bool TryStartRunner(BaseRunner runner)
    {
        try
        {
            runner.Play();
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not start {runner.GetType().Name}: {exception.Message}", this);
            TryStopRunner(runner);
            return false;
        }
    }

    private void TryStopRunner(BaseRunner runner)
    {
        if (runner == null) return;
        try
        {
            runner.Stop();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not stop {runner.GetType().Name}: {exception.Message}", this);
        }
    }

    private void StopSharedCamera()
    {
        TryStopRunner(_faceRunner);
        TryStopRunner(_handRunner);
        WebCamSource source = _ownedSource;
        _ownedSource = null;
        _streamingDeviceName = null;
        if (source == null) return;
        if (ImageSourceProvider.ImageSource == source) ImageSourceProvider.ImageSource = null;
        try
        {
            source.Stop();
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not stop the webcam source: {exception.Message}", this);
        }
    }
}

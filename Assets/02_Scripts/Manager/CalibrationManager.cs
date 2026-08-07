using System.Collections;
using UnityEngine;
using System;

public class CalibrationManager : MonoBehaviour
{
    public static CalibrationManager Instance { get; private set; }

    [Header("캘리브레이션 파라미터 (PARAM-008, 033)")]
    public float calibrationDuration = 3.0f;

    public bool IsCalibrated { get; private set; } = false;
    public Vector3 OriginPosition { get; private set; }

    public event Action OnCalibrationSuccess;
    public event Action OnCalibrationFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void StartCalibration()
    {
        Debug.Log("[CalibrationManager] 입력 원점 캘리브레이션을 시작합니다. 3초간 바른 자세를 유지하십시오.");
        StartCoroutine(CalibrationRoutine());
    }

    private IEnumerator CalibrationRoutine()
    {
        IsCalibrated = false;
        float timer = 0f;

        // 3초 대기 (실제 바른 자세 유지 구간)
        while (timer < calibrationDuration)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        CompleteCalibration();
    }

    private void CompleteCalibration()
    {
        // [핵심 핫픽스] 유니티 카메라 위치가 아니라, MediaPipe가 인식한 실제 머리/손 좌표를 Baseline으로 강제 주입
        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.baselineHeadPosition = VisionTrackingManager.Instance.currentHeadPosition;
            VisionTrackingManager.Instance.baselineHandPosition = VisionTrackingManager.Instance.currentHandPosition;

            OriginPosition = VisionTrackingManager.Instance.baselineHeadPosition;
            IsCalibrated = true;
            Debug.Log($"[CalibrationManager] 캘리브레이션 성공. Vision AI 원점 등록 완료. Head: {OriginPosition}");
            OnCalibrationSuccess?.Invoke();
        }
        else
        {
            Debug.LogError("[CalibrationManager] VisionTrackingManager를 찾을 수 없어 캘리브레이션에 실패했습니다.");
            OnCalibrationFailed?.Invoke();
        }
    }
}
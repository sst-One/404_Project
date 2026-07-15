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

        // 실제 비전 API 연동 시 이곳에 분산/오차 검증 로직이 추가됩니다.
        // 현재는 Fallback 모드 테스트를 위해 시간 대기 후 현재 카메라 위치를 원점으로 확정합니다.
        while (timer < calibrationDuration)
        {
            timer += Time.deltaTime;
            // 미세 떨림 초과 시 실패 처리 로직 추가 예정
            yield return null;
        }

        CompleteCalibration();
    }

    private void CompleteCalibration()
    {
        if (Camera.main != null)
        {
            OriginPosition = Camera.main.transform.position;
        }
        else
        {
            OriginPosition = Vector3.zero;
        }

        IsCalibrated = true;
        Debug.Log($"[CalibrationManager] 캘리브레이션 성공. 원점 등록 완료: {OriginPosition}");
        OnCalibrationSuccess?.Invoke();
    }
}
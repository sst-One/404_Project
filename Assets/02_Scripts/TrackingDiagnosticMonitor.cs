using UnityEngine;

public class TrackingDiagnosticMonitor : MonoBehaviour
{
    private void OnGUI()
    {
        if (VisionTrackingManager.Instance == null)
        {
            GUI.Label(new Rect(10, 10, 500, 20), "VisionTrackingManager: NULL (시스템 파괴됨)");
            return;
        }

        string debugText = "=== [비전 AI 트래킹 진단 모니터] ===\n";
        debugText += $"Is Tracking (AI 데이터 수신): {VisionTrackingManager.Instance.isTracking}\n";
        debugText += $"Iris/Head (동공/머리 좌표): {VisionTrackingManager.Instance.currentHeadPosition}\n";
        debugText += $"Hand (손 좌표): {VisionTrackingManager.Instance.currentHandPosition}\n\n";

        debugText += "=== [플레이어 입력 브릿지 상태] ===\n";
        PlayerInputProvider input = FindObjectOfType<PlayerInputProvider>();
        if (input != null)
        {
            debugText += $"Gaze Screen Pos (화면 커서 위치): {input.GazeScreenPosition}\n";
            debugText += $"Reach (손 뻗기 Trigger/Hold): {input.IsGripTriggered} / {input.IsGripHeld}\n";
            debugText += $"Lean (기울임 Trigger): {input.IsLeanTriggered}\n";
            debugText += $"Freeze (정지 상태): {input.IsFreezeActive}\n";
        }
        else
        {
            debugText += "PlayerInputProvider: 현재 씬에서 찾을 수 없음! (플레이어 파괴됨)\n";
        }

        debugText += "\n=== [시스템 블로킹 요인] ===\n";
        debugText += $"Camera.main: {(Camera.main != null ? Camera.main.name : "NULL (카메라 유실)")}\n";
        debugText += $"Time.timeScale: {Time.timeScale}\n";

        if (SystemMenuController.Instance != null)
        {
            debugText += $"IsPaused (ESC 메뉴 켜짐 상태): {SystemMenuController.Instance.IsPaused}\n";
        }
        else
        {
            debugText += "SystemMenuController: NULL\n";
        }

        if (UIManager.Instance != null)
        {
            debugText += $"UIManager.IsAnyUIBlocking(): {UIManager.Instance.IsAnyUIBlocking()}\n";
        }

        // 화면 좌측 상단에 노란색 텍스트로 고정 출력
        GUIStyle style = new GUIStyle();
        style.fontSize = 24;
        style.normal.textColor = Color.yellow;

        // 배경에 반투명 검은색 박스를 깔아 글씨 가독성 확보
        GUI.color = new Color(0, 0, 0, 0.7f);
        GUI.DrawTexture(new Rect(10, 10, 600, 450), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUI.Label(new Rect(20, 20, 600, 450), debugText, style);
    }
}
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerScaleController : MonoBehaviour
{
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        string sceneName = scene.name;

        // 404호 내부를 다루는 씬 목록 식별 (Stage 2, Stage 5-6, Stage 8-11, Stage 12-13)
        if (sceneName == "03_Stage2" || sceneName == "05_Stage5_6" ||
            sceneName == "07_Stage8_11" || sceneName == "08_Stage12_13")
        {
            transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Debug.Log($"[PlayerScaleController] {sceneName} 진입: 플레이어 스케일 0.5 축소 완료");
        }
        else
        {
            // 엘리베이터 및 타이틀 등 외부 씬에서는 원래 크기로 복구
            transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
            Debug.Log($"[PlayerScaleController] {sceneName} 진입: 플레이어 스케일 1.0 복구 완료");
        }
    }
}
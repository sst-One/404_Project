using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance { get; private set; }

    [Header("Persistent References")]
    public Transform playerRig;

    [Tooltip("플레이어 프리팹 하위의 진짜 Main Camera를 연결하십시오.")]
    public Camera mainCamera;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // SystemInit 씬 자체 로드 시에는 스킵
        if (scene.name.Contains("SystemInit")) return;

        // 1. 플레이어 위치 동기화
        GameObject spawnPoint = GameObject.Find("Player_SpawnPoint");
        if (spawnPoint != null && playerRig != null)
        {
            // 충돌 방지를 위해 CharacterController가 있다면 임시 비활성화 후 이동
            var cc = playerRig.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerRig.position = spawnPoint.transform.position;
            playerRig.rotation = spawnPoint.transform.rotation;

            if (cc != null) cc.enabled = true;
            Debug.Log($"[Bootstrapper] {scene.name} 진입 완료. 스폰 포인트 맵핑 성공.");
        }
        else if (!scene.name.Contains("Title"))
        {
            Debug.LogWarning($"[Bootstrapper] {scene.name} 씬에 'Player_SpawnPoint'가 존재하지 않아 원점 대기합니다.");
        }

        // 2. 씬에 잘못 남아있는 가짜 카메라 및 이벤트 시스템 강제 파괴 (스트림 충돌 방어)
        CleanUpDuplicateSystems(scene);
    }

    private void CleanUpDuplicateSystems(Scene scene)
    {
        // 씬 로드 시 스테이지에 실수로 남아있는 카메라(AudioListener 포함) 강제 삭제
        Camera[] allCameras = FindObjectsOfType<Camera>();
        foreach (Camera cam in allCameras)
        {
            // 현재 로드된 씬 소속이면서, 우리의 진짜 메인 카메라가 아닌 경우 모조리 파괴
            if (cam != mainCamera && cam.gameObject.scene.name == scene.name)
            {
                Debug.LogWarning($"[Bootstrapper] {scene.name} 씬에서 복제된 가짜 카메라({cam.name})가 발견되어 강제 파괴합니다.");
                Destroy(cam.gameObject);
            }
        }

        // UI 이벤트 꼬임 방지를 위한 복제 EventSystem 삭제
        EventSystem[] eventSystems = FindObjectsOfType<EventSystem>();
        if (eventSystems.Length > 1)
        {
            foreach (EventSystem es in eventSystems)
            {
                if (es.gameObject.scene.name == scene.name) Destroy(es.gameObject);
            }
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
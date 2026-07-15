using UnityEngine;
using UnityEngine.SceneManagement;

public class GameBootstrapper : MonoBehaviour
{
    public static GameBootstrapper Instance { get; private set; }

    [Header("Persistent References")]
    public Transform playerRig;

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
        GameObject spawnPoint = GameObject.Find("Player_SpawnPoint");

        if (spawnPoint != null && playerRig != null)
        {
            playerRig.position = spawnPoint.transform.position;
            playerRig.rotation = spawnPoint.transform.rotation;
        }
        else if (!scene.name.Contains("SystemInit") && !scene.name.Contains("Title"))
        {
            Debug.LogError($"[Bootstrapper] {scene.name} 씬에 'Player_SpawnPoint'가 존재하지 않습니다!");
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}
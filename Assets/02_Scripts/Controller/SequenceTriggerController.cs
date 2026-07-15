using UnityEngine;
using UnityEngine.SceneManagement;

public class SequenceTriggerController : MonoBehaviour
{
    private InteractableItem stage1ElevatorButton;
    private InteractableItem stage2TvRemote;
    private InteractableItem stage6FuseBox;
    private InteractableItem stage7Gem;

    private void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindTriggersForCurrentScene(scene.name);
    }

    private void BindTriggersForCurrentScene(string sceneName)
    {
        if (sceneName == "02_Elevator")
        {
            GameObject btnObj = GameObject.Find("Item_ElevatorButton");
            if (btnObj != null)
            {
                stage1ElevatorButton = btnObj.GetComponent<InteractableItem>();
                stage1ElevatorButton.onInteractEvent.AddListener(OnStage1Completed);
            }
        }
        else if (sceneName == "04_Room404")
        {
            GameObject tvObj = GameObject.Find("Item_TvRemote");
            if (tvObj != null)
            {
                stage2TvRemote = tvObj.GetComponent<InteractableItem>();
                stage2TvRemote.onInteractEvent.AddListener(OnStage2Completed);
                stage2TvRemote.gameObject.SetActive(GameFlowManager.Instance.currentStage == GameStage.Stage2_Room);
            }

            GameObject fuseObj = GameObject.Find("Item_FuseBox");
            if (fuseObj != null)
            {
                stage6FuseBox = fuseObj.GetComponent<InteractableItem>();
                stage6FuseBox.onInteractEvent.AddListener(OnStage6Completed);
                stage6FuseBox.gameObject.SetActive(GameFlowManager.Instance.currentStage == GameStage.Stage6_Blackout);
            }
        }
        else if (sceneName == "03_Corridor_4F")
        {
            GameObject gemObj = GameObject.Find("Item_Gem_Normal");
            if (gemObj != null)
            {
                stage7Gem = gemObj.GetComponent<InteractableItem>();
                stage7Gem.onInteractEvent.AddListener(OnStage7Completed);
                stage7Gem.gameObject.SetActive(GameFlowManager.Instance.currentStage == GameStage.Stage7_Gem);
            }
        }
    }

    private void OnStage1Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
        if (stage1ElevatorButton != null) stage1ElevatorButton.gameObject.SetActive(false);
    }

    private void OnStage2Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);
        if (stage2TvRemote != null) stage2TvRemote.gameObject.SetActive(false);
    }

    private void OnStage6Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
        if (stage6FuseBox != null) stage6FuseBox.gameObject.SetActive(false);
    }

    private void OnStage7Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
        if (stage7Gem != null) stage7Gem.gameObject.SetActive(false);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingSequenceController : MonoBehaviour
{
    private PhoneController phoneController;
    private InteractableItem endingGem;

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
        // 로드된 씬이 폰이나 보석을 포함하지 않는다면 바인딩 시도 자체를 중단함
        if (scene.name != "04_Room404" && scene.name != "05_Ending") return;

        GameObject phoneObj = GameObject.Find("Item_Phone");
        if (phoneObj != null)
        {
            phoneController = phoneObj.GetComponent<PhoneController>();
            if (phoneController != null) // 안전 장치 추가
            {
                phoneController.onCallSuccess.RemoveAllListeners(); // 중복 등록 방지
                phoneController.onCallSuccess.AddListener(OnStage11Completed);
            }
        }

        GameObject gemObj = GameObject.Find("Item_EndingGem");
        if (gemObj != null)
        {
            endingGem = gemObj.GetComponent<InteractableItem>();
            if (endingGem != null)
            {
                endingGem.onInteractEvent.RemoveAllListeners();
                endingGem.onInteractEvent.AddListener(OnStage13Completed);
                endingGem.gameObject.SetActive(GameFlowManager.Instance.currentStage == GameStage.Stage13_Ending);
            }
        }
    }

    private void OnStage11Completed()
    {
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        StartCoroutine(PoliceArrivalAndNextDayRoutine());
    }

    private IEnumerator PoliceArrivalAndNextDayRoutine()
    {
        yield return new WaitForSeconds(4.0f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        yield return new WaitForSeconds(3.0f);

        if (phoneController != null) phoneController.gameObject.SetActive(false);
        if (endingGem != null) endingGem.gameObject.SetActive(true);
    }

    private void OnStage13Completed()
    {
        StartCoroutine(FlashbackAndCreditRoutine());
    }

    private IEnumerator FlashbackAndCreditRoutine()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);
        yield return new WaitForSeconds(4.0f);
    }
}
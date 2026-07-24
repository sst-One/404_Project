using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("사운드 에셋 연결")]
    public AudioSource policeSirenSource;
    public AudioSource doorKnockSource;

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
        if (scene.name == "08_Stage12_13")
        {
            // 다음 씬으로 넘어왔을 때 암전 스크린이 있다면 서서히 밝아지게 처리
            GameObject blackObj = GameObject.Find("ForcedBlackScreen");
            if (blackObj != null)
            {
                StartCoroutine(FadeOutBlackScreen(blackObj, 2.0f));
            }
        }

        if (scene.name != "07_Stage8_11" && scene.name != "08_Stage12_13") return;

        GameObject phoneObj = GameObject.Find("Item_Phone");
        if (phoneObj != null)
        {
            phoneController = phoneObj.GetComponent<PhoneController>();
            if (phoneController != null)
            {
                phoneController.onCallSuccess.RemoveAllListeners();
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

                bool isEndingStage = (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Stage13_Ending);
                endingGem.gameObject.SetActive(isEndingStage);
            }
        }
    }

    private void OnStage11Completed()
    {
        if (StateManager.Instance != null) StateManager.Instance.RemoveThreat();
        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        Debug.Log("[EndingSequence] 112 신고 성공. 최상단 암전 스크린 생성 및 페이드 아웃 가동.");

        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        // TitleController 대신 시스템 최상단에 검은 캔버스를 직접 생성하여 화면을 덮음
        yield return StartCoroutine(FadeInForcedBlackScreen(2.0f));

        if (policeSirenSource != null) policeSirenSource.Play();
        yield return new WaitForSeconds(4.0f);

        if (doorKnockSource != null) doorKnockSource.Play();
        yield return new WaitForSeconds(4.0f);

        Debug.Log("[EndingSequence] 연출 종료. Stage 12/13 씬 로드 요청.");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
        }

        StartCoroutine(NextDayRoutine());
    }

    // --- 강제 암전 오버레이 로직 ---
    private IEnumerator FadeInForcedBlackScreen(float duration)
    {
        GameObject blackObj = new GameObject("ForcedBlackScreen");
        DontDestroyOnLoad(blackObj); // 씬이 넘어가도 검은 화면 강제 유지

        Canvas canvas = blackObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // GameFlowManager의 UI보다 무조건 위에 렌더링되도록 설정

        Image img = blackObj.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            img.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        img.color = new Color(0, 0, 0, 1);
    }

    private IEnumerator FadeOutBlackScreen(GameObject targetObj, float duration)
    {
        Image img = targetObj.GetComponent<Image>();
        if (img != null)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                img.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / duration));
                yield return null;
            }
        }
        Destroy(targetObj);
    }
    // ---------------------------------

    private IEnumerator NextDayRoutine()
    {
        yield return new WaitForSeconds(3.0f);

        if (GameFlowManager.Instance != null && GameFlowManager.Instance.currentStage == GameStage.Stage12_Police)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }

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
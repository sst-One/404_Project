// 1. Stage8_11_Controller.cs
using System.Collections;
using UnityEngine;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("사운드 에셋 연결 (AudioSources)")]
    public AudioSource policeSirenSource;
    public AudioSource doorKnockSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string policeSirenClipName = "SND-055_PoliceSiren_Loop";
    public string doorKnockClipName = "";

    private PhoneController phoneController;

    private void Start()
    {
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
    }

    private void OnStage11Completed()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.RemoveThreat();
            StateManager.Instance.ResetHeartbeat();
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemy in enemies)
        {
            enemy.SetActive(false);
        }

        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        Debug.Log("[EndingSequence] 112 신고 성공. 최상단 암전 스크린 생성 및 페이드 아웃 가동.");

        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        yield return StartCoroutine(FadeInForcedBlackScreen(2.0f));

        if (policeSirenSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(policeSirenClipName);
            if (clip != null)
            {
                policeSirenSource.clip = clip;
                policeSirenSource.Play();
            }
        }

        yield return new WaitForSeconds(4.0f);

        if (doorKnockSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(doorKnockClipName);
            if (clip != null)
            {
                doorKnockSource.clip = clip;
                doorKnockSource.Play();
            }
        }

        yield return new WaitForSeconds(4.0f);

        Debug.Log("[EndingSequence] 연출 종료. Stage 12/13 씬 로드 요청.");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
        }
    }

    private IEnumerator FadeInForcedBlackScreen(float duration)
    {
        GameObject blackObj = new GameObject("ForcedBlackScreen");
        DontDestroyOnLoad(blackObj);

        Canvas canvas = blackObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        UnityEngine.UI.Image img = blackObj.AddComponent<UnityEngine.UI.Image>();
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
}
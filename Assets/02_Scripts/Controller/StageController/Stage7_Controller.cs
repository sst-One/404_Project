using System.Collections;
using UnityEngine;

public class Stage7_Controller : MonoBehaviour
{
    [Header("References")]
    public InteractableItem gemItem;
    public GameObject suspiciousMan;

    [Header("Audio Clip Names")]
    public string gemGetClipName = "SND-041_JemRecieve_OneShot";

    private void Start()
    {
        if (suspiciousMan != null)
        {
            suspiciousMan.SetActive(true);
            EnemyAI ai = suspiciousMan.GetComponent<EnemyAI>();
            if (ai != null) ai.isNarrativeMode = true;
        }

        GameObject gemObj = GameObject.Find("Item_Gem_Normal");
        if (gemObj != null)
        {
            gemItem = gemObj.GetComponent<InteractableItem>();
            if (gemItem != null)
            {
                gemItem.enabled = false;
                if (gemItem.GetComponent<Collider>() != null) gemItem.GetComponent<Collider>().enabled = false;

                gemItem.onInteractEvent.RemoveAllListeners();
                gemItem.onInteractEvent.AddListener(OnGemReached);
            }
        }

        StartCoroutine(Stage7_NarrativeSequence());
    }

    private IEnumerator Stage7_NarrativeSequence()
    {
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_1));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_2));
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_3));
        }

        if (gemItem != null)
        {
            gemItem.enabled = true;
            if (gemItem.GetComponent<Collider>() != null) gemItem.GetComponent<Collider>().enabled = true;
        }
    }

    private void OnGemReached()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(gemGetClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (gemItem != null) gemItem.gameObject.SetActive(false);

        StartCoroutine(EndStage7Sequence());
    }

    private IEnumerator EndStage7Sequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

        yield return new WaitForSeconds(2.0f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
    }
}
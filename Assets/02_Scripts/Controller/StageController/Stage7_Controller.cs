// Stage7_Controller.cs
using System.Collections;
using UnityEngine;

public class Stage7_Controller : MonoBehaviour
{
    private InteractableItem gemItem;
    private EnemyAI suspiciousMan;
    public AudioSource gemGetSource;

    private void Start()
    {
        suspiciousMan = FindObjectOfType<EnemyAI>(true);
        if (suspiciousMan != null)
        {
            suspiciousMan.gameObject.SetActive(true);
            suspiciousMan.isNarrativeMode = true;
        }

        GameObject gemObj = GameObject.Find("Item_Gem_Normal");
        if (gemObj != null)
        {
            gemItem = gemObj.GetComponent<InteractableItem>();
            gemItem.enabled = false;
            if (gemItem.GetComponent<Collider>() != null) gemItem.GetComponent<Collider>().enabled = false;

            gemItem.onInteractEvent.RemoveAllListeners();
            gemItem.onInteractEvent.AddListener(OnGemReached);
        }

        StartCoroutine(Stage7_NarrativeSequence());
    }

    private IEnumerator Stage7_NarrativeSequence()
    {
        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_1));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_2));
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day3_Man_3));
        }

        if (gemItem != null)
        {
            gemItem.enabled = true;
            if (gemItem.GetComponent<Collider>() != null) gemItem.GetComponent<Collider>().enabled = true;
        }
    }

    private void OnGemReached()
    {
        if (gemGetSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-041_JemRecieve_OneShot");
            if (clip != null) gemGetSource.PlayOneShot(clip);
        }

        if (gemItem != null) gemItem.gameObject.SetActive(false);
        StartCoroutine(EndStage7Sequence());
    }

    private IEnumerator EndStage7Sequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
        yield return new WaitForSeconds(2.0f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage8_Intruder);
    }
}
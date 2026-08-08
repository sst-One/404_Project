using System.Collections;
using UnityEngine;

public class Stage2_Controller : MonoBehaviour
{
    private InteractableItem tvRemote;
    public AudioSource tvNewsSource;

    private void Start()
    {
        EnemyAI enemy = FindObjectOfType<EnemyAI>(true);
        if (enemy != null) enemy.gameObject.SetActive(false);

        GameObject tvObj = GameObject.Find("Item_TvRemote");
        if (tvObj != null)
        {
            tvRemote = tvObj.GetComponent<InteractableItem>();
            tvRemote.onInteractEvent.RemoveAllListeners();
            tvRemote.onInteractEvent.AddListener(OnTvRemoteReached);

            bool isStage2 = GameFlowManager.Instance.currentStage == GameStage.Stage2_Room;
            tvRemote.enabled = isStage2;
            if (tvRemote.GetComponent<Collider>() != null) tvRemote.GetComponent<Collider>().enabled = isStage2;
        }
    }

    private void OnTvRemoteReached()
    {
        if (tvRemote != null)
        {
            tvRemote.enabled = false;
            if (tvRemote.GetComponent<Collider>() != null) tvRemote.GetComponent<Collider>().enabled = false;
        }
        StartCoroutine(TvNewsAndSleepSequence());
    }

    private IEnumerator TvNewsAndSleepSequence()
    {
        if (tvNewsSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-060_NewsReport_Loop");
            if (clip != null)
            {
                tvNewsSource.clip = clip;
                tvNewsSource.Play();
            }
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day1_TVNews));
        }

        if (tvNewsSource != null && tvNewsSource.isPlaying) tvNewsSource.Stop();

        yield return new WaitForSeconds(2.0f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
    }
}
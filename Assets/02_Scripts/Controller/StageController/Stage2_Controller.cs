// Stage2_Controller.cs
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
            AudioClip clip = AudioManager.Instance.GetClip("SND-060_NewsReport_Loop"); // 기획상 파일명 기준
            if (clip != null)
            {
                tvNewsSource.clip = clip;
                tvNewsSource.Play();
            }
        }

        if (SubtitleController.Instance != null)
        {
            yield return StartCoroutine(SubtitleController.Instance.ShowInteractiveSubtitle(NarrativeData.Day1_TVNews));
        }

        if (tvNewsSource != null && tvNewsSource.isPlaying) tvNewsSource.Stop();

        yield return new WaitForSeconds(2.0f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Video; // Video 네임스페이스 추가

public class Stage2_Controller : MonoBehaviour
{
    [Header("TV References")]
    public InteractableItem tvRemote;
    public GameObject tvScreenDisplay;
    public AudioSource tvNewsSource;
    public VideoPlayer tvVideoPlayer; // 비디오 플레이어 추가

    [Header("Settings")]
    public float fadeDuration = 1.2f;

    private bool hasTriggered = false;

    private void Start()
    {
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(false);

        if (tvRemote != null)
        {
            tvRemote.onInteractEvent.RemoveAllListeners();
            tvRemote.onInteractEvent.AddListener(OnTvReachAction);
        }
    }

    public void OnTvReachAction()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (tvRemote != null)
        {
            tvRemote.enabled = false;
            if (tvRemote.GetComponent<Collider>() != null)
                tvRemote.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(NewsSequenceRoutine());
    }

    private IEnumerator NewsSequenceRoutine()
    {
        yield return new WaitForSeconds(1.0f);

        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(true);

        float playDuration = 12.0f; // 기본 임시값

        if (tvVideoPlayer != null)
        {
            tvVideoPlayer.Play();
            // 비디오 클립이 할당되어 있다면 비디오의 실제 길이를 사용
            if (tvVideoPlayer.clip != null) playDuration = (float)tvVideoPlayer.clip.length;
        }
        else if (tvNewsSource != null && tvNewsSource.clip != null)
        {
            // 비디오가 없고 오디오만 있을 경우의 Fallback
            tvNewsSource.Play();
            playDuration = tvNewsSource.clip.length;
        }

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day1_TVNews));
        }

        if (tvVideoPlayer != null && tvVideoPlayer.isPlaying) tvVideoPlayer.Stop();
        if (tvNewsSource != null && tvNewsSource.isPlaying) tvNewsSource.Stop();

        yield return new WaitForSeconds(1.5f);

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen(fadeDuration));
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
        }
    }
}
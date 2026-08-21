using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class Stage2_Controller : MonoBehaviour
{
    [Header("TV References")]
    public InteractableItem tvRemote;
    public GameObject tvScreenDisplay;
    public AudioSource tvNewsSource;
    public VideoPlayer tvVideoPlayer;

    [Header("Settings")]
    public float fadeDuration = 1.2f;
    public Transform tvLookTarget;

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

        float playDuration = 12.0f;

        if (tvVideoPlayer != null)
        {
            tvVideoPlayer.Play();
            if (tvVideoPlayer.clip != null) playDuration = (float)tvVideoPlayer.clip.length;
        }
        else if (tvNewsSource != null && tvNewsSource.clip != null)
        {
            tvNewsSource.Play();
            playDuration = tvNewsSource.clip.length;
        }

        // 카메라 시선 강제 고정
        MonoBehaviour cameraLookScript = null;
        if (PlayerController.Instance != null && PlayerController.Instance.mainCamera != null)
        {
            Camera cam = PlayerController.Instance.mainCamera;
            cameraLookScript = cam.GetComponent("FirstPersonCameraLook") as MonoBehaviour;
            if (cameraLookScript != null) cameraLookScript.enabled = false;

            if (tvLookTarget != null)
            {
                float t = 0f;
                Quaternion startRot = cam.transform.rotation;
                Quaternion targetRot = Quaternion.LookRotation(tvLookTarget.position - cam.transform.position);
                targetRot.z = 0;
                while (t < 1.0f)
                {
                    t += Time.deltaTime * 1.5f;
                    cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                    yield return null;
                }
                cam.transform.rotation = targetRot;
            }
        }

        // [핵심 핫픽스 3] 클릭 대기를 유발하는 yield return 제거
        // 비동기로 자막을 띄우기만 하고 로직은 계속 타이머를 흐르게 만듦
        Coroutine subtitleRoutine = null;
        if (UIManager.Instance != null)
        {
            subtitleRoutine = StartCoroutine(UIManager.Instance.ShowInteractiveSubtitle(NarrativeData.Day1_TVNews));
        }

        // 클릭과 무관하게 정확히 비디오 재생 시간(playDuration) 동안만 대기
        yield return new WaitForSeconds(Mathf.Max(0f, playDuration - 1.0f));

        if (tvVideoPlayer != null && tvVideoPlayer.isPlaying) tvVideoPlayer.Stop();
        if (tvNewsSource != null && tvNewsSource.isPlaying) tvNewsSource.Stop();

        // 열려있는 자막 강제 종료 처리 (필요시 추가)
        // 만약 UIManager에 자막을 강제로 끄는 함수(예: CloseSubtitle)가 있다면 여기서 호출해야 합니다.

        yield return new WaitForSeconds(1.5f);

        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.FadeOutScreen(fadeDuration));
        }

        if (cameraLookScript != null) cameraLookScript.enabled = true;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
        }
    }
}
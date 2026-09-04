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
            tvRemote.onInteractAction -= OnTvReachAction;
            tvRemote.onInteractAction += OnTvReachAction;
        }

        tvRemote.EnableInteractionWithLight();
    }

    public void OnTvReachAction()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        if (tvRemote != null)
        {
            // [원리 적용] 스크립트를 강제로 끄는 enabled = false 대신 정규 플래그 사용
            tvRemote.isInteractable = false;
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

        FirstPersonCameraLook cameraLookScript = null;
        if (PlayerController.Instance != null && PlayerController.Instance.CamLook != null)
        {
            cameraLookScript = PlayerController.Instance.CamLook;
            cameraLookScript.enabled = false;

            Camera cam = PlayerController.Instance.mainCamera;
            if (cam != null && tvLookTarget != null)
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

        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day1_TVNews);

        yield return new WaitForSeconds(Mathf.Max(0f, playDuration - 1.0f));

        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        if (tvVideoPlayer != null && tvVideoPlayer.isPlaying) tvVideoPlayer.Stop();
        if (tvNewsSource != null && tvNewsSource.isPlaying) tvNewsSource.Stop();

        yield return new WaitForSeconds(1.5f);

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(fadeDuration));

        if (cameraLookScript != null) cameraLookScript.enabled = true;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage3_Anomaly);
    }
}
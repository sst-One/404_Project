using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Lighting Settings")]
    public Light mainRoomLight;

    [Header("Hallucination Objects")]
    public GameObject hallucinationDecals;

    [Header("사운드 에셋 연결 (AudioSources)")]
    public AudioSource fuseBoxSwitchSource;
    public AudioSource hallucinationSource;
    public AudioSource lightFlickerSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string fuseBoxClipName = "SND-031_FuseBoxSwitch_OneShot";
    public string hallucinationClipName = "SND-011_Hallucination_OneShot";
    public string lightFlickerClipName = "SND-012_LightFlicker_Loop_OneShot";
    public string lightRestoreClipName = "SND-032_LIghtRestore_OneShot";

    private InteractableItem fuseBox;
    private bool isBlackout = false;

    private void Start()
    {
        if (mainRoomLight != null) mainRoomLight.enabled = true;
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);

        GameObject fuseObj = GameObject.Find("Item_FuseBox");
        if (fuseObj != null)
        {
            fuseBox = fuseObj.GetComponent<InteractableItem>();
            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;

            fuseBox.onInteractEvent.RemoveAllListeners();
            fuseBox.onInteractEvent.AddListener(OnFuseBoxReached);
        }

        // [핫픽스] 누락되었던 코루틴 실행 복구 (8초 후 암전 트리거)
        StartCoroutine(Stage5_ClueSequence());
    }

    private IEnumerator Stage5_ClueSequence()
    {
        Debug.Log("[Stage5_6] Stage 5 진입. 거실 단서 탐색 대기...");
        yield return new WaitForSeconds(8.0f);

        Debug.Log("[Stage5_6] Stage 6 진입. 쾅 소리와 함께 암전 발생.");
        if (mainRoomLight != null) mainRoomLight.enabled = false;
        isBlackout = true;

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        if (fuseBox != null)
        {
            fuseBox.enabled = true;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = true;
        }
    }

    private void OnFuseBoxReached()
    {
        if (!isBlackout) return;

        if (fuseBoxSwitchSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(fuseBoxClipName);
            if (clip != null) fuseBoxSwitchSource.PlayOneShot(clip);
        }

        if (fuseBox != null)
        {
            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(Stage6_HallucinationSequence());
    }

    private IEnumerator Stage6_HallucinationSequence()
    {
        if (mainRoomLight != null) mainRoomLight.enabled = true;
        isBlackout = false;

        yield return new WaitForSeconds(0.5f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(true);

        if (hallucinationSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(hallucinationClipName);
            if (clip != null) hallucinationSource.PlayOneShot(clip);
        }

        yield return new WaitForSeconds(1.5f);

        if (mainRoomLight != null) mainRoomLight.enabled = false;

        if (lightFlickerSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(lightFlickerClipName);
            if (clip != null)
            {
                lightFlickerSource.clip = clip;
                lightFlickerSource.loop = true;
                lightFlickerSource.Play();
            }
        }

        yield return new WaitForSeconds(0.2f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);
        if (mainRoomLight != null) mainRoomLight.enabled = true;

        // [추가 연출] 조명이 복구될 때 켜지는 소리
        if (fuseBoxSwitchSource != null && AudioManager.Instance != null)
        {
            AudioClip restoreClip = AudioManager.Instance.GetClip(lightRestoreClipName);
            if (restoreClip != null) fuseBoxSwitchSource.PlayOneShot(restoreClip);
        }

        if (lightFlickerSource != null && lightFlickerSource.isPlaying) lightFlickerSource.Stop();

        yield return new WaitForSeconds(2.0f);
        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
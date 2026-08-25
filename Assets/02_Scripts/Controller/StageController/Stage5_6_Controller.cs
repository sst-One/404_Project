using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Lighting Settings (그룹 최적화)")]
    [Tooltip("모든 조명을 자식으로 묶어둔 부모 빈 오브젝트를 이곳에 할당하세요.")]
    public GameObject mainRoomLightGroup;

    [Header("References")]
    public InteractableItem fuseBox;
    public GameObject hallucinationDecals;

    [Header("Audio Clip Names")]
    public string fuseBoxClipName = "SND-031_FuseBoxSwitch_OneShot";
    public string hallucinationClipName = "SND-011_Hallucination_OneShot";
    public string lightFlickerClipName = "SND-012_LightFlicker_Loop_OneShot";
    public string lightRestoreClipName = "SND-032_LIghtRestore_OneShot";

    private bool isBlackout = false;

    private void Start()
    {
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);

        if (fuseBox != null)
        {
            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;
            fuseBox.onInteractEvent.RemoveAllListeners();
            fuseBox.onInteractEvent.AddListener(OnFuseBoxReached);
        }

        StartCoroutine(Stage5_ClueSequence());
    }

    private IEnumerator Stage5_ClueSequence()
    {
        yield return new WaitForSeconds(8.0f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);
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

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(fuseBoxClipName, AudioManager.Instance.sfxMixerGroup);

        if (fuseBox != null)
        {
            fuseBox.enabled = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(Stage6_HallucinationSequence());
    }

    private IEnumerator Stage6_HallucinationSequence()
    {
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        isBlackout = false;

        yield return new WaitForSeconds(0.5f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(hallucinationClipName, AudioManager.Instance.playerStatusMixerGroup);

        yield return new WaitForSeconds(1.5f);

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);

        // [최적화] AudioSource 변수 삭제. 0.4초간의 짧은 지직거림은 Global 2D로 한 번만 재생
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(lightFlickerClipName))
        {
            AudioManager.Instance.PlayGlobal2D(lightFlickerClipName, AudioManager.Instance.sfxMixerGroup);
        }

        yield return new WaitForSeconds(0.4f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(lightRestoreClipName, AudioManager.Instance.sfxMixerGroup);

        yield return new WaitForSeconds(2.0f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
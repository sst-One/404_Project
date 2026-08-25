using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Narrative Phone (UI 팝업용)")]
    public PhoneUIController narrativePhone;

    [Header("Lighting Settings")]
    public GameObject mainRoomLightGroup;

    [Header("References")]
    public InteractableItem fuseBox;
    public InteractableItem clueItem;
    public GameObject hallucinationDecals;

    [Header("Item Audio Reference")]
    public AudioSource lightFlickerSource;

    [Header("Audio Clip Names")]
    public string disasterAlertClip = "SND-028_Alert_OneShot";
    public string clueMonologueClip = "SND-039_ClueMonologue_OneShot";
    public string fuseBoxClipName = "SND-031_FuseBoxSwitch_OneShot";
    public string hallucinationClipName = "SND-011_Hallucination_OneShot";
    public string lightFlickerClipName = "SND-012_LightFlicker_Loop_OneShot";
    public string lightRestoreClipName = "SND-032_LIghtRestore_OneShot";

    private bool isBlackout = false;
    private bool hasFoundClue = false;

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

        if (clueItem != null)
        {
            clueItem.onInteractEvent.RemoveAllListeners();
            clueItem.onInteractEvent.AddListener(OnClueFound);
        }

        StartCoroutine(Day3IntroSequence());
    }

    private IEnumerator Day3IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(3));

        yield return new WaitForSeconds(1.0f);

        if (narrativePhone != null)
        {
            narrativePhone.gameObject.SetActive(true);
            narrativePhone.ShowDay3Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);

            yield return new WaitForSeconds(5.0f);
            narrativePhone.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(8.0f);
        if (!isBlackout) TriggerBlackout();
    }

    private void OnClueFound()
    {
        if (hasFoundClue || isBlackout) return;
        hasFoundClue = true;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(clueMonologueClip, AudioManager.Instance.voiceMixerGroup);
        TriggerBlackout();
    }

    private void TriggerBlackout()
    {
        if (isBlackout) return;
        isBlackout = true;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(fuseBoxClipName, AudioManager.Instance.sfxMixerGroup);

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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(hallucinationClipName, AudioManager.Instance.playerStatusMixerGroup);

        yield return new WaitForSeconds(1.5f);
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);

        if (lightFlickerSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(lightFlickerClipName);
            if (clip != null) { lightFlickerSource.clip = clip; lightFlickerSource.loop = true; lightFlickerSource.Play(); }
        }

        yield return new WaitForSeconds(0.4f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(lightRestoreClipName, AudioManager.Instance.sfxMixerGroup);
        if (lightFlickerSource != null && lightFlickerSource.isPlaying) lightFlickerSource.Stop();

        yield return new WaitForSeconds(2.0f);
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
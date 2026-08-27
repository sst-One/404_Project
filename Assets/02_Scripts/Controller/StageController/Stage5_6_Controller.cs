using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Lighting Settings")]
    public GameObject mainRoomLightGroup;
    public bool affectAmbientLight = true;
    private Color _originalAmbientColor;

    [Header("References")]
    public InteractableItem fuseBox;
    public InteractableItem clueItem; // 반지 단서 아이템
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
        _originalAmbientColor = RenderSettings.ambientLight;

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);

        if (fuseBox != null)
        {
            fuseBox.isInteractable = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;
            fuseBox.onInteractAction -= OnFuseBoxReached;
            fuseBox.onInteractAction += OnFuseBoxReached;
        }

        if (clueItem != null)
        {
            clueItem.onInteractAction -= OnClueFound;
            clueItem.onInteractAction += OnClueFound;
        }

        StartCoroutine(Day3IntroSequence());
    }

    private void OnDestroy()
    {
        if (fuseBox != null) fuseBox.onInteractAction -= OnFuseBoxReached;
        if (clueItem != null) clueItem.onInteractAction -= OnClueFound;
    }

    private IEnumerator Day3IntroSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(3));
        yield return new WaitForSeconds(1.0f);

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDay3Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);
            yield return new WaitForSeconds(5.0f);
            PhoneController.Instance.HidePhone();
        }

        if (clueItem != null) clueItem.EnableInteractionWithLight();
    }

    private void OnClueFound()
    {
        if (hasFoundClue || isBlackout) return;
        hasFoundClue = true;

        // [간소화 반영] 전역 플래그 없이 순수하게 단서(반지) 오브젝트만 비활성화 처리
        if (clueItem != null)
        {
            clueItem.gameObject.SetActive(false);
        }

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(clueMonologueClip, AudioManager.Instance.voiceMixerGroup);
        TriggerBlackout();
    }

    private void TriggerBlackout()
    {
        if (isBlackout) return;
        isBlackout = true;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);

        if (fuseBox != null)
        {
            fuseBox.transform.SetParent(null);
            fuseBox.gameObject.SetActive(true);
        }

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);

        if (affectAmbientLight) RenderSettings.ambientLight = Color.black;
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDefaultScreen();
        }

        StartCoroutine(DelayedEnableFuseBox());
    }

    private IEnumerator DelayedEnableFuseBox()
    {
        yield return new WaitForSeconds(0.1f);
        if (fuseBox != null) fuseBox.EnableInteractionWithLight();
    }

    private void OnFuseBoxReached()
    {
        if (!isBlackout) return;
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(fuseBoxClipName, AudioManager.Instance.sfxMixerGroup);

        if (fuseBox != null)
        {
            fuseBox.isInteractable = false;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(Stage6_HallucinationSequence());
    }

    private IEnumerator Stage6_HallucinationSequence()
    {
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (affectAmbientLight) RenderSettings.ambientLight = _originalAmbientColor;
        isBlackout = false;

        yield return new WaitForSeconds(0.5f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(hallucinationClipName, AudioManager.Instance.playerStatusMixerGroup);

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartCameraShake(0.3f, 0.15f);
        }

        yield return new WaitForSeconds(1.5f);

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);
        if (affectAmbientLight) RenderSettings.ambientLight = Color.black;

        if (lightFlickerSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(lightFlickerClipName);
            if (clip != null) { lightFlickerSource.clip = clip; lightFlickerSource.loop = true; lightFlickerSource.Play(); }
        }

        yield return new WaitForSeconds(0.4f);

        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (affectAmbientLight) RenderSettings.ambientLight = _originalAmbientColor;

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(lightRestoreClipName, AudioManager.Instance.sfxMixerGroup);
        if (lightFlickerSource != null && lightFlickerSource.isPlaying) lightFlickerSource.Stop();

        if (PhoneController.Instance != null) PhoneController.Instance.HidePhone();

        yield return new WaitForSeconds(2.0f);
        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage7_Gem);
    }
}
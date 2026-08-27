using System.Collections;
using UnityEngine;

public class Stage5_6_Controller : MonoBehaviour
{
    [Header("Lighting Settings")]
    public GameObject mainRoomLightGroup;

    // [신규 추가] 암전 시 집안의 전체적인 환경광까지 완벽히 차단하여 완전한 칠흑을 만듭니다.
    [Tooltip("암전 시 조작할 환경광(Ambient Color)을 완전 검정색으로 할지 여부")]
    public bool affectAmbientLight = true;
    private Color _originalAmbientColor;

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
        // 씬 시작 시 원래 환경광 색상을 저장해둡니다.
        _originalAmbientColor = RenderSettings.ambientLight;

        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (hallucinationDecals != null) hallucinationDecals.SetActive(false);

        if (fuseBox != null)
        {
            fuseBox.isInteractable = false;
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
        // UI-005 Day Transition (기능정의서 SCN-002, UI_화면명세 연동)
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(3));

        yield return new WaitForSeconds(1.0f);

        // FEAT-014 Phone UI Controller (전화/문자 전환)
        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDay3Message();

            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(disasterAlertClip, AudioManager.Instance.uiMixerGroup);

            yield return new WaitForSeconds(5.0f);

            PhoneController.Instance.HidePhone();
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

    // EVT-021: 정전/두꺼비집 복구
    private void TriggerBlackout()
    {
        if (isBlackout) return;
        isBlackout = true;

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage6_Blackout);

        // 전등 그룹 소등
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);

        // [신규 추가] 환경광 강제 소등 (완전 암흑)
        if (affectAmbientLight) RenderSettings.ambientLight = Color.black;

        // Heartbeat 급상승 (EVT-021 명세)
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        // 정전 시 휴대폰 화면 빛(FEAT-021)에 의존하여 탐색
        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDefaultScreen();
        }

        // 두꺼비집 상호작용 개방
        if (fuseBox != null)
        {
            fuseBox.isInteractable = true;
            if (fuseBox.GetComponent<Collider>() != null) fuseBox.GetComponent<Collider>().enabled = true;
        }
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

        // 전기 복구 직후 피/얼굴 환각 노출 (EVT-025)
        StartCoroutine(Stage6_HallucinationSequence());
    }

    private IEnumerator Stage6_HallucinationSequence()
    {
        // 전등 그룹 점등 및 환경광 원상 복구
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(true);
        if (affectAmbientLight) RenderSettings.ambientLight = _originalAmbientColor;
        isBlackout = false;

        yield return new WaitForSeconds(0.5f);

        // 환각 즉시 노출 (EVT-025, 3D-023/3D-024)
        if (hallucinationDecals != null) hallucinationDecals.SetActive(true);

        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayGlobal2D(hallucinationClipName, AudioManager.Instance.playerStatusMixerGroup);

        // 심장 박동 급증 및 카메라 강제 진동 쇼크 (EVT-025 명세)
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.StartCameraShake(0.3f, 0.15f);
        }

        // 환각 노출 시간 (Hold 1.5~3s)
        yield return new WaitForSeconds(1.5f);

        // 다시 암전 (Blackout)
        if (mainRoomLightGroup != null) mainRoomLightGroup.SetActive(false);
        if (affectAmbientLight) RenderSettings.ambientLight = Color.black;

        if (lightFlickerSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(lightFlickerClipName);
            if (clip != null) { lightFlickerSource.clip = clip; lightFlickerSource.loop = true; lightFlickerSource.Play(); }
        }

        yield return new WaitForSeconds(0.4f);

        // 환각 증발 및 현실 복구 완료 (EVT-025)
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
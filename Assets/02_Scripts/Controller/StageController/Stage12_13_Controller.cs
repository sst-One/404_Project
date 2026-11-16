using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem endingGem;
    public InteractableItem tvRemote;

    [Header("Ring Visual Reference (Scene Object)")]
    public GameObject sceneRingObject;

    [Header("Item Audio Reference")]
    public AudioSource tvAudioSource;

    [Header("TV Video Reference")]
    public GameObject tvScreenDisplay;
    public Canvas tvCanvas;
    public VideoPlayer tvVideoPlayer;

    [Header("Audio Clip Names")]
    public string policeMessageAlertClip = NarrativeData.PoliceMessageSound;
    public string tvNewsClipName = "SND-060_NewsReport_Loop";
    public string tinnitusClipName = "SND-061_TinnitusRing_Loop_Timed";
    public string gemDropClipName = "SND-062_GemDrop_OneShot";
    public string flashbackSnd1 = "SND-068_MemoryCut_OneShot";
    public string flashbackSnd2 = "SND-069_Flashback_Layer2";
    public string flashbackSnd3 = "SND-070_Flashback_Layer3";
    private const float NewsVolumeScale = 0.75f;

    private RawImage[] dynamicFlashbackImages = new RawImage[3];
    private bool isTvTurnedOn = false;

    private void Start()
    {
        if (PhoneController.Instance != null) PhoneController.Instance.HidePhone();
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();
        if (endingGem != null)
        {
            StageVisuals.ReplaceGem(endingGem);
            endingGem.isInteractable = false;
            endingGem.gameObject.SetActive(false);
        }
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(false);

        if (sceneRingObject != null) sceneRingObject.SetActive(false);

        if (tvRemote != null)
        {
            tvRemote.isInteractable = false;
            if (tvRemote.GetComponent<Collider>() != null) tvRemote.GetComponent<Collider>().enabled = false;
            tvRemote.onInteractAction -= OnTvRemoteClicked;
            tvRemote.onInteractAction += OnTvRemoteClicked;
        }

        StartCoroutine(MasterSequenceRoutine());
    }

    private void OnDestroy()
    {
        if (tvRemote != null) tvRemote.onInteractAction -= OnTvRemoteClicked;
        if (endingGem != null) endingGem.onInteractAction -= OnGemReached;
    }

    private IEnumerator MasterSequenceRoutine()
    {
        // 1단계 : 휴대폰 화면 보여주기
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(5));
        yield return new WaitForSeconds(1.0f);

        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);
        bool hasMessageScreen = phone != null && phone.phoneUI != null && phone.phoneUI.policeReceiveScreen != null;

        if (hasMessageScreen)
        {
            phone.ShowPhoneInHand();
            phone.phoneUI.ShowLockDay5();
        }
        else
        {
            Debug.LogWarning("Day 5 police message screen is unavailable.", this);
        }

        if (hasMessageScreen && AudioManager.Instance != null && !string.IsNullOrEmpty(policeMessageAlertClip))
        {
            AudioManager.Instance.PlayGlobal2D(policeMessageAlertClip, AudioManager.Instance.uiMixerGroup, 0.75f);
        }

        if (hasMessageScreen)
        {
            yield return new WaitForSeconds(1f);
            phone.phoneUI.ShowPoliceReceiveScreen();
            phone.phoneUI.SetCrackedScreen(true);
            yield return new WaitForSeconds(7f);
            phone.HidePhone();
        }

        isTvTurnedOn = false;
        if (tvRemote != null) tvRemote.EnableInteractionWithLight();

        while (!isTvTurnedOn)
        {
            yield return null;
        }

        // 2단계 : 뉴스 시청
        if (tvCanvas != null) tvCanvas.gameObject.SetActive(true);
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(true);
        AudioSource videoAudio = null;
        if (tvVideoPlayer != null)
        {
            if (tvVideoPlayer.controlledAudioTrackCount > 0 && tvVideoPlayer.audioOutputMode == VideoAudioOutputMode.AudioSource)
            {
                videoAudio = tvVideoPlayer.GetTargetAudioSource(0);
                if (videoAudio != null) videoAudio.volume *= NewsVolumeScale;
            }
            else if (tvVideoPlayer.controlledAudioTrackCount > 0 && tvVideoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
                tvVideoPlayer.SetDirectAudioVolume(0, tvVideoPlayer.GetDirectAudioVolume(0) * NewsVolumeScale);
            tvVideoPlayer.Play();
        }

        float newsDuration = 10.0f; // 기본 안전 대기 시간
        if (tvAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null)
            {
                if (tvAudioSource != videoAudio) tvAudioSource.volume *= NewsVolumeScale;
                tvAudioSource.clip = clip;
                tvAudioSource.Play();
                newsDuration = clip.length; // 실제 오디오 클립 길이 적용
            }
        }

        float halfNewsDuration = Mathf.Max(2.0f, newsDuration * 0.5f);
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(NarrativeData.Day5_TVNews_1, halfNewsDuration));
            if (endingGem != null) endingGem.gameObject.SetActive(true);
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(NarrativeData.Day5_TVNews_2, Mathf.Max(2f, newsDuration - halfNewsDuration)));
        }
        else yield return new WaitForSeconds(newsDuration);

        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(gemDropClipName, AudioManager.Instance.sfxMixerGroup);

        bool isGemInteracted = false;
        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(true);
            endingGem.EnableInteractionWithLight();

            System.Action gemCallback = () => { isGemInteracted = true; };
            endingGem.onInteractAction -= gemCallback;
            endingGem.onInteractAction += gemCallback;

            while (!isGemInteracted)
            {
                yield return null;
            }
            endingGem.onInteractAction -= gemCallback;
        }

        if (sceneRingObject != null)
        {
            Camera itemCamera = null;
            GameObject itemCamObj = GameObject.Find("Item_Camera");
            if (itemCamObj != null) itemCamera = itemCamObj.GetComponent<Camera>();
            if (itemCamera == null && PlayerController.Instance != null) itemCamera = PlayerController.Instance.mainCamera;

            if (itemCamera != null)
            {
                sceneRingObject.transform.SetParent(itemCamera.transform);
                sceneRingObject.layer = LayerMask.NameToLayer("FP_Item");
                foreach (Transform child in sceneRingObject.GetComponentsInChildren<Transform>(true))
                {
                    child.gameObject.layer = LayerMask.NameToLayer("FP_Item");
                }

                sceneRingObject.transform.localPosition = new Vector3(0.3264f, -0.2058f, 0.4325f);
                sceneRingObject.transform.localRotation = Quaternion.Euler(-38.699f, 60.659f, -185.19f);
            }

            sceneRingObject.SetActive(true);
        }
        OnGemReached();
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.ShowSubtitleForDuration(NarrativeData.Day5_Gem, 2.8f));
        else
            yield return new WaitForSeconds(2.8f);

        yield return StartCoroutine(FlashbackAndEndingSequence());
    }

    private void OnTvRemoteClicked()
    {
        if (isTvTurnedOn) return;
        isTvTurnedOn = true;

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D("SND-013_ButtonClick_OneShot", AudioManager.Instance.sfxMixerGroup);
        }

        if (tvRemote != null)
        {
            tvRemote.isInteractable = false;
            if (tvRemote.GetComponent<Collider>() != null) tvRemote.GetComponent<Collider>().enabled = false;
            tvRemote.onInteractAction -= OnTvRemoteClicked;
        }
    }

    private void OnGemReached()
    {
        if (endingGem != null)
        {
            endingGem.isInteractable = false;
            if (endingGem.GetComponent<Collider>() != null) endingGem.GetComponent<Collider>().enabled = false;
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);
    }

    private IEnumerator FlashbackAndEndingSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(1);

        Transform panelTransform = null;
        if (UIManager.Instance != null)
        {
            panelTransform = FindChildRecursive(UIManager.Instance.transform, "Flashback_Panel");
        }
        else
        {
            GameObject canvasObj = GameObject.Find("UI_Canvas");
            if (canvasObj != null) panelTransform = FindChildRecursive(canvasObj.transform, "Flashback_Panel");
        }

        if (panelTransform != null)
        {
            panelTransform.gameObject.SetActive(true);

            int imgCount = 0;
            foreach (Transform child in panelTransform)
            {
                RawImage ri = child.GetComponent<RawImage>();
                if (ri != null && imgCount < dynamicFlashbackImages.Length)
                {
                    dynamicFlashbackImages[imgCount] = ri;
                    ri.gameObject.SetActive(false);
                    imgCount++;
                }
            }
        }

        if (dynamicFlashbackImages[0] != null && dynamicFlashbackImages[0].texture != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(flashbackSnd1, AudioManager.Instance.playerStatusMixerGroup);
            dynamicFlashbackImages[0].gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(4.5f);

        if (dynamicFlashbackImages[1] != null && dynamicFlashbackImages[1].texture != null)
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayGlobal2D("SND-064_FlashBackDistortion_OneShot", AudioManager.Instance.playerStatusMixerGroup, 0.7f);
                if (!string.IsNullOrEmpty(flashbackSnd2))
                    AudioManager.Instance.PlayGlobal2D(flashbackSnd2, AudioManager.Instance.playerStatusMixerGroup);
            }
            dynamicFlashbackImages[1].gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(4.5f);

        if (dynamicFlashbackImages[2] != null && dynamicFlashbackImages[2].texture != null)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(flashbackSnd3, AudioManager.Instance.playerStatusMixerGroup);
            dynamicFlashbackImages[2].gameObject.SetActive(true);
        }
        yield return new WaitForSeconds(4.5f);

        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (panelTransform != null)
        {
            panelTransform.gameObject.SetActive(false);
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
        {
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }
}

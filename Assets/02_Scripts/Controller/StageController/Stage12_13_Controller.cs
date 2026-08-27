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

    [Header("Flashback Overlays")]
    public Image[] flashbackImages;

    [Header("Audio Clip Names")]
    public string policeMessageAlertClip = "SND-058_PhoneRing_Loop";
    public string tvNewsClipName = "SND-060_NewsReport_Loop";
    public string tinnitusClipName = "SND-061_TinnitusRing_Loop_Timed";
    public string gemDropClipName = "SND-062_GemDrop_OneShot";
    public string flashbackSnd1 = "SND-068_Flashback_Layer1";
    public string flashbackSnd2 = "SND-069_Flashback_Layer2";
    public string flashbackSnd3 = "SND-070_Flashback_Layer3";

    private bool isTvTurnedOn = false;

    private void Start()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();
        if (endingGem != null) endingGem.gameObject.SetActive(false);
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(false);

        if (sceneRingObject != null) sceneRingObject.SetActive(false);

        if (flashbackImages != null)
        {
            foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }
        }

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
        // Day 5 전환 연출
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.ShowDayTransition(5));
        yield return new WaitForSeconds(1.0f);

        // ----------------------------------------------------
        // [시퀀스 1] 경찰에게서 문자(메시지) 확인 (상호작용 없음)
        // ----------------------------------------------------
        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null)
            {
                PhoneController.Instance.phoneUI.SetCrackedScreen(true);
                PhoneController.Instance.phoneUI.ShowPoliceReceive();
            }
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(policeMessageAlertClip))
        {
            AudioManager.Instance.PlayGlobal2D(policeMessageAlertClip, AudioManager.Instance.sfxMixerGroup);
        }

        // 문자 확인 연출 3초 대기
        yield return new WaitForSeconds(3.0f);

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.HidePhone();
        }

        // ----------------------------------------------------
        // [시퀀스 2] 리모컨 상호작용 후 뉴스 시청
        // ----------------------------------------------------
        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_Police);
        yield return new WaitForSeconds(2.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        isTvTurnedOn = false;
        if (tvRemote != null) tvRemote.EnableInteractionWithLight();

        while (!isTvTurnedOn)
        {
            yield return null;
        }

        if (tvCanvas != null) tvCanvas.gameObject.SetActive(true);
        if (tvScreenDisplay != null) tvScreenDisplay.SetActive(true);
        if (tvVideoPlayer != null) tvVideoPlayer.Play();

        if (tvAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(tvNewsClipName);
            if (clip != null) { tvAudioSource.clip = clip; tvAudioSource.Play(); }
        }

        if (UIManager.Instance != null) UIManager.Instance.ShowSubtitle(NarrativeData.Day5_TVNews);
        if (AudioManager.Instance != null) AudioManager.Instance.PlayStatusSound(tinnitusClipName);
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        yield return new WaitForSeconds(6.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        // ----------------------------------------------------
        // [시퀀스 3] 뉴스 이후 보석 등장
        // ----------------------------------------------------
        if (AudioManager.Instance != null) AudioManager.Instance.PlayGlobal2D(gemDropClipName, AudioManager.Instance.sfxMixerGroup);

        bool isGemInteracted = false;
        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(true);
            endingGem.EnableInteractionWithLight();

            // [컴파일 픽스] onInteractAction = null; 제거 완료. 표준 -= 및 += 연산 사용
            System.Action gemCallback = () => { isGemInteracted = true; };
            endingGem.onInteractAction -= gemCallback;
            endingGem.onInteractAction += gemCallback;

            while (!isGemInteracted)
            {
                yield return null;
            }
            endingGem.onInteractAction -= gemCallback;
        }

        // ----------------------------------------------------
        // [시퀀스 4] 보석 상호작용 -> Item_Camera 찾아 오른손 위치로 반지 장착 후 활성화
        // ----------------------------------------------------
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
                sceneRingObject.transform.localPosition = new Vector3(0.15f, -0.2f, 0.5f);
                sceneRingObject.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }

            sceneRingObject.SetActive(true);
        }
        OnGemReached();

        // ----------------------------------------------------
        // [시퀀스 5] 2초 대기 후 플래시백 작동
        // ----------------------------------------------------
        yield return new WaitForSeconds(2.0f);

        // ----------------------------------------------------
        // [시퀀스 6] 플래시백 연출 및 엔딩 씬 이동
        // ----------------------------------------------------
        yield return StartCoroutine(FlashbackAndEndingSequence());
    }

    private void OnTvRemoteClicked()
    {
        if (isTvTurnedOn) return;
        isTvTurnedOn = true;

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
    }

    private IEnumerator FlashbackAndEndingSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayGlobal2D(flashbackSnd1, AudioManager.Instance.playerStatusMixerGroup);
            AudioManager.Instance.PlayGlobal2D(flashbackSnd2, AudioManager.Instance.playerStatusMixerGroup);
            AudioManager.Instance.PlayGlobal2D(flashbackSnd3, AudioManager.Instance.playerStatusMixerGroup);
        }

        float duration = 12.0f;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            if (flashbackImages != null)
            {
                foreach (var img in flashbackImages)
                {
                    if (img != null)
                    {
                        bool isVisible = Random.value > 0.5f;
                        img.gameObject.SetActive(isVisible);
                        if (isVisible)
                        {
                            Color c = img.color;
                            c.a = Random.Range(0.2f, 0.8f);
                            img.color = c;
                        }
                    }
                }
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        if (flashbackImages != null)
        {
            foreach (var img in flashbackImages) { if (img != null) img.gameObject.SetActive(false); }
        }

        if (AudioManager.Instance != null) AudioManager.Instance.StopStatusSound();
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
    }
}
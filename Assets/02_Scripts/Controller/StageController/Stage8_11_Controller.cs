using System.Collections;
using UnityEngine;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("Stage 8: Encounter References")]
    public Animator intruderAnimator;

    // [핫픽스] public PhoneController phoneController; 변수 삭제 (인스펙터 종속성 해제)

    [Tooltip("폰이 미끄러져 멈출 바닥의 목표 위치")]
    public Transform phoneDropTarget;

    [Header("Stage 8: Encounter Settings")]
    public float strikeDelay = 0.5f;
    public float dropDuration = 0.8f;

    [Header("Audio Clip Names")]
    public string silentAmbienceClip = "SND-010_SilentTension_Loop";
    public string knifeStrikeClipName = "SND-047_KnifeStrike_OneShot";
    public string policeSirenClipName = "SND-055_PoliceSiren_Loop";
    public string doorKnockClipName = "SND-057_DoorKnock_OneShot";

    private bool hasEncounterTriggered = false;

    private void Start()
    {
        EnemyAI enemy = FindObjectOfType<EnemyAI>(true);
        if (enemy != null)
        {
            bool isChaseStage = (GameFlowManager.Instance != null &&
                                 GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                                 GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);
            enemy.gameObject.SetActive(isChaseStage);
            enemy.isNarrativeMode = false;
        }

        // [핫픽스] 인스펙터 변수 대신 싱글톤 인스턴스에 다이렉트 이벤트 리스너 부착
        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.onCallSuccess.RemoveAllListeners();
            PhoneController.Instance.onCallSuccess.AddListener(OnStage11Completed);
            PhoneController.Instance.currentState = PhoneState.Idle;
        }

        StartCoroutine(InitStageSequence());
    }

    private IEnumerator InitStageSequence()
    {
        // 씬 시작 시 조용한 앰비언스 재생
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(silentAmbienceClip))
        {
            AudioManager.Instance.PlayGlobal2D(silentAmbienceClip, AudioManager.Instance.sfxMixerGroup);
        }

        // PhoneController의 Start()가 먼저 실행되어 HidePhone()이 작동할 수 있도록 한 프레임 대기
        yield return null;

        // [핫픽스] 플레이어가 아무 영문도 모른 채 기본 폰 화면을 들고 걷도록 강제 세팅
        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null)
            {
                PhoneController.Instance.phoneUI.ShowDefaultScreen();
            }
        }
    }

    // 인스펙터 Event Trigger 충돌 시 호출되는 함수
    public void StartEncounterEvent()
    {
        if (!hasEncounterTriggered)
        {
            hasEncounterTriggered = true;
            StartCoroutine(EncounterSequence());
        }
    }

    private IEnumerator EncounterSequence()
    {
        if (intruderAnimator != null) intruderAnimator.SetTrigger("Strike");

        yield return new WaitForSeconds(strikeDelay);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(knifeStrikeClipName))
        {
            AudioManager.Instance.PlayGlobal2D(knifeStrikeClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        // [핫픽스] 싱글톤을 직접 추적하여 부모 종속을 끊고 바닥으로 던짐
        if (PhoneController.Instance != null && phoneDropTarget != null)
        {
            PhoneController.Instance.transform.SetParent(null);
            PhoneController.Instance.TriggerDrop(phoneDropTarget);
            yield return new WaitForSeconds(dropDuration);
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage9_Hide);
    }

    private void OnStage11Completed()
    {
        if (StateManager.Instance != null)
        {
            StateManager.Instance.RemoveThreat();
            StateManager.Instance.ResetHeartbeat();
        }

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        foreach (GameObject enemyObj in enemies) enemyObj.SetActive(false);

        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));
        else yield return new WaitForSeconds(2.0f);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(policeSirenClipName))
        {
            AudioManager.Instance.PlayGlobal2D(policeSirenClipName, AudioManager.Instance.sfxMixerGroup);
        }

        yield return new WaitForSeconds(4.0f);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(doorKnockClipName))
        {
            AudioManager.Instance.PlayGlobal2D(doorKnockClipName, AudioManager.Instance.sfxMixerGroup);
        }

        yield return new WaitForSeconds(4.0f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
    }
}
using System.Collections;
using UnityEngine;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("Stage 8: Encounter References")]
    public Animator intruderAnimator;
    public Transform phoneDropTarget;

    [Header("Respawn Settings")]
    public Transform playerSpawnPoint;
    public string deathJumpscareClip = "SND-011_Hallucination_OneShot";

    [Header("Stage 8: Encounter Settings")]
    public float strikeDelay = 0.5f;
    public float dropDuration = 0.8f;

    [Header("Audio Clip Names")]
    public string silentAmbienceClip = "SND-010_SilentTension_Loop";
    public string knifeStrikeClipName = "SND-047_KnifeStrike_OneShot";
    public string policeSirenClipName = "SND-055_PoliceSiren_Loop";
    public string doorKnockClipName = "SND-057_DoorKnock_OneShot";

    private bool hasEncounterTriggered = false;
    private EnemyAI _enemyAI;

    private void Start()
    {
        _enemyAI = FindObjectOfType<EnemyAI>(true);
        if (_enemyAI != null)
        {
            bool isChaseStage = (GameFlowManager.Instance != null &&
                                 GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                                 GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);
            _enemyAI.gameObject.SetActive(isChaseStage);
            _enemyAI.isNarrativeMode = false;

            _enemyAI.OnPlayerCaught += HandlePlayerCaught;
        }

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.onCallSuccess.RemoveAllListeners();
            PhoneController.Instance.onCallSuccess.AddListener(OnStage11Completed);
            PhoneController.Instance.currentState = PhoneState.Idle;
        }

        StartCoroutine(InitStageSequence());
    }

    private void OnDestroy()
    {
        if (_enemyAI != null) _enemyAI.OnPlayerCaught -= HandlePlayerCaught;
    }

    private IEnumerator InitStageSequence()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(silentAmbienceClip))
        {
            AudioManager.Instance.PlayGlobal2D(silentAmbienceClip, AudioManager.Instance.sfxMixerGroup);
        }

        yield return null;

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.ShowPhoneInHand();
            if (PhoneController.Instance.phoneUI != null) PhoneController.Instance.phoneUI.ShowDefaultScreen();
        }
    }

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
        // 최초 조우 시 칼부림 연출
        if (intruderAnimator != null) intruderAnimator.SetTrigger("Strike");

        yield return new WaitForSeconds(strikeDelay);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(knifeStrikeClipName))
        {
            AudioManager.Instance.PlayGlobal2D(knifeStrikeClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        // 최초 조우 시에만 핸드폰을 바닥에 떨어뜨림
        if (PhoneController.Instance != null && phoneDropTarget != null)
        {
            PhoneController.Instance.transform.SetParent(null);
            PhoneController.Instance.TriggerDrop(phoneDropTarget);
            yield return new WaitForSeconds(dropDuration);
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage9_Hide);
    }

    private void HandlePlayerCaught(Transform enemyTransform)
    {
        StartCoroutine(CaughtSequence(enemyTransform));
    }

    private IEnumerator CaughtSequence(Transform enemyTransform)
    {
        // 1. 조작 잠금 및 사운드 점프스케어
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementLock(true);
            PlayerController.Instance.SetCameraLock(true);
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathJumpscareClip))
        {
            AudioManager.Instance.PlayGlobal2D(deathJumpscareClip, AudioManager.Instance.sfxMixerGroup);
        }

        // 2. 시야 강제 고정
        Camera cam = Camera.main;
        if (cam != null && enemyTransform != null)
        {
            Vector3 targetDir = (enemyTransform.position + Vector3.up * 1.5f) - cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(targetDir);

            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                cam.transform.rotation = Quaternion.Slerp(startRot, targetRot, t / 0.4f);
                yield return null;
            }
        }

        yield return new WaitForSeconds(1.0f);

        // 3. 화면 암전
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));
        else yield return new WaitForSeconds(1.5f);

        // ==========================================
        // 4. 상태 및 위치 초기화 (기획 원안 RULE-018: 폰은 회수된 상태면 유지)
        // ==========================================

        if (playerSpawnPoint != null && PlayerController.Instance != null)
        {
            CharacterController cc = PlayerController.Instance.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            PlayerController.Instance.transform.position = playerSpawnPoint.position;
            PlayerController.Instance.transform.rotation = playerSpawnPoint.rotation;
            if (cam != null) cam.transform.localRotation = Quaternion.identity;

            if (cc != null) cc.enabled = true;
        }

        if (_enemyAI != null) _enemyAI.ResetEnemy();

        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        // 5. 화면 밝아짐 및 조작 복구
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.5f));

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementLock(false);
            PlayerController.Instance.SetCameraLock(false);
        }
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
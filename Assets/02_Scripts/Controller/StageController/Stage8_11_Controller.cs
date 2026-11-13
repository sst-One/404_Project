using System.Collections;
using UnityEngine;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("Stage 8: Encounter References")]
    public EnemyAI enemyAI;
    public Animator intruderAnimator;
    public Transform phoneDropTarget;

    [Header("Respawn Settings")]
    public Transform playerSpawnPoint;
    public string deathJumpscareClip = "SND-011_Hallucination_OneShot";
    public string playerScreamClipName = "SND-033_PlayerScream_OneShot";
    [Range(0f, 1f)] public float playerScreamVolume = 0.9f;

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
        if (enemyAI != null)
        {
            bool isChaseStage = (GameFlowManager.Instance != null &&
                                 GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                                 GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);
            enemyAI.gameObject.SetActive(isChaseStage);
            enemyAI.isNarrativeMode = false;
            enemyAI.OnPlayerCaught += HandlePlayerCaught;
        }

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.onCallSuccess -= OnStage11Completed;
            PhoneController.Instance.onCallSuccess += OnStage11Completed;
            PhoneController.Instance.currentState = PhoneState.Idle;
        }

        PrepareHidingSpotInteractions();
        StartCoroutine(InitStageSequence());
    }


    private void PrepareHidingSpotInteractions()
    {
        HidingSpotAction[] hidingSpots = FindObjectsOfType<HidingSpotAction>(true);
        int targetLayer = 6; // Player 프리팹의 targetMask(7744)에 포함된 Interactable 레이어
        if (PlayerController.Instance != null)
        {
            int mask = PlayerController.Instance.targetMask.value;
            for (int i = 0; i < 32; i++)
            {
                if ((mask & (1 << i)) != 0) { targetLayer = i; break; }
            }
        }

        Debug.Log($"[Stage8_11_Controller] 은신처 상호작용 초기화 대상: {hidingSpots.Length}개");
        foreach (HidingSpotAction spot in hidingSpots)
        {
            if (spot == null) continue;
            spot.gameObject.layer = targetLayer;
            spot.interactOnlyOnce = false;
            // Stage 8에서는 아직 은신처가 진행 목표가 아닙니다.
            spot.isInteractable = false;
            spot.DisableObjectiveHighlight();
            Collider[] colliders = spot.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in colliders)
                if (col != null) col.enabled = true;
        }
    }

    private void OnDestroy()
    {
        if (enemyAI != null) enemyAI.OnPlayerCaught -= HandlePlayerCaught;
        if (PhoneController.Instance != null) PhoneController.Instance.onCallSuccess -= OnStage11Completed;
    }

    private IEnumerator InitStageSequence()
    {
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(silentAmbienceClip))
        {
            AudioManager.Instance.PlayGlobal2D(silentAmbienceClip, AudioManager.Instance.sfxMixerGroup);
        }

        yield return null;

        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);

        if (phone != null)
        {
            phone.ShowPhoneInHand();
            if (phone.phoneUI != null) phone.phoneUI.ShowDefaultScreen();
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
        if (intruderAnimator != null) intruderAnimator.SetTrigger("Attack");

        yield return new WaitForSeconds(strikeDelay);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(knifeStrikeClipName))
        {
            AudioManager.Instance.PlayGlobal2D(knifeStrikeClipName, AudioManager.Instance.sfxMixerGroup);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        if (PhoneController.Instance != null && phoneDropTarget != null)
        {
            PhoneController.Instance.transform.SetParent(null);
            PhoneController.Instance.TriggerDrop(phoneDropTarget);
            yield return new WaitForSeconds(dropDuration);
        }

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage9_Hide);
        ArmHidingSpotObjectives();
    }

    private void ArmHidingSpotObjectives()
    {
        HidingSpotAction[] hidingSpots = FindObjectsOfType<HidingSpotAction>(true);
        foreach (HidingSpotAction spot in hidingSpots)
        {
            if (spot == null || !spot.gameObject.activeInHierarchy) continue;
            spot.EnableInteractionWithLight();
        }
    }

    private void HandlePlayerCaught(Transform enemyTransform)
    {
        StartCoroutine(CaughtSequence(enemyTransform));
    }

    private IEnumerator CaughtSequence(Transform enemyTransform)
    {
        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementLock(true);
            PlayerController.Instance.SetCameraLock(true);
        }

        if (AudioManager.Instance != null)
        {
            // 플레이어 비명과 기존 점프스케어 사운드를 함께 재생합니다.
            if (!string.IsNullOrEmpty(playerScreamClipName))
                AudioManager.Instance.PlayGlobal2D(playerScreamClipName, AudioManager.Instance.voiceMixerGroup, playerScreamVolume);
            if (!string.IsNullOrEmpty(deathJumpscareClip))
                AudioManager.Instance.PlayGlobal2D(deathJumpscareClip, AudioManager.Instance.sfxMixerGroup);
        }

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

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(1.5f));
        else yield return new WaitForSeconds(1.5f);

        if (playerSpawnPoint != null && PlayerController.Instance != null)
        {
            if (PlayerController.Instance.CC != null) PlayerController.Instance.CC.enabled = false;
            PlayerController.Instance.transform.position = playerSpawnPoint.position;
            PlayerController.Instance.transform.rotation = playerSpawnPoint.rotation;
            if (cam != null) cam.transform.localRotation = Quaternion.identity;
            if (PlayerController.Instance.CC != null) PlayerController.Instance.CC.enabled = true;
        }

        if (enemyAI != null) enemyAI.ResetEnemy();
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

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
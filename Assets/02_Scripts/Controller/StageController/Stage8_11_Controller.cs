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

        StartCoroutine(InitStageSequence());
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

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(deathJumpscareClip))
        {
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
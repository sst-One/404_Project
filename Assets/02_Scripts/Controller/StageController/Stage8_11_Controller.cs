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
    private Vector3 _initialEnemyPosition;
    private Quaternion _initialEnemyRotation;
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
    [Min(0f)] public float policeSirenDelay = 5.0f;
    public string doorKnockClipName = "SND-057_DoorKnock_OneShot";

    private bool hasEncounterTriggered = false;

    private void Start()
    {
        if (enemyAI != null)
        {
            _initialEnemyPosition = enemyAI.transform.position;
            _initialEnemyRotation = enemyAI.transform.rotation;
            enemyAI.gameObject.SetActive(false);
            enemyAI.enabled = false;
            enemyAI.isNarrativeMode = false;
            enemyAI.chaseSpeed = Mathf.Max(enemyAI.chaseSpeed * 1.30f, 0.65f);
            enemyAI.viewRadius = Mathf.Max(enemyAI.viewRadius * 1.25f, 2.5f);
            enemyAI.OnPlayerCaught += HandlePlayerCaught;
        }

        if (PhoneController.Instance != null)
        {
            PhoneController.Instance.onCallSuccess -= OnStage11Completed;
            PhoneController.Instance.onCallSuccess += OnStage11Completed;
            PhoneController.Instance.currentState = PhoneState.Idle;
        }

        PrepareHidingSpotInteractions();
        if (HidingSpotManager.Instance != null)
        {
            HidingSpotManager.Instance.onFirstHide -= OnPlayerFirstHide;
            HidingSpotManager.Instance.onFirstHide += OnPlayerFirstHide;
        }
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
        if (HidingSpotManager.Instance != null) HidingSpotManager.Instance.onFirstHide -= OnPlayerFirstHide;
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
            if (phone.phoneUI != null) phone.phoneUI.ShowLockDay4();
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
        if (enemyAI != null) enemyAI.gameObject.SetActive(true);
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
        if (UIManager.Instance != null)
            StartCoroutine(UIManager.Instance.ShowGameplayHint("빨리 안전한 곳에 숨어야 해.", 3.5f));
        if (enemyAI != null) enemyAI.enabled = true;
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

    private void OnPlayerFirstHide()
    {
        // 플레이어가 처음 은신한 뒤에야 바닥에 떨어진 휴대폰을 회수할 수 있습니다.
        PhoneController phone = PhoneController.Instance;
        if (phone == null) phone = FindObjectOfType<PhoneController>(true);

        if (phone != null && phone.currentState == PhoneState.Dropped)
            phone.EnableDroppedPhoneRecovery();
        if (UIManager.Instance != null)
            StartCoroutine(UIManager.Instance.ShowGameplayHint("아저씨를 피해 떨어진 휴대폰을 주워야 해.", 3.5f));
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

        Camera cam = PlayerController.Instance != null ? PlayerController.Instance.mainCamera : Camera.main;
        Vector3 normalCameraPosition = cam != null ? cam.transform.localPosition : Vector3.zero;
        Quaternion normalCameraRotation = cam != null ? cam.transform.localRotation : Quaternion.identity;
        if (cam != null && enemyTransform != null)
        {
            Transform head = intruderAnimator != null && intruderAnimator.isHuman
                ? intruderAnimator.GetBoneTransform(HumanBodyBones.Head) : null;
            CapsuleCollider body = enemyTransform.GetComponentInChildren<CapsuleCollider>();
            float height = body != null ? body.bounds.size.y : Mathf.Max(0.15f, enemyTransform.lossyScale.y * 3.8f);
            Vector3 face = head != null ? head.position : body != null
                ? new Vector3(body.bounds.center.x, body.bounds.min.y + height * 0.88f, body.bounds.center.z)
                : enemyTransform.position + Vector3.up * height * 0.88f;
            Vector3 forward = Vector3.ProjectOnPlane(enemyTransform.forward, Vector3.up).normalized;
            Vector3 frontOfFace = face + forward * Mathf.Max(cam.nearClipPlane + 0.15f, height * 0.7f);
            Vector3 startPosition = cam.transform.position;
            Quaternion startRot = cam.transform.rotation;
            Quaternion targetRot = Quaternion.LookRotation(-forward, Vector3.up);

            float t = 0;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                cam.transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, frontOfFace, t / 0.4f),
                    Quaternion.Slerp(startRot, targetRot, t / 0.4f));
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
            if (PlayerController.Instance.CC != null) PlayerController.Instance.CC.enabled = true;
        }
        if (cam != null)
        {
            cam.transform.localPosition = normalCameraPosition;
            cam.transform.localRotation = normalCameraRotation;
            if (PlayerController.Instance != null && PlayerController.Instance.CamLook != null)
                PlayerController.Instance.CamLook.SyncToCurrentLocalRotation();
        }

        if (enemyAI != null)
        {
            enemyAI.gameObject.SetActive(true);
            enemyAI.enabled = true;
            enemyAI.ResetEnemy(_initialEnemyPosition, _initialEnemyRotation);
        }
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

        if (PhoneController.Instance != null) PhoneController.Instance.HidePhone();
        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();
        if (AudioManager.Instance != null) AudioManager.Instance.StopBreathSound();

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));
        else yield return new WaitForSeconds(2.0f);

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(policeSirenClipName))
        {
            float startTime = policeSirenClipName == "SND-056_SirenFade_Loop_Fade" ? 3.75f : 0f;
            AudioManager.Instance.PlayGlobal2D(policeSirenClipName, AudioManager.Instance.sfxMixerGroup, 1f, startTime);
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

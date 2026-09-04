using System.Collections;
using UnityEngine;

public class HidingSpotAction : InteractableItem
{
    [Header("Hiding Spot Transforms")]
    public Transform insidePoint;
    public Transform lookTarget;
    public Transform exitPoint;

    [Header("Settings")]
    public float transitionDuration = 1.0f;
    public string enterClipName = "SND-020_HideEnter_OneShot";

    private bool _isOccupied = false;
    private bool _isTransitioning = false;

    private void Start()
    {
        interactOnlyOnce = false;
        if (HidingSpotManager.Instance != null)
        {
            HidingSpotManager.Instance.onForceEject += ForceEject;
        }
    }

    private void OnDestroy()
    {
        if (HidingSpotManager.Instance != null)
        {
            HidingSpotManager.Instance.onForceEject -= ForceEject;
        }
    }

    public override void OnInteract()
    {
        // [핵심 롤백] 무조건 부모(InteractableItem)의 상태 관리를 먼저 통과시킵니다!
        base.OnInteract();

        if (_isTransitioning) return;

        if (_isOccupied) StartCoroutine(ExitRoutine());
        else StartCoroutine(EnterRoutine());
    }

    private void ForceEject()
    {
        if (_isOccupied && !_isTransitioning) StartCoroutine(ExitRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        _isTransitioning = true;
        _isOccupied = true;
        isInteractable = false;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(enterClipName))
            AudioManager.Instance.PlayGlobal2D(enterClipName, AudioManager.Instance.sfxMixerGroup);

        Transform playerRig = PlayerController.Instance.transform;

        if (PlayerController.Instance.CC != null) PlayerController.Instance.CC.enabled = false;

        FirstPersonCameraLook camLook = PlayerController.Instance.CamLook;
        Transform playerBody = null;
        if (camLook != null)
        {
            camLook.enabled = false;
            playerBody = camLook.playerBody;
        }

        Vector3 startPos = playerRig.position;
        Quaternion startRigRot = playerRig.rotation;
        Quaternion startBodyRot = playerBody != null ? playerBody.localRotation : Quaternion.identity;
        Quaternion startCamRot = camLook != null ? camLook.transform.localRotation : Quaternion.identity;

        Vector3 targetPos = insidePoint != null ? insidePoint.position : startPos;

        Vector3 dirToTarget = playerRig.forward;
        if (lookTarget != null)
        {
            dirToTarget = lookTarget.position - (camLook != null ? camLook.transform.position : targetPos);
            dirToTarget.y = 0;
        }
        Quaternion targetRigRot = dirToTarget != Vector3.zero ? Quaternion.LookRotation(dirToTarget) : startRigRot;

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);

            playerRig.position = Vector3.Lerp(startPos, targetPos, t);
            playerRig.rotation = Quaternion.Slerp(startRigRot, targetRigRot, t);

            if (playerBody != null) playerBody.localRotation = Quaternion.Slerp(startBodyRot, Quaternion.identity, t);
            if (camLook != null) camLook.transform.localRotation = Quaternion.Slerp(startCamRot, Quaternion.identity, t);

            yield return null;
        }

        playerRig.position = targetPos;
        playerRig.rotation = targetRigRot;
        if (playerBody != null) playerBody.localRotation = Quaternion.identity;
        if (camLook != null) camLook.transform.localRotation = Quaternion.identity;

        if (camLook != null)
        {
            camLook.SyncToCurrentLocalRotation();
            camLook.enabled = true;
        }

        isInteractable = true;
        _isTransitioning = false;

        if (HidingSpotManager.Instance != null) HidingSpotManager.Instance.StartHiding();
    }

    private IEnumerator ExitRoutine()
    {
        _isTransitioning = true;
        isInteractable = false;

        if (HidingSpotManager.Instance != null) HidingSpotManager.Instance.StopHiding();

        Transform playerRig = PlayerController.Instance.transform;
        FirstPersonCameraLook camLook = PlayerController.Instance.CamLook;
        Transform playerBody = null;
        if (camLook != null)
        {
            camLook.enabled = false;
            playerBody = camLook.playerBody;
        }

        Vector3 startPos = playerRig.position;
        Vector3 targetPos = exitPoint != null ? exitPoint.position : startPos - playerRig.forward * 1.5f;

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);
            playerRig.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        playerRig.position = targetPos;

        if (camLook != null)
        {
            camLook.SyncToCurrentLocalRotation();
            camLook.enabled = true;
        }

        if (PlayerController.Instance.CC != null) PlayerController.Instance.CC.enabled = true;
        isInteractable = true;

        _isOccupied = false;
        _isTransitioning = false;
    }
}
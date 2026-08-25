using System.Collections;
using UnityEngine;

[RequireComponent(typeof(InteractableItem))]
public class HidingSpotAction : MonoBehaviour
{
    [Header("Hiding Spot Transforms")]
    public Transform insidePoint;
    public Transform lookTarget;
    public Transform exitPoint;

    [Header("Settings")]
    public float transitionDuration = 1.0f;
    public string enterClipName = "SND-020_HideEnter_OneShot";

    private InteractableItem _interactable;
    private CharacterController _playerCC;
    private bool _isOccupied = false;
    private bool _isTransitioning = false;

    private void Start()
    {
        _interactable = GetComponent<InteractableItem>();
        if (_interactable != null)
        {
            _interactable.interactOnlyOnce = false;
            _interactable.onInteractEvent.AddListener(OnInteract);
        }

        if (PlayerController.Instance != null)
        {
            _playerCC = PlayerController.Instance.GetComponent<CharacterController>();
        }

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

    private void OnInteract()
    {
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
        if (_interactable != null) _interactable.isInteractable = false;

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(enterClipName))
            AudioManager.Instance.PlayGlobal2D(enterClipName, AudioManager.Instance.sfxMixerGroup);

        Transform playerRig = PlayerController.Instance.transform;
        if (_playerCC != null) _playerCC.enabled = false;

        // [핫픽스 2] 은신처 진입 시 엉뚱한 곳을 보는 카메라 축 틀어짐 방지
        FirstPersonCameraLook camLook = playerRig.GetComponentInChildren<FirstPersonCameraLook>();
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

            // 진입하면서 마우스로 돌아갔던 카메라 각도를 0점으로 부드럽게 복구
            if (playerBody != null) playerBody.localRotation = Quaternion.Slerp(startBodyRot, Quaternion.identity, t);
            if (camLook != null) camLook.transform.localRotation = Quaternion.Slerp(startCamRot, Quaternion.identity, t);

            yield return null;
        }

        playerRig.position = targetPos;
        playerRig.rotation = targetRigRot;
        if (playerBody != null) playerBody.localRotation = Quaternion.identity;
        if (camLook != null) camLook.transform.localRotation = Quaternion.identity;

        // 정렬이 완료되면 CameraLook 스크립트에 내부 각도가 0이 되었음을 알림
        if (camLook != null)
        {
            camLook.SyncToCurrentLocalRotation();
            camLook.enabled = true;
        }

        if (_interactable != null) _interactable.isInteractable = true;
        _isTransitioning = false;

        if (HidingSpotManager.Instance != null) HidingSpotManager.Instance.StartHiding();
    }

    private IEnumerator ExitRoutine()
    {
        _isTransitioning = true;
        if (_interactable != null) _interactable.isInteractable = false;

        if (HidingSpotManager.Instance != null) HidingSpotManager.Instance.StopHiding();

        Transform playerRig = PlayerController.Instance.transform;

        FirstPersonCameraLook camLook = playerRig.GetComponentInChildren<FirstPersonCameraLook>();
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

        if (_playerCC != null) _playerCC.enabled = true;
        if (_interactable != null) _interactable.isInteractable = true;

        _isOccupied = false;
        _isTransitioning = false;
    }
}
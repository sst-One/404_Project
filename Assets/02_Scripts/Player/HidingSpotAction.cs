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
    public AudioSource hideAudioSource;
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

        if (_isOccupied)
            StartCoroutine(ExitRoutine());
        else
            StartCoroutine(EnterRoutine());
    }

    private void ForceEject()
    {
        if (_isOccupied && !_isTransitioning)
        {
            StartCoroutine(ExitRoutine());
        }
    }

    private IEnumerator EnterRoutine()
    {
        _isTransitioning = true;
        _isOccupied = true;
        if (_interactable != null) _interactable.isInteractable = false;

        if (hideAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(enterClipName);
            if (clip != null) hideAudioSource.PlayOneShot(clip);
        }

        Transform playerRig = PlayerController.Instance.transform;
        if (_playerCC != null) _playerCC.enabled = false;

        Vector3 startPos = playerRig.position;
        Quaternion startRot = playerRig.rotation;

        Vector3 targetPos = insidePoint != null ? insidePoint.position : startPos;
        Quaternion targetRot = lookTarget != null ? Quaternion.LookRotation(lookTarget.position - targetPos) : startRot;
        targetRot.x = 0; targetRot.z = 0; // 고개 상하 꺾임 방지

        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, timer / transitionDuration);
            playerRig.position = Vector3.Lerp(startPos, targetPos, t);
            playerRig.rotation = Quaternion.Slerp(startRot, targetRot, t);
            yield return null;
        }

        playerRig.position = targetPos;
        playerRig.rotation = targetRot;

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

        if (_playerCC != null) _playerCC.enabled = true;
        if (_interactable != null) _interactable.isInteractable = true;

        _isOccupied = false;
        _isTransitioning = false;
    }
}
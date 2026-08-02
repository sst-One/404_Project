using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PlayerInputProvider))]
[RequireComponent(typeof(PlayerGazeController))]
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public static PlayerMovement Instance { get; private set; }

    [Header("Movement Settings")]
    public float moveSpeed = 1.0f;
    public float footstepInterval = 0.4f;

    public bool IsMoving { get; private set; }

    private PlayerInputProvider inputProvider;
    private PlayerGazeController gazeController;
    private CharacterController cc;

    [Header("오디오 설정 (AudioSources)")]
    public AudioSource playerFootstepSource;

    [Header("사운드 에셋 이름 (SND-xxx)")]
    public string footstepClipName = "SND-006_FootWalk_Oneshot_Loop";

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        inputProvider = GetComponent<PlayerInputProvider>();
        gazeController = GetComponent<PlayerGazeController>();
        cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (IsMoving) return;
        if (FreezeManager.Instance != null && FreezeManager.Instance.IsFreezing) return;

        if (gazeController.IsFloorValid && inputProvider.IsLeanTriggered)
        {
            StartCoroutine(MoveRoutine(gazeController.CurrentFloorHitPoint));
        }
    }

    private IEnumerator MoveRoutine(Vector3 targetPosition)
    {
        IsMoving = true;

        if (cc != null) cc.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, startPos.y, targetPosition.z);

        float distance = Vector3.Distance(startPos, endPos);
        float duration = distance / moveSpeed;
        float elapsedTime = 0f;

        Coroutine footstepCoroutine = null;

        if (duration > 0.01f)
        {
            footstepCoroutine = StartCoroutine(FootstepLoopRoutine(duration));

            while (elapsedTime < duration)
            {
                float t = Mathf.Clamp01(elapsedTime / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(startPos, endPos, smoothT);
                elapsedTime += Time.deltaTime;

                yield return null;
            }
        }

        transform.position = endPos;

        if (cc != null) cc.enabled = true;
        if (footstepCoroutine != null) StopCoroutine(footstepCoroutine);

        IsMoving = false;

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.2f);
    }

    private IEnumerator FootstepLoopRoutine(float maxDuration)
    {
        float timer = 0f;
        while (timer < maxDuration)
        {
            if (playerFootstepSource != null && AudioManager.Instance != null)
            {
                AudioClip clip = AudioManager.Instance.GetClip(footstepClipName);
                if (clip != null) playerFootstepSource.PlayOneShot(clip);
            }
            yield return new WaitForSeconds(footstepInterval);
            timer += footstepInterval;
        }
    }
}
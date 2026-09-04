using System.Collections;
using UnityEngine;

public class Stage1_Controller : MonoBehaviour
{
    [Header("References")]
    public ElevatorDoorController doorController;
    public InteractableItem elevatorButton;

    [Header("Item Audio Reference")]
    public AudioSource elevatorAudioSource;

    [Header("Audio Clip Names")]
    public string buttonClickClipName = "SND-013_ButtonClick_OneShot";
    public string elevatorMoveClipName = "SND-004_ElevatorMove_Loop_Timed";
    public string elevatorArriveClipName = "SND-005_ElevatorArrive_OneShot";

    private void Start()
    {
        if (doorController != null) doorController.SetDoorsOpenImmediately();

        if (elevatorButton != null)
        {
            elevatorButton.onInteractAction -= OnButtonReached;
            elevatorButton.onInteractAction += OnButtonReached;
        }

        StartCoroutine(InitDay1Routine());
    }

    private IEnumerator InitDay1Routine()
    {
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDayTransition(1));
        }

        if (elevatorButton != null) elevatorButton.EnableInteractionWithLight();
    }

    private void OnButtonReached()
    {
        if (elevatorButton != null)
        {
            elevatorButton.isInteractable = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.1f);
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickClipName))
            AudioManager.Instance.PlayGlobal2D(buttonClickClipName, AudioManager.Instance.sfxMixerGroup);

        StartCoroutine(ElevatorTravelSequence());
    }

    private IEnumerator ElevatorTravelSequence()
    {
        if (doorController != null) { doorController.animationDuration = 2.5f; doorController.CloseDoors(); }
        yield return new WaitForSeconds(2.5f);

        if (elevatorAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(elevatorMoveClipName);
            if (clip != null) { elevatorAudioSource.clip = clip; elevatorAudioSource.loop = true; elevatorAudioSource.Play(); }
        }

        yield return new WaitForSeconds(4.0f);
        if (elevatorAudioSource != null && elevatorAudioSource.isPlaying) elevatorAudioSource.Stop();

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(elevatorArriveClipName))
            AudioManager.Instance.PlayGlobal2D(elevatorArriveClipName, AudioManager.Instance.sfxMixerGroup);

        if (doorController != null) { doorController.animationDuration = 2.5f; doorController.OpenDoors(); }
        yield return new WaitForSeconds(2.5f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
    }
}
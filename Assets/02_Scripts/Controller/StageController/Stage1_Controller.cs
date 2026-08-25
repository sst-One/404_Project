using System.Collections;
using UnityEngine;

public class Stage1_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;

    [Header("Item Audio Reference")]
    public AudioSource elevatorAudioSource;

    [Header("Audio Clip Names")]
    public string buttonClickClipName = "SND-013_ButtonClick_OneShot";
    public string elevatorMoveClipName = "SND-004_ElevatorMove_Loop_Timed";
    public string elevatorArriveClipName = "SND-005_ElevatorArrive_OneShot";

    private void Start()
    {
        doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null) doorController.SetDoorsOpenImmediately();

        GameObject btnObj = GameObject.Find("Item_ElevatorButton");
        if (btnObj != null)
        {
            elevatorButton = btnObj.GetComponent<InteractableItem>();
            if (elevatorButton != null)
            {
                elevatorButton.onInteractEvent.RemoveAllListeners();
                elevatorButton.onInteractEvent.AddListener(OnButtonReached);
            }
        }

        StartCoroutine(InitDay1Routine());
    }

    private IEnumerator InitDay1Routine()
    {
        if (UIManager.Instance != null)
        {
            yield return StartCoroutine(UIManager.Instance.ShowDayTransition(1)); // 인자 단순화
        }
    }

    private void OnButtonReached()
    {
        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
            if (elevatorButton.GetComponent<Collider>() != null) elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (StateManager.Instance != null) StateManager.Instance.AddNoise(0.1f);
        if (AudioManager.Instance != null && !string.IsNullOrEmpty(buttonClickClipName)) AudioManager.Instance.PlayGlobal2D(buttonClickClipName, AudioManager.Instance.sfxMixerGroup);

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

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(elevatorArriveClipName)) AudioManager.Instance.PlayGlobal2D(elevatorArriveClipName, AudioManager.Instance.sfxMixerGroup);

        if (doorController != null) { doorController.animationDuration = 2.5f; doorController.OpenDoors(); }
        yield return new WaitForSeconds(2.5f);

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
    }
}
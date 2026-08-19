using System.Collections;
using UnityEngine;

public class Stage1_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;

    [Header("Audio Settings")]
    public AudioSource buttonAudioSource;
    public AudioSource elevatorMoveSource;
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
    }

    private void OnButtonReached()
    {
        if (elevatorButton != null)
        {
            elevatorButton.enabled = false;
            if (elevatorButton.GetComponent<Collider>() != null)
                elevatorButton.GetComponent<Collider>().enabled = false;
        }

        if (StateManager.Instance != null)
        {
            StateManager.Instance.AddNoise(0.1f);
        }

        if (buttonAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(buttonClickClipName);
            if (clip != null) buttonAudioSource.PlayOneShot(clip);
        }

        StartCoroutine(ElevatorTravelSequence());
    }

    private IEnumerator ElevatorTravelSequence()
    {
        if (doorController != null)
        {
            // [수정점] 기획 명세(EVT-005)에 맞춰 문 닫힘 애니메이션 시간 강제 동기화
            doorController.animationDuration = 1.4f;
            doorController.CloseDoors();
        }

        yield return new WaitForSeconds(1.4f);

        if (elevatorMoveSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(elevatorMoveClipName);
            if (clip != null)
            {
                elevatorMoveSource.clip = clip;
                elevatorMoveSource.loop = true;
                elevatorMoveSource.Play();
            }
        }

        yield return new WaitForSeconds(4.0f);

        if (elevatorMoveSource != null && elevatorMoveSource.isPlaying)
        {
            elevatorMoveSource.Stop();
        }

        if (AudioManager.Instance != null)
        {
            AudioClip arriveClip = AudioManager.Instance.GetClip(elevatorArriveClipName);
            if (arriveClip != null && buttonAudioSource != null)
            {
                buttonAudioSource.PlayOneShot(arriveClip);
            }
        }

        if (doorController != null)
        {
            doorController.animationDuration = 1.3f; // EVT-006: Door Open 1.3s
            doorController.OpenDoors();
        }

        yield return new WaitForSeconds(1.3f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
        }
    }
}
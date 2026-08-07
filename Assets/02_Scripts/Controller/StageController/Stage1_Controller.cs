// Stage1_Controller.cs
using System.Collections;
using UnityEngine;

public class Stage1_Controller : MonoBehaviour
{
    private InteractableItem elevatorButton;
    private ElevatorDoorController doorController;
    public AudioSource buttonAudioSource;

    public AudioSource elevatorMoveSource;
    public string elevatorMoveClipName = "SND-004_ElevatorMove_Loop_Timed";

    private void Start()
    {
        doorController = FindObjectOfType<ElevatorDoorController>();
        if (doorController != null) doorController.SetDoorsOpenImmediately();

        GameObject btnObj = GameObject.Find("Item_ElevatorButton");
        if (btnObj != null)
        {
            elevatorButton = btnObj.GetComponent<InteractableItem>();
            elevatorButton.onInteractEvent.RemoveAllListeners();
            elevatorButton.onInteractEvent.AddListener(OnButtonReached);
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

        if (buttonAudioSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip("SND-013_ButtonClick_OneShot");
            if (clip != null) buttonAudioSource.PlayOneShot(clip);
        }

        StartCoroutine(ElevatorTravelSequence());
    }

    private IEnumerator ElevatorTravelSequence()
    {
        if (doorController != null) doorController.CloseDoors();
        yield return new WaitForSeconds(1.5f);

        // 핫픽스: 문이 닫힌 후 이동음 재생
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

        Debug.Log("[Stage1_Controller] 4층으로 이동 중 (4초 대기)...");
        yield return new WaitForSeconds(4.0f);

        // 핫픽스: 문이 열리기 직전 이동음 정지
        if (elevatorMoveSource != null && elevatorMoveSource.isPlaying) elevatorMoveSource.Stop();

        Debug.Log("[Stage1_Controller] 4층 도착. 문 열림.");
        if (doorController != null) doorController.OpenDoors();
        yield return new WaitForSeconds(1.5f);

        GameFlowManager.Instance.AdvanceToStage(GameStage.Stage2_Room);
    }
}
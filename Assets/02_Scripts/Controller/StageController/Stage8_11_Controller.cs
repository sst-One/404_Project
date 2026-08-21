using System.Collections;
using UnityEngine;

public class Stage8_11_Controller : MonoBehaviour
{
    [Header("Stage 8: Encounter References")]
    public Animator intruderAnimator;
    public AudioSource knifeStrikeAudio;
    public GameObject playerPhone;
    public Transform phoneDropTarget;

    [Header("Stage 8: Encounter Settings")]
    public float strikeDelay = 0.5f;
    public float dropDuration = 0.8f;

    [Header("Stage 11: Police Arrival References")]
    public AudioSource policeSirenSource;
    public AudioSource doorKnockSource;

    [Header("Audio Clip Names")]
    public string knifeStrikeClipName = "SND-047_KnifeStrike_OneShot";
    public string policeSirenClipName = "SND-055_PoliceSiren_Loop";
    public string doorKnockClipName = "SND-057_DoorKnock_OneShot";

    private PhoneController phoneController;
    private bool hasEncounterTriggered = false;

    private void Start()
    {
        EnemyAI enemy = FindObjectOfType<EnemyAI>(true);
        if (enemy != null)
        {
            bool isChaseStage = (GameFlowManager.Instance.currentStage >= GameStage.Stage8_Intruder &&
                                 GameFlowManager.Instance.currentStage <= GameStage.Stage11_Call);
            enemy.gameObject.SetActive(isChaseStage);
            enemy.isNarrativeMode = false;
        }

        GameObject phoneObj = GameObject.Find("Item_Phone");
        if (phoneObj != null)
        {
            phoneController = phoneObj.GetComponent<PhoneController>();
            if (phoneController != null)
            {
                phoneController.onCallSuccess.RemoveAllListeners();
                phoneController.onCallSuccess.AddListener(OnStage11Completed);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasEncounterTriggered && other.CompareTag("Player"))
        {
            hasEncounterTriggered = true;
            StartCoroutine(EncounterSequence());
        }
    }

    private IEnumerator EncounterSequence()
    {
        if (intruderAnimator != null) intruderAnimator.SetTrigger("Strike");

        yield return new WaitForSeconds(strikeDelay);

        if (knifeStrikeAudio != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(knifeStrikeClipName);
            if (clip != null) knifeStrikeAudio.PlayOneShot(clip);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        if (playerPhone != null && phoneDropTarget != null)
        {
            playerPhone.transform.SetParent(null);
            Vector3 startPos = playerPhone.transform.position;
            Quaternion startRot = playerPhone.transform.rotation;

            float timer = 0f;
            while (timer < dropDuration)
            {
                timer += Time.deltaTime;
                float t = timer / dropDuration;
                playerPhone.transform.position = Vector3.Lerp(startPos, phoneDropTarget.position, t);
                playerPhone.transform.rotation = Quaternion.Slerp(startRot, phoneDropTarget.rotation, t);
                yield return null;
            }
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage9_Hide);
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
        foreach (GameObject enemyObj in enemies)
        {
            enemyObj.SetActive(false);
        }

        StartCoroutine(PoliceArrivalSequence());
    }

    private IEnumerator PoliceArrivalSequence()
    {
        if (StateManager.Instance != null) StateManager.Instance.ResetHeartbeat();

        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));
        else yield return new WaitForSeconds(2.0f);

        if (policeSirenSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(policeSirenClipName);
            if (clip != null)
            {
                policeSirenSource.clip = clip;
                policeSirenSource.Play();
            }
        }

        yield return new WaitForSeconds(4.0f);

        if (doorKnockSource != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(doorKnockClipName);
            if (clip != null)
            {
                doorKnockSource.clip = clip;
                doorKnockSource.Play();
            }
        }

        yield return new WaitForSeconds(4.0f);

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage12_Police);
        }
    }
}
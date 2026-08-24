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

                // [핵심 추가] 씬 시작 시 플레이어의 카메라를 찾아 휴대폰을 왼손 위치에 쥐어줍니다.
                if (PlayerController.Instance != null && PlayerController.Instance.mainCamera != null)
                {
                    phoneController.transform.SetParent(PlayerController.Instance.mainCamera.transform);
                    phoneController.transform.localPosition = phoneController.leftHandPosition;
                    phoneController.transform.localRotation = Quaternion.Euler(phoneController.leftHandRotation);
                }
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

        // 칼을 높이 드는 애니메이션 대기
        yield return new WaitForSeconds(strikeDelay);

        // 바닥을 찍는 사운드 재생
        if (knifeStrikeAudio != null && AudioManager.Instance != null)
        {
            AudioClip clip = AudioManager.Instance.GetClip(knifeStrikeClipName);
            if (clip != null) knifeStrikeAudio.PlayOneShot(clip);
        }

        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(3);

        // [핵심 수정] 수동 Lerp 연산을 제거하고 PhoneController 전용 함수를 통해 시각/청각 완벽 동기화
        if (phoneController != null && phoneDropTarget != null)
        {
            phoneController.transform.SetParent(null); // 플레이어 종속 해제
            phoneController.TriggerDrop(phoneDropTarget);

            // 휴대폰이 바닥에 미끄러지며 떨어지는 연출 시간만큼 씬 전환 대기
            yield return new WaitForSeconds(dropDuration);
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
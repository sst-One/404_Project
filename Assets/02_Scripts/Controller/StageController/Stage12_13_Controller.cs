using System.Collections;
using UnityEngine;

public class Stage12_13_Controller : MonoBehaviour
{
    [Header("Interactable Objects")]
    public InteractableItem crackedPhone;
    public InteractableItem endingGem;

    [Header("Audio Triggers (추후 AudioManager 연동)")]
    public AudioSource phoneRingSource;
    public AudioSource tvNewsSource;
    public AudioSource tinnitusSource;
    public AudioSource gemDropSource;

    private void Start()
    {
        // 초기화: 보석은 숨기고 전화기 상호작용 이벤트 바인딩
        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(false);
        }

        if (crackedPhone != null)
        {
            crackedPhone.onInteractEvent.RemoveAllListeners();
            crackedPhone.onInteractEvent.AddListener(OnPhoneAnswered);
        }

        // POL-017: 초기 연출 시작
        StartCoroutine(Day5MorningSequence());
    }

    private IEnumerator Day5MorningSequence()
    {
        Debug.Log("[Stage12_13] Day 5 아침 시작. 3초 후 전화 수신.");
        yield return new WaitForSeconds(3.0f);

        if (phoneRingSource != null) phoneRingSource.Play();
        Debug.Log("[Stage12_13] 전화벨 울림. 휴대폰 Reach 입력 대기 중...");
    }

    private void OnPhoneAnswered()
    {
        // 입력 잠금 처리 (POL-017)
        if (crackedPhone != null)
        {
            crackedPhone.enabled = false;
            if (crackedPhone.GetComponent<Collider>() != null)
                crackedPhone.GetComponent<Collider>().enabled = false;
        }

        if (phoneRingSource != null) phoneRingSource.Stop();
        StartCoroutine(CallAndNewsSequence());
    }

    private IEnumerator CallAndNewsSequence()
    {
        // 1. 경찰 전화 음성 (EVT-054)
        Debug.Log("[Stage12_13] 경찰 통화: 용의자 석방 안내 (약 8초 재생).");
        yield return new WaitForSeconds(8.0f);

        // 2. TV 뉴스 재생 및 이명 발생 (EVT-055, EVT-056)
        Debug.Log("[Stage12_13] TV 뉴스 재생 시작.");
        if (tvNewsSource != null) tvNewsSource.Play();

        yield return new WaitForSeconds(4.0f);

        Debug.Log("[Stage12_13] 이명 발생 및 Heartbeat 강제 상승.");
        if (tinnitusSource != null) tinnitusSource.Play();
        if (StateManager.Instance != null) StateManager.Instance.AddHeartbeat(2);

        // 3. 텐션 누적 대기
        yield return new WaitForSeconds(6.0f);

        // 4. 보석 낙하 연출 (EVT-057)
        Debug.Log("[Stage12_13] 땡그랑. 보석 낙하.");
        if (gemDropSource != null) gemDropSource.Play();

        // 보석 상호작용 개방 (EVT-058 대기)
        if (endingGem != null)
        {
            endingGem.gameObject.SetActive(true);
            endingGem.onInteractEvent.RemoveAllListeners();
            endingGem.onInteractEvent.AddListener(OnGemReached);
        }
    }

    private void OnGemReached()
    {
        // 입력 잠금 처리 (POL-017)
        if (endingGem != null)
        {
            endingGem.enabled = false;
            if (endingGem.GetComponent<Collider>() != null)
                endingGem.GetComponent<Collider>().enabled = false;
        }

        StartCoroutine(FlashbackAndEndingSequence());
    }

    private IEnumerator FlashbackAndEndingSequence()
    {
        // EVT-058 ~ EVT-062: 플래시백 시퀀스
        Debug.Log("[Stage12_13] 보석-반지 일치 확인. Heartbeat Overload 및 플래시백 시작.");

        if (StateManager.Instance != null)
        {
            // Heartbeat 시스템의 최고 단계(Overload)로 강제 고정
            StateManager.Instance.AddHeartbeat(3);
        }

        // 플래시백 연출 시간 대기 (추후 Timeline 연동 시 Timeline 길이로 동기화)
        yield return new WaitForSeconds(10.0f);

        // EVT-063: 엔딩 씬 전환
        Debug.Log("[Stage12_13] 연출 종료. 09_Ending 씬 로드 요청.");
        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage13_Ending);
        }
    }
}
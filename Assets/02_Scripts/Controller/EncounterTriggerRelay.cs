using UnityEngine;

public class EncounterTriggerRelay : MonoBehaviour
{
    [Header("연결할 메인 컨트롤러")]
    public Stage8_11_Controller mainController;

    private void OnTriggerEnter(Collider other)
    {
        // [디버그] 무엇이든 이 투명 박스에 닿으면 콘솔창에 이름을 띄웁니다.
        Debug.LogWarning($"[트리거 감지됨] 닿은 오브젝트: {other.gameObject.name} / 현재 태그: {other.tag}");

        if (other.CompareTag("Player") && mainController != null)
        {
            Debug.LogWarning("[트리거 성공] 플레이어 태그 확인 완료! 이벤트를 발동시킵니다.");
            mainController.StartEncounterEvent();
            gameObject.SetActive(false);
        }
        else if (mainController == null)
        {
            Debug.LogError("[트리거 에러] 닿긴 했는데 mainController 슬롯이 비어있습니다!");
        }
    }
}
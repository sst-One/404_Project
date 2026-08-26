using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    private enum TutorialStep
    {
        Intro, Calibration, ESC, Rotation, Movement, Freeze, Interaction, HidingEnter, HidingExit, Complete
    }

    private TutorialStep currentStep = TutorialStep.Intro;

    [Header("Tutorial Targets")]
    public Transform leanTargetPoint;
    public InteractableItem testObject;
    public HidingSpotAction testHidingSpot;

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0f, 0f, 0.5f); // 붉은색 발광 유지
    public float highlightPulseSpeed = 2.0f;

    private Coroutine _highlightCoroutine;
    public bool IsCameraLocked { get; private set; } = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        IsCameraLocked = true;

        if (PlayerController.Instance != null)
        {
            PlayerController.Instance.SetMovementLock(true);
        }

        if (testObject != null)
        {
            testObject.isInteractable = false;
            testObject.interactOnlyOnce = false;
        }

        if (testHidingSpot != null)
        {
            InteractableItem hideInteractable = testHidingSpot.GetComponent<InteractableItem>();
            if (hideInteractable != null) hideInteractable.isInteractable = false;
        }

        // 이동 지점의 렌더러와 콜라이더를 명시적으로 숨김 처리
        SetTargetVisibility(leanTargetPoint, false);

        StartCoroutine(TutorialSequence());
    }

    // 오브젝트 비활성화 버그를 막기 위한 컴포넌트 개별 제어 함수
    private void SetTargetVisibility(Transform target, bool isVisible)
    {
        if (target == null) return;

        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = isVisible;
        }

        Collider col = target.GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;
    }

    private IEnumerator TutorialSequence()
    {
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.0f));

        // ==============================================
        // 1. 튜토리얼 멘트 즉시 출력 및 캘리브레이션 동시 시작
        // ==============================================
        currentStep = TutorialStep.Intro;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("안전 수칙 교육을 시작합니다. 지시에 따라 행동하십시오.");

        // 멘트가 화면에 렌더링될 수 있도록 정확히 1프레임 대기
        yield return null;

        currentStep = TutorialStep.Calibration;
        if (VisionTrackingManager.Instance != null && !VisionTrackingManager.Instance.IsCalibrated)
        {
            VisionTrackingManager.Instance.StartCalibration();
            yield return new WaitUntil(() => VisionTrackingManager.Instance.IsCalibrated);
        }
        else
        {
            // 이미 캘리브레이션이 완료된 상태라도 멘트 유지 시간을 보장
            yield return new WaitForSeconds(3.0f);
        }

        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();
        yield return new WaitForSeconds(0.5f);

        // ==============================================
        // 2. ESC 안내
        // ==============================================
        currentStep = TutorialStep.ESC;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("[ESC] 키를 눌러 시스템 메뉴를 열고, 감도를 조절한 뒤 다시 닫으세요.");

        bool hasOpenedMenu = false;
        while (currentStep == TutorialStep.ESC)
        {
            if (UIManager.Instance != null && UIManager.Instance.systemMenuPanel != null)
            {
                if (UIManager.Instance.systemMenuPanel.activeInHierarchy) hasOpenedMenu = true;
                else if (hasOpenedMenu && !UIManager.Instance.systemMenuPanel.activeInHierarchy) currentStep = TutorialStep.Rotation;
            }
            yield return null;
        }

        IsCameraLocked = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("마우스를 움직여 주변 환경을 자유롭게 둘러보세요.");
        yield return new WaitForSeconds(3.0f);
        currentStep = TutorialStep.Movement;

        // ==============================================
        // 3. 이동 튜토리얼
        // ==============================================
        if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLock(false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("전방에 빛나는 지점을 바라보고 몸을 기울여(W) 이동하세요.");

        // MovePoint 물리 및 렌더러 활성화 후 발광 시작
        SetTargetVisibility(leanTargetPoint, true);
        if (leanTargetPoint != null)
        {
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(leanTargetPoint));
        }

        while (currentStep == TutorialStep.Movement)
        {
            if (leanTargetPoint != null && PlayerController.Instance != null)
            {
                float dist = Vector3.Distance(PlayerController.Instance.transform.position, leanTargetPoint.position);
                if (dist < 1.5f) currentStep = TutorialStep.Freeze;
            }
            yield return null;
        }
        StopHighlight();

        // 이동 완료 직후 MovePoint 물리 및 렌더러 영구 비활성화
        SetTargetVisibility(leanTargetPoint, false);

        currentStep = TutorialStep.Freeze;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("본래 자세로 돌아와 움직임을 멈추고 숨을 참으세요(Origin Freeze).");

        while (currentStep == TutorialStep.Freeze)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive)
            {
                currentStep = TutorialStep.Interaction;
            }
            yield return null;
        }

        // ==============================================
        // 4. 상호작용
        // ==============================================
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("빛나는 사물을 가만히 응시한 뒤, 손을 뻗어 조작하세요.");

        bool interactionDone = false;
        UnityAction interactCallback = () => { interactionDone = true; };

        if (testObject != null)
        {
            testObject.onInteractEvent.RemoveListener(interactCallback);
            testObject.onInteractEvent.AddListener(interactCallback);
            testObject.isInteractable = true;
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(testObject.transform));
        }

        while (!interactionDone)
        {
            yield return null;
        }
        StopHighlight();
        currentStep = TutorialStep.HidingEnter;

        if (testObject != null) testObject.onInteractEvent.RemoveListener(interactCallback);

        // ==============================================
        // 5. 숨기 (세탁기)
        // ==============================================
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 다가옵니다! 근처 옷장(세탁기) 안으로 피신하세요.");

        bool hidingEntered = false;
        UnityAction hidingEnterCallback = () => { hidingEntered = true; };
        InteractableItem hideItem = null;

        if (testHidingSpot != null)
        {
            hideItem = testHidingSpot.GetComponent<InteractableItem>();
            if (hideItem != null)
            {
                hideItem.onInteractEvent.RemoveListener(hidingEnterCallback);
                hideItem.onInteractEvent.AddListener(hidingEnterCallback);
                hideItem.isInteractable = true;
            }
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(testHidingSpot.transform));
        }

        while (!hidingEntered)
        {
            yield return null;
        }
        StopHighlight();
        currentStep = TutorialStep.HidingExit;

        if (hideItem != null) hideItem.onInteractEvent.RemoveListener(hidingEnterCallback);

        // ==============================================
        // 6. 나오기
        // ==============================================
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 사라졌습니다. 다시 밖을 응시하고 손을 뻗어 밖으로 나오세요.");

        bool hidingExited = false;
        UnityAction hidingExitCallback = () => { hidingExited = true; };

        if (hideItem != null)
        {
            yield return new WaitForSeconds(1.5f);
            hideItem.onInteractEvent.AddListener(hidingExitCallback);
        }
        else
        {
            hidingExited = true;
        }

        while (!hidingExited)
        {
            yield return null;
        }
        if (hideItem != null) hideItem.onInteractEvent.RemoveListener(hidingExitCallback);

        currentStep = TutorialStep.Complete;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("모든 안전 수칙을 익히셨습니다. 본 게임을 시작합니다.");

        yield return new WaitForSeconds(3.0f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();
        if (UIManager.Instance != null) yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));

        if (GameFlowManager.Instance != null) GameFlowManager.Instance.AdvanceToStage(GameStage.Stage1_Elevator);
    }

    private IEnumerator PulseHighlightRoutine(Transform targetTransform)
    {
        if (targetTransform == null) yield break;

        Renderer[] renderers = targetTransform.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) yield break;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        int emissionColorID = Shader.PropertyToID("_EmissionColor");

        foreach (Renderer r in renderers)
        {
            if (r != null && r.sharedMaterial != null && !r.sharedMaterial.IsKeywordEnabled("_EMISSION"))
            {
                r.sharedMaterial.EnableKeyword("_EMISSION");
            }
        }

        while (true)
        {
            float lerp = Mathf.PingPong(Time.time * highlightPulseSpeed, 1f);
            Color finalColor = Color.Lerp(Color.black, highlightColor, lerp);

            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.GetPropertyBlock(propBlock);
                    propBlock.SetColor(emissionColorID, finalColor);
                    r.SetPropertyBlock(propBlock);
                }
            }
            yield return null;
        }
    }

    private void StopHighlight()
    {
        if (_highlightCoroutine != null)
        {
            StopCoroutine(_highlightCoroutine);
            _highlightCoroutine = null;
        }

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        int emissionColorID = Shader.PropertyToID("_EmissionColor");

        Renderer[] allRenderers = FindObjectsOfType<Renderer>();
        foreach (Renderer r in allRenderers)
        {
            if (r != null)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(emissionColorID, Color.black);
                r.SetPropertyBlock(propBlock);
            }
        }
    }
}
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    private enum TutorialStep
    {
        Calibration, Intro, ESC, Rotation, Movement, Freeze, Interaction, HidingEnter, HidingGuide, HidingExit, Complete
    }

    private TutorialStep currentStep = TutorialStep.Calibration;

    [Header("Tutorial Targets")]
    public Transform leanTargetPoint;
    public InteractableItem testObject;
    public HidingSpotAction testHidingSpot;

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0f, 0f, 0.5f);
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

        SetTargetVisibility(leanTargetPoint, false);

        StartCoroutine(TutorialSequence());
    }

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

        currentStep = TutorialStep.Intro;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("튜토리얼을 시작합니다. 지시에 따라 행동하십시오.");

        yield return null;

        currentStep = TutorialStep.Calibration;
        if (VisionTrackingManager.Instance != null && !VisionTrackingManager.Instance.IsCalibrated)
        {
            VisionTrackingManager.Instance.StartCalibration();
            yield return new WaitUntil(() => VisionTrackingManager.Instance.IsCalibrated);
        }
        else
        {
            yield return new WaitForSeconds(3.0f);
        }

        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();
        yield return new WaitForSeconds(0.5f);

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
            UIManager.Instance.ShowSubtitle("고개를 움직여 주변 환경을 자유롭게 둘러보세요.");
        yield return new WaitForSeconds(3.0f);
        currentStep = TutorialStep.Movement;

        if (PlayerController.Instance != null) PlayerController.Instance.SetMovementLock(false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("전방에 빛나는 지점을 바라보고 몸을 기울여 이동하세요.");

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

        SetTargetVisibility(leanTargetPoint, false);

        currentStep = TutorialStep.Freeze;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("본래 자세로 돌아와 움직임을 멈추고 상체를 뒤로 젖혀 숨을 참으세요.");

        while (currentStep == TutorialStep.Freeze)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive)
            {
                currentStep = TutorialStep.Interaction;
            }
            yield return null;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("빛나는 사물을 가만히 응시한 뒤, 손을 뻗어 상호작용하세요.");

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

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 다가옵니다! 발코니에 있는 세탁기 안으로 피신하세요.");

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

        // 핫픽스: 대사가 나오는 동안 다시 나가는 것을 방지하기 위해 상호작용 잠금
        if (hideItem != null) hideItem.isInteractable = false;

        currentStep = TutorialStep.HidingGuide;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("이곳(세탁기, 옷장, 침대 밑, 서재 책상 아래 등)과 같은 장소에 숨을 수 있습니다.");

        yield return new WaitForSeconds(3.0f);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 사라졌습니다. 다시 밖을 응시하고 손을 뻗어 밖으로 나오세요.");

        currentStep = TutorialStep.HidingExit;
        bool hidingExited = false;
        UnityAction hidingExitCallback = () => { hidingExited = true; };

        if (hideItem != null)
        {
            // 핫픽스: 안내 대사가 끝났으므로 다시 상호작용을 활성화하여 나갈 수 있도록 허용
            hideItem.isInteractable = true;
            yield return new WaitForSeconds(1.0f);
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
            UIManager.Instance.ShowSubtitle("튜토리얼이 종료되었습니다. 건투를 빕니다.");

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
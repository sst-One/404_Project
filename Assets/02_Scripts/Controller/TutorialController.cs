using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    private enum TutorialStep
    {
        Intro, CameraSetup, Calibration, Rotation, Movement, Sensitivity, Freeze, Interaction, HidingEnter, HidingGuide, HidingExit, Complete
    }

    private TutorialStep currentStep = TutorialStep.Intro;

    [Header("Tutorial Targets")]
    public Transform leanTargetPoint;
    public InteractableItem testObject;
    public HidingSpotAction testHidingSpot;

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0f, 0f, 0.5f);
    public float highlightPulseSpeed = 2.0f;

    private Coroutine _highlightCoroutine;
    public bool IsCameraLocked { get; private set; } = true;
    private Renderer[] _activeHighlightRenderers;
    private GameObject _calibrationGuideRoot;
    private CanvasGroup _calibrationGuideCanvasGroup;

    private void Awake() { Instance = this; }

    private void Start()
    {
        IsCameraLocked = true;
        highlightColor = Color.white;

        if (PlayerController.Instance != null)
            PlayerController.Instance.SetMovementLock(true);

        if (testObject != null)
        {
            testObject.isInteractable = false;
            testObject.interactOnlyOnce = false;
        }

        if (testHidingSpot != null) testHidingSpot.isInteractable = false;

        SetTargetVisibility(leanTargetPoint, false);
        StartCoroutine(TutorialSequence());
    }

    private void SetTargetVisibility(Transform target, bool isVisible)
    {
        if (target == null) return;
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = isVisible;
        Collider col = target.GetComponent<Collider>();
        if (col != null) col.enabled = isVisible;
    }

    private IEnumerator TutorialSequence()
    {
        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.0f));

        currentStep = TutorialStep.Intro;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("404에 오신 것을 환영합니다. 화면의 안내에 따라 조작을 익혀보세요.");
        yield return new WaitForSeconds(2.5f);

        // 첫 번째 ESC는 감도 설정이 아니라 사용할 카메라를 선택하기 위한 단계입니다.
        currentStep = TutorialStep.CameraSetup;
        if (VisionTrackingManager.Instance != null)
            VisionTrackingManager.Instance.allowCameraActivation = true;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("[ESC]를 눌러 사용할 카메라를 선택한 뒤 메뉴를 닫아주세요.");
        yield return StartCoroutine(WaitForSystemMenuOpenAndClose());

        // 카메라가 준비된 뒤 영점을 맞춥니다.
        yield return new WaitUntil(() =>
            VisionTrackingManager.Instance != null &&
            VisionTrackingManager.Instance.IsCameraReady);

        currentStep = TutorialStep.Calibration;
        ShowCalibrationGuide("정면을 바라보세요\n영점 조절 중");
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("화면 중앙을 바라보고 바른 자세를 유지하세요. 약 3초 동안 영점을 맞춥니다.");

        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.StartCalibration();
            yield return new WaitUntil(() =>
                VisionTrackingManager.Instance == null ||
                VisionTrackingManager.Instance.IsCalibrated);
        }

        HideCalibrationGuide();
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();
        yield return new WaitForSeconds(0.4f);

        // 먼저 시야 회전을 익힙니다.
        currentStep = TutorialStep.Rotation;
        IsCameraLocked = false;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("고개를 천천히 위·아래·좌·우로 움직여 주변을 둘러보세요.");
        yield return new WaitForSeconds(3.5f);

        // 이어서 실제 이동을 직접 수행하게 합니다.
        currentStep = TutorialStep.Movement;
        if (PlayerController.Instance != null)
            PlayerController.Instance.SetMovementLock(false);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("상체를 앞·뒤로 기울여 이동하고, 머리를 좌·우로 기울여 옆으로 이동해 목표 지점까지 가보세요.");

        SetTargetVisibility(leanTargetPoint, true);
        if (leanTargetPoint != null)
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(leanTargetPoint));

        float movementElapsed = 0f;
        bool reachedMovementTarget = leanTargetPoint == null;
        while (!reachedMovementTarget)
        {
            movementElapsed += Time.deltaTime;

            if (leanTargetPoint != null && PlayerController.Instance != null)
            {
                float dist = Vector3.Distance(
                    PlayerController.Instance.transform.position,
                    leanTargetPoint.position
                );
                reachedMovementTarget = dist < 1.5f;
            }

            // 씬에 목표 오브젝트가 연결되지 않은 경우 튜토리얼이 영원히 멈추지 않도록 합니다.
            if (leanTargetPoint == null || movementElapsed >= 45f)
                reachedMovementTarget = true;

            yield return null;
        }

        StopHighlight();
        SetTargetVisibility(leanTargetPoint, false);

        // 감도 조절은 플레이어가 기본 이동을 경험한 다음 안내합니다.
        currentStep = TutorialStep.Sensitivity;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("[ESC]를 눌러 '조작 설정'에서 상하·좌우 회전 감도를 조절해보세요. 완료하면 메뉴를 닫으세요.");
        yield return StartCoroutine(WaitForSystemMenuOpenAndClose());

        currentStep = TutorialStep.Freeze;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("숨을 참아야 할 때는 손으로 입을 가리세요. 손을 내리면 숨 참기가 해제됩니다. 먼저 3초간 유지해보세요.");

        float freezeTimer = 0f;
        while (currentStep == TutorialStep.Freeze)
        {
            if (PlayerController.Instance != null && PlayerController.Instance.IsFreezeActive)
            {
                freezeTimer += Time.deltaTime;
                if (freezeTimer >= 3.0f)
                    currentStep = TutorialStep.Interaction;
            }
            else
            {
                freezeTimer = 0f;
            }

            yield return null;
        }

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("흰색으로 빛나는 사물을 바라본 뒤, 손을 뻗어 상호작용하세요.");

        bool interactionDone = false;
        System.Action interactCallback = () => { interactionDone = true; };

        if (testObject != null)
        {
            testObject.onInteractAction -= interactCallback;
            testObject.onInteractAction += interactCallback;
            testObject.EnableInteractionWithLight();
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(testObject.transform));
        }

        while (!interactionDone) yield return null;
        StopHighlight();

        if (testObject != null) testObject.onInteractAction -= interactCallback;
        currentStep = TutorialStep.HidingEnter;

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 다가옵니다! 가까운 은신처를 바라보고 손을 뻗어 숨으세요.");

        bool hidingEntered = false;
        System.Action hidingEnterCallback = () => { hidingEntered = true; };

        if (testHidingSpot != null)
        {
            testHidingSpot.onInteractAction -= hidingEnterCallback;
            testHidingSpot.onInteractAction += hidingEnterCallback;
            testHidingSpot.EnableInteractionWithLight();
            _highlightCoroutine = StartCoroutine(PulseHighlightRoutine(testHidingSpot.transform));
        }

        while (!hidingEntered) yield return null;
        StopHighlight();

        if (testHidingSpot != null) testHidingSpot.isInteractable = false;

        currentStep = TutorialStep.HidingGuide;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("세탁기, 옷장, 침대 밑처럼 몸을 숨길 수 있는 장소를 기억하세요.");
        yield return new WaitForSeconds(3.0f);

        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("위협이 사라졌습니다. 밖을 바라보고 손을 뻗어 은신처에서 나오세요.");

        currentStep = TutorialStep.HidingExit;
        bool hidingExited = false;
        System.Action hidingExitCallback = () => { hidingExited = true; };

        if (testHidingSpot != null)
        {
            testHidingSpot.isInteractable = true;
            testHidingSpot.onInteractAction += hidingExitCallback;
        }
        else hidingExited = true;

        while (!hidingExited) yield return null;
        if (testHidingSpot != null) testHidingSpot.onInteractAction -= hidingExitCallback;

        currentStep = TutorialStep.Complete;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("기본 조작을 익혔습니다. 이제 이야기를 시작합니다.");

        yield return new WaitForSeconds(2.5f);
        if (UIManager.Instance != null) UIManager.Instance.HideSubtitle();

        if (TitleController.Instance != null)
        {
            TitleController.Instance.PlayIntroThenStartStage(GameStage.Stage1_Elevator);
        }
        else
        {
            if (UIManager.Instance != null)
                yield return StartCoroutine(UIManager.Instance.FadeOutScreen(2.0f));
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.AdvanceToStage(GameStage.Stage1_Elevator);
        }
    }

    private IEnumerator WaitForSystemMenuOpenAndClose()
    {
        if (UIManager.Instance == null || UIManager.Instance.systemMenuPanel == null)
            yield break;

        bool hasOpenedMenu = false;
        while (true)
        {
            bool isMenuOpen = UIManager.Instance.systemMenuPanel.activeInHierarchy;
            if (isMenuOpen)
                hasOpenedMenu = true;
            else if (hasOpenedMenu)
                yield break;

            yield return null;
        }
    }

    private void ShowCalibrationGuide(string message)
    {
        if (_calibrationGuideRoot == null)
        {
            _calibrationGuideRoot = new GameObject("Tutorial_CalibrationGuide");
            Canvas canvas = _calibrationGuideRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            CanvasScaler guideScaler = _calibrationGuideRoot.AddComponent<CanvasScaler>();
            guideScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            guideScaler.referenceResolution = new Vector2(1920f, 1080f);
            guideScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            guideScaler.matchWidthOrHeight = 0.5f;
            _calibrationGuideRoot.AddComponent<GraphicRaycaster>();
            _calibrationGuideCanvasGroup = _calibrationGuideRoot.AddComponent<CanvasGroup>();
            _calibrationGuideCanvasGroup.blocksRaycasts = false;
            _calibrationGuideCanvasGroup.interactable = false;

            CreateGuideBar("Guide_Top", new Vector2(0f, 36f), new Vector2(4f, 30f));
            CreateGuideBar("Guide_Bottom", new Vector2(0f, -36f), new Vector2(4f, 30f));
            CreateGuideBar("Guide_Left", new Vector2(-36f, 0f), new Vector2(30f, 4f));
            CreateGuideBar("Guide_Right", new Vector2(36f, 0f), new Vector2(30f, 4f));
            CreateGuideBar("Guide_CenterDot", Vector2.zero, new Vector2(8f, 8f));

            GameObject textObject = new GameObject(
                "Guide_Message",
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );
            textObject.transform.SetParent(_calibrationGuideRoot.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = new Vector2(0f, 110f);
            textRect.sizeDelta = new Vector2(700f, 90f);

            TextMeshProUGUI guideText = textObject.GetComponent<TextMeshProUGUI>();
            guideText.name = "Guide_Message_Text";
            if (UIManager.Instance != null &&
                UIManager.Instance.subtitleText != null &&
                UIManager.Instance.subtitleText.font != null)
                guideText.font = UIManager.Instance.subtitleText.font;
            else if (TMP_Settings.defaultFontAsset != null)
                guideText.font = TMP_Settings.defaultFontAsset;
            guideText.fontSize = 30f;
            guideText.alignment = TextAlignmentOptions.Center;
            guideText.color = Color.white;
            guideText.enableWordWrapping = true;
            guideText.text = message;
        }
        else
        {
            TextMeshProUGUI guideText = _calibrationGuideRoot.GetComponentInChildren<TextMeshProUGUI>(true);
            if (guideText != null) guideText.text = message;
        }

        _calibrationGuideRoot.SetActive(true);
        if (_calibrationGuideCanvasGroup != null) _calibrationGuideCanvasGroup.alpha = 1f;
    }

    private void CreateGuideBar(string objectName, Vector2 position, Vector2 size)
    {
        GameObject bar = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(_calibrationGuideRoot.transform, false);
        RectTransform rect = bar.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = bar.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.92f);
        image.raycastTarget = false;
    }

    private void HideCalibrationGuide()
    {
        if (_calibrationGuideRoot != null)
            _calibrationGuideRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        HideCalibrationGuide();
    }

    private IEnumerator PulseHighlightRoutine(Transform targetTransform)
    {
        if (targetTransform == null) yield break;
        _activeHighlightRenderers = targetTransform.GetComponentsInChildren<Renderer>();
        if (_activeHighlightRenderers.Length == 0) yield break;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        int emissionColorID = Shader.PropertyToID("_EmissionColor");

        foreach (Renderer r in _activeHighlightRenderers)
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

            foreach (Renderer r in _activeHighlightRenderers)
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

        if (_activeHighlightRenderers == null) return;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        int emissionColorID = Shader.PropertyToID("_EmissionColor");

        foreach (Renderer r in _activeHighlightRenderers)
        {
            if (r != null)
            {
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(emissionColorID, Color.black);
                r.SetPropertyBlock(propBlock);
            }
        }
        _activeHighlightRenderers = null;
    }
}
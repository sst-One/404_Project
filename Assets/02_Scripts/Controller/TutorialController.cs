using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialController : MonoBehaviour
{
    public static TutorialController Instance { get; private set; }

    private enum TutorialStep
    {
        Intro, CameraSetup, Rotation, Movement, Sensitivity, Interaction, HidingEnter, HidingGuide, HidingExit, Complete
    }

    private TutorialStep currentStep = TutorialStep.Intro;

    [Header("Tutorial Targets")]
    public Transform leanTargetPoint;
    public InteractableItem testObject;
    public HidingSpotAction testHidingSpot;
    [SerializeField] private TutorialFloorRoute hidingFloorRoute;

    [Header("Highlight Settings")]
    public Color highlightColor = new Color(1f, 0f, 0f, 0.5f);
    public float highlightPulseSpeed = 2.0f;

    private Coroutine _highlightCoroutine;
    private PlayerController _tutorialPlayer;

    private PlayerController GetTutorialPlayer()
    {
        if (_tutorialPlayer != null) return _tutorialPlayer;
        if (PlayerController.Instance != null) _tutorialPlayer = PlayerController.Instance;
        else _tutorialPlayer = FindObjectOfType<PlayerController>(true);
        return _tutorialPlayer;
    }
    public bool IsCameraLocked { get; private set; } = true;
    private Renderer[] _activeHighlightRenderers;

    private void Awake() { Instance = this; }

    private void OnDisable()
    {
        if (hidingFloorRoute != null) hidingFloorRoute.HideRoute();
    }

    private void Start()
    {
        IsCameraLocked = true;
        _tutorialPlayer = GetTutorialPlayer();
        if (hidingFloorRoute == null)
            hidingFloorRoute = FindObjectOfType<TutorialFloorRoute>(true);
        highlightColor = new Color(0.36f, 0.36f, 0.36f);

        if (_tutorialPlayer != null)
            _tutorialPlayer.SetMovementLock(true);

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
        // Player는 이전 씬의 싱글톤이 아니라 튜토리얼 씬에서 생성될 수 있으므로 준비될 때까지 탐색합니다.
        yield return new WaitUntil(() => GetTutorialPlayer() != null);
        _tutorialPlayer = GetTutorialPlayer();
        _tutorialPlayer.SetMovementLock(true);

        if (UIManager.Instance != null)
            yield return StartCoroutine(UIManager.Instance.FadeInScreen(1.0f));

        currentStep = TutorialStep.Intro;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle("404에 오신 것을 환영합니다. 화면의 안내에 따라 조작을 익혀보세요.");
        yield return new WaitForSeconds(2.5f);

        currentStep = TutorialStep.CameraSetup;
        if (VisionTrackingManager.Instance != null)
            VisionTrackingManager.Instance.allowCameraActivation = true;

        if (UIManager.Instance != null)
        {
            UIManager.Instance.HideSubtitle();
            yield return StartCoroutine(UIManager.Instance.RunCameraSetup(requireDecision: true));
        }

        if (AudioManager.Instance != null && VisionTrackingManager.Instance != null && VisionTrackingManager.Instance.IsCalibrated)
            AudioManager.Instance.PlayGlobal2D("SND-072_CalibrationComplete_OneShot", AudioManager.Instance.uiMixerGroup, 0.8f);
        yield return new WaitForSeconds(0.4f);

        // 먼저 시야 회전을 익힙니다.
        currentStep = TutorialStep.Rotation;
        IsCameraLocked = false;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle(IsUsingFallback() ? "마우스를 움직여 주변을 둘러보세요." :
                "고개를 천천히 위·아래·좌·우로 움직여 주변을 둘러보세요.");
        yield return new WaitForSeconds(3.5f);

        // 이어서 실제 이동을 직접 수행하게 합니다.
        currentStep = TutorialStep.Movement;
        if (GetTutorialPlayer() != null)
            GetTutorialPlayer().SetMovementLock(false);

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

            if (leanTargetPoint != null && GetTutorialPlayer() != null)
            {
                float dist = Vector3.Distance(
                    GetTutorialPlayer().transform.position,
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
            UIManager.Instance.ShowSubtitle("[ESC] → 설정 → 시선에서 상하·좌우 시선 감도를 조절해보세요. 완료하면 게임으로 돌아오세요.");
        yield return StartCoroutine(WaitForSystemMenuOpenAndClose());

        // 수동 숨참기 튜토리얼은 제거했습니다.
        // 은신 중 호흡 억제/발각 완화 효과는 HidingSpotManager의 은신 상태를 통해
        // PlayerController와 StateManager가 자동으로 처리합니다.
        currentStep = TutorialStep.Interaction;
        if (UIManager.Instance != null)
            UIManager.Instance.ShowSubtitle(IsUsingFallback() ? "흰색으로 빛나는 사물을 바라보고 마우스 왼쪽 버튼을 눌러 상호작용하세요." :
                "흰색으로 빛나는 사물을 바라보세요. 손을 뻗은 상태에서 가볍게 주먹을 쥐었다가 다시 펴면 상호작용합니다.");

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
            UIManager.Instance.ShowSubtitle("위협이 다가옵니다! 바닥의 흰색 안내선을 따라 은신처로 이동한 뒤, 은신처를 바라보고 상호작용하세요.");

        if (hidingFloorRoute != null && testHidingSpot != null)
        {
            PlayerController routePlayer = GetTutorialPlayer();
            hidingFloorRoute.SetTargets(routePlayer != null ? routePlayer.transform : null, testHidingSpot.transform);
            hidingFloorRoute.ShowRoute();
        }

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
        if (hidingFloorRoute != null) hidingFloorRoute.HideRoute();

        if (testHidingSpot != null)
        {
            testHidingSpot.DisableObjectiveHighlight();
            testHidingSpot.isInteractable = false;
        }

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

    private bool IsUsingFallback()
    {
        PlayerController player = GetTutorialPlayer();
        return player != null && player.AllowsKeyboardMouse;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (currentStep == TutorialStep.CameraSetup && UIManager.Instance != null)
            UIManager.Instance.CloseCameraSetup();
    }

    // 튜토리얼에서 강조한 렌더러만 원래 발광/PropertyBlock 상태를 복구하기 위한 캐시입니다.
    private readonly Dictionary<Renderer, MaterialPropertyBlock> _highlightOriginalBlocks =
        new Dictionary<Renderer, MaterialPropertyBlock>();
    private readonly Dictionary<Renderer, Material[]> _highlightMaterials =
        new Dictionary<Renderer, Material[]>();
    private readonly Dictionary<Renderer, bool[]> _highlightOriginalEmissionKeywords =
        new Dictionary<Renderer, bool[]>();

    private IEnumerator PulseHighlightRoutine(Transform targetTransform)
    {
        if (targetTransform == null) yield break;
        _activeHighlightRenderers = targetTransform.GetComponentsInChildren<Renderer>(true);
        if (_activeHighlightRenderers.Length == 0) yield break;

        int emissionColorID = Shader.PropertyToID("_EmissionColor");
        _highlightOriginalBlocks.Clear();
        _highlightMaterials.Clear();
        _highlightOriginalEmissionKeywords.Clear();

        // 강조 대상(testObject) 하위 렌더러만 별도 Material 인스턴스로 처리합니다.
        // sharedMaterial의 키워드를 변경하면 같은 재질을 쓰는 다른 문까지 영향을 받을 수 있습니다.
        foreach (Renderer r in _activeHighlightRenderers)
        {
            if (r == null) continue;

            MaterialPropertyBlock originalBlock = new MaterialPropertyBlock();
            r.GetPropertyBlock(originalBlock);
            _highlightOriginalBlocks[r] = originalBlock;

            Material[] materials = r.materials;
            bool[] originalKeywords = new bool[materials.Length];
            for (int i = 0; i < materials.Length; i++)
            {
                Material material = materials[i];
                if (material == null) continue;
                originalKeywords[i] = material.IsKeywordEnabled("_EMISSION");
                if (material.HasProperty(emissionColorID)) material.EnableKeyword("_EMISSION");
            }
            _highlightMaterials[r] = materials;
            _highlightOriginalEmissionKeywords[r] = originalKeywords;
        }

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        while (true)
        {
            float lerp = Mathf.PingPong(Time.time * highlightPulseSpeed, 1f);
            Color peak = testObject != null && targetTransform == testObject.transform ? highlightColor * 1.5f : highlightColor;
            Color finalColor = Color.Lerp(Color.black, peak, lerp);

            foreach (Renderer r in _activeHighlightRenderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(propBlock);
                propBlock.SetColor(emissionColorID, finalColor);
                r.SetPropertyBlock(propBlock);
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

        if (_activeHighlightRenderers != null)
        {
            foreach (Renderer r in _activeHighlightRenderers)
            {
                if (r == null) continue;

                // 강조 전의 PropertyBlock을 그대로 복구해 원래 재질 발광을 훼손하지 않습니다.
                if (_highlightOriginalBlocks.TryGetValue(r, out MaterialPropertyBlock originalBlock))
                    r.SetPropertyBlock(originalBlock);

                if (_highlightMaterials.TryGetValue(r, out Material[] materials) &&
                    _highlightOriginalEmissionKeywords.TryGetValue(r, out bool[] originalKeywords))
                {
                    for (int i = 0; i < materials.Length && i < originalKeywords.Length; i++)
                    {
                        if (materials[i] == null) continue;
                        if (originalKeywords[i]) materials[i].EnableKeyword("_EMISSION");
                        else materials[i].DisableKeyword("_EMISSION");
                    }
                }
            }
        }

        _highlightOriginalBlocks.Clear();
        _highlightMaterials.Clear();
        _highlightOriginalEmissionKeywords.Clear();
        _activeHighlightRenderers = null;
    }
}

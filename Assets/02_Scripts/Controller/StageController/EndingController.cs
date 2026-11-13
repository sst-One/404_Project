using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EndingController : MonoBehaviour
{
    [Header("Ending Scene UI (기존 씬 연결 유지)")]
    public CanvasGroup titleCanvasGroup;
    public RectTransform creditsRectTransform;
    public float titleHoldTime = 2f;
    public float titleFadeTime = 1.5f;
    public float creditScrollSpeed = 50f;
    public float endDelay = 3f;
    public float overrideTargetY = 0f;

    [Header("Ending Audio")]
    public AudioSource endingBgmSource;
    public string endingBgmClipName = "SND-065_EndingTheme_Loop";

    private PlayerController _playerController;
    private FirstPersonCameraLook _cameraLook;
    private bool _playerControllerWasEnabled;
    private bool _cameraLookWasEnabled;
    private bool _controlsLocked;
    private bool _isReturningToTitle;
    private Coroutine _endingRoutine;
    private GameObject _skipButtonObject;

    private void Start()
    {
        LockPlayerControls();

        // 엔딩에 도달하면 이어하기 저장 데이터를 제거합니다.
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResetSavedGameProgress();

        StartEndingMusic();
        _endingRoutine = StartCoroutine(EndingSequenceRoutine());
    }

    private void LockPlayerControls()
    {
        _playerController = PlayerController.Instance;
        if (_playerController != null)
        {
            _playerControllerWasEnabled = _playerController.enabled;
            _playerController.SetMovementLock(true);
            _playerController.SetCameraLock(true);
            _cameraLook = _playerController.CamLook;
        }

        if (_cameraLook == null)
            _cameraLook = FindObjectOfType<FirstPersonCameraLook>();

        if (_cameraLook != null)
            _cameraLookWasEnabled = _cameraLook.enabled;

        // 락 플래그로 차단되지 않는 상호작용 입력까지 막습니다.
        if (_playerController != null)
            _playerController.enabled = false;
        if (_cameraLook != null)
            _cameraLook.enabled = false;

        _controlsLocked = true;
    }

    private void RestorePlayerControls()
    {
        if (!_controlsLocked) return;

        if (_playerController != null)
        {
            _playerController.enabled = _playerControllerWasEnabled;
            _playerController.SetMovementLock(false);
            _playerController.SetCameraLock(false);
        }

        if (_cameraLook != null)
            _cameraLook.enabled = _cameraLookWasEnabled;

        _controlsLocked = false;
    }

    private void StartEndingMusic()
    {
        AudioClip clip = AudioManager.Instance != null
            ? AudioManager.Instance.GetClip(endingBgmClipName)
            : null;

        if (endingBgmSource != null)
        {
            if (clip != null) endingBgmSource.clip = clip;
            if (endingBgmSource.clip != null)
            {
                endingBgmSource.loop = true;
                endingBgmSource.Play();
                return;
            }
        }

        if (AudioManager.Instance != null && !string.IsNullOrEmpty(endingBgmClipName))
            AudioManager.Instance.PlayBGM(endingBgmClipName);
    }

    private IEnumerator EndingSequenceRoutine()
    {
        // 기존 엔딩 타이틀 페이드 연출을 유지합니다.
        if (titleCanvasGroup != null)
        {
            titleCanvasGroup.alpha = 1f;
            if (titleHoldTime > 0f)
                yield return new WaitForSecondsRealtime(titleHoldTime);

            float duration = Mathf.Max(0f, titleFadeTime);
            if (duration > 0f)
            {
                float elapsed = 0f;
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    titleCanvasGroup.alpha = Mathf.Lerp(1f, 0f, Mathf.Clamp01(elapsed / duration));
                    yield return null;
                }
            }
            titleCanvasGroup.alpha = 0f;
        }

        // 크레딧이 시작되는 시점에 언제든 메인 화면으로 이동할 수 있는 스킵 버튼을 표시합니다.
        CreateSkipButton();

        // 크레딧 전체가 화면 위로 완전히 빠져나갈 때까지 스크롤합니다.
        if (creditsRectTransform != null && creditScrollSpeed > 0f)
        {
            TMP_Text creditsText = creditsRectTransform.GetComponent<TMP_Text>();
            if (creditsText != null)
                creditsText.ForceMeshUpdate();

            // RectTransform 높이가 짧게 설정되어 있어도 실제 텍스트 전체 높이를 기준으로
            // 스크롤 종료 지점을 계산해 마지막 줄까지 화면 밖으로 이동시킵니다.
            float contentHeight = creditsRectTransform.rect.height;
            if (creditsText != null)
                contentHeight = Mathf.Max(contentHeight, creditsText.preferredHeight);

            float targetY = Mathf.Approximately(overrideTargetY, 0f)
                ? Screen.height + contentHeight
                : overrideTargetY;

            while (creditsRectTransform != null &&
                   creditsRectTransform.anchoredPosition.y < targetY)
            {
                Vector2 position = creditsRectTransform.anchoredPosition;
                position.y = Mathf.Min(targetY, position.y + creditScrollSpeed * Time.unscaledDeltaTime);
                creditsRectTransform.anchoredPosition = position;
                yield return null;
            }
        }
        else if (creditsRectTransform == null)
        {
            Debug.LogWarning("[EndingController] 크레딧 RectTransform이 연결되지 않아 크레딧 스크롤을 건너뜁니다.", this);
        }

        // 기존 씬 설정의 endDelay(기본 3초)를 유지한 뒤 자동으로 메인 화면으로 복귀합니다.
        if (endDelay > 0f)
            yield return new WaitForSecondsRealtime(endDelay);

        ReturnToTitle();
    }

    private void CreateSkipButton()
    {
        if (_skipButtonObject != null || creditsRectTransform == null) return;

        Canvas canvas = creditsRectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[EndingController] 엔딩 Canvas를 찾지 못해 크레딧 스킵 버튼을 만들 수 없습니다.", this);
            return;
        }

        _skipButtonObject = new GameObject("Credits_SkipButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform buttonRect = _skipButtonObject.GetComponent<RectTransform>();
        buttonRect.SetParent(canvas.transform, false);
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.sizeDelta = new Vector2(190f, 64f);
        buttonRect.anchoredPosition = new Vector2(-48f, 48f);

        Image background = _skipButtonObject.GetComponent<Image>();
        background.color = new Color(0.06f, 0.06f, 0.06f, 0.88f);
        background.raycastTarget = true;

        Button button = _skipButtonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(1f, 1f, 1f, 1f);
        colors.highlightedColor = new Color(0.82f, 0.9f, 1f, 1f);
        colors.pressedColor = new Color(0.65f, 0.75f, 0.9f, 1f);
        button.colors = colors;
        button.onClick.AddListener(ReturnToTitle);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(buttonRect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        TMP_Text sourceCredits = creditsRectTransform.GetComponent<TMP_Text>();
        if (sourceCredits != null) label.font = sourceCredits.font;
        label.text = "SKIP  ▶";
        label.fontSize = 26f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        buttonRect.SetAsLastSibling();
    }

    private void ReturnToTitle()
    {
        if (_isReturningToTitle) return;
        _isReturningToTitle = true;

        if (_endingRoutine != null)
        {
            StopCoroutine(_endingRoutine);
            _endingRoutine = null;
        }

        if (_skipButtonObject != null)
        {
            Destroy(_skipButtonObject);
            _skipButtonObject = null;
        }

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.ResetSavedGameProgress();
            // GameFlowManager가 페이드아웃 → 엔딩 씬 언로드 → 메인 메뉴 표시를 처리합니다.
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
        else
        {
            Debug.LogError("[EndingController] GameFlowManager를 찾지 못해 메인 화면으로 돌아갈 수 없습니다.", this);
            RestorePlayerControls();
        }
    }

    private void OnDestroy()
    {
        RestorePlayerControls();
    }
}

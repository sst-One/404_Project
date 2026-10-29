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

    [Header("Return to Title")]
    [Min(0f)] public float returnButtonDelay = 10f;
    public string returnButtonLabel = "게임 시작 화면으로 돌아가기";

    private PlayerController _playerController;
    private FirstPersonCameraLook _cameraLook;
    private bool _playerControllerWasEnabled;
    private bool _cameraLookWasEnabled;
    private bool _controlsLocked;
    private bool _isReturningToTitle;
    private GameObject _returnButtonObject;

    private void Start()
    {
        LockPlayerControls();

        // 엔딩에 도달하면 이어하기 저장 데이터를 제거합니다.
        // GameFlowManager에서도 Stage13_Ending 저장을 차단해 재저장을 방지합니다.
        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResetSavedGameProgress();

        StartEndingMusic();
        StartCoroutine(EndingSequenceRoutine());
        StartCoroutine(ShowReturnButtonRoutine());
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

        // 락 플래그만으로 막히지 않는 상호작용 입력까지 차단합니다.
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
        // 기존 엔딩 씬의 타이틀 페이드 효과를 유지합니다.
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

        // 기존 엔딩 씬의 크레딧 스크롤 설정을 유지합니다.
        if (creditsRectTransform != null && creditScrollSpeed > 0f)
        {
            float targetY = Mathf.Approximately(overrideTargetY, 0f)
                ? Screen.height + creditsRectTransform.rect.height
                : overrideTargetY;

            while (creditsRectTransform.anchoredPosition.y < targetY)
            {
                Vector2 position = creditsRectTransform.anchoredPosition;
                position.y = Mathf.Min(targetY, position.y + creditScrollSpeed * Time.unscaledDeltaTime);
                creditsRectTransform.anchoredPosition = position;
                yield return null;
            }
        }

        // 크레딧 종료 후 자동으로 타이틀로 이동하지 않습니다.
        // 사용자가 10초 후 표시되는 버튼을 눌러 직접 돌아갑니다.
        if (endDelay > 0f)
            yield return new WaitForSecondsRealtime(endDelay);
    }

    private IEnumerator ShowReturnButtonRoutine()
    {
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, returnButtonDelay));
        if (!_isReturningToTitle)
            CreateReturnButton();
    }

    private void CreateReturnButton()
    {
        Canvas canvas = FindObjectOfType<Canvas>(true);
        if (canvas == null)
        {
            Debug.LogError("[EndingController] 엔딩 씬에서 버튼을 배치할 Canvas를 찾지 못했습니다.");
            return;
        }

        // 09_Ending.unity의 Canvas RectTransform은 원본 파일에서 localScale이 (0, 0, 0)입니다.
        // 이 상태에서 자식으로 버튼을 생성하면 버튼도 월드 스케일이 0이 되어 보이지 않습니다.
        // 버튼 생성 전에 Canvas 스케일을 정상화합니다.
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        if (canvasRect != null && (Mathf.Approximately(canvasRect.localScale.x, 0f) ||
                                   Mathf.Approximately(canvasRect.localScale.y, 0f) ||
                                   Mathf.Approximately(canvasRect.localScale.z, 0f)))
        {
            canvasRect.localScale = Vector3.one;
            Debug.LogWarning("[EndingController] 엔딩 Canvas의 스케일이 0이어서 Vector3.one으로 복구했습니다.");
        }

        canvas.gameObject.SetActive(true);
        _returnButtonObject = new GameObject("ReturnToTitleButton", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = _returnButtonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.SetAsLastSibling();
        rect.localScale = Vector3.one;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 90f);
        rect.sizeDelta = new Vector2(460f, 78f);

        Image image = _returnButtonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.12f, 0.12f, 0.94f);

        Button button = _returnButtonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = new ColorBlock
        {
            normalColor = new Color(0.18f, 0.18f, 0.18f, 0.96f),
            highlightedColor = new Color(0.32f, 0.32f, 0.32f, 1f),
            pressedColor = new Color(0.08f, 0.08f, 0.08f, 1f),
            selectedColor = new Color(0.32f, 0.32f, 0.32f, 1f),
            disabledColor = new Color(0.15f, 0.15f, 0.15f, 0.5f),
            colorMultiplier = 1f,
            fadeDuration = 0.1f
        };
        button.onClick.AddListener(ReturnToTitle);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        TextMeshProUGUI label = labelObject.GetComponent<TextMeshProUGUI>();
        label.text = string.IsNullOrEmpty(returnButtonLabel) ? "게임 시작 화면으로 돌아가기" : returnButtonLabel;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = 26f;
        label.color = Color.white;
        label.raycastTarget = false;

        TMP_Text existingText = canvas.GetComponentInChildren<TMP_Text>(true);
        if (existingText != null && existingText.font != null)
            label.font = existingText.font;
        else if (TMP_Settings.defaultFontAsset != null)
            label.font = TMP_Settings.defaultFontAsset;
    }

    private void ReturnToTitle()
    {
        if (_isReturningToTitle) return;
        _isReturningToTitle = true;

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.ResetSavedGameProgress();

        if (GameFlowManager.Instance != null)
        {
            // 씬 전환 페이드가 끝나고 엔딩 씬이 언로드될 때 OnDestroy에서 조작을 복구합니다.
            // 전환 중 플레이어가 잠깐 움직이는 것을 방지하기 위해 여기서는 잠금을 유지합니다.
            GameFlowManager.Instance.AdvanceToStage(GameStage.Title);
        }
        else
        {
            RestorePlayerControls();
            Debug.LogError("[EndingController] GameFlowManager를 찾지 못해 타이틀로 돌아갈 수 없습니다.");
        }
    }

    private void OnDestroy()
    {
        RestorePlayerControls();
    }
}

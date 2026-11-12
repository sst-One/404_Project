using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 상호작용 가능한 대상이 시선과 감지 범위 안에 있을 때만
/// 열린 손/주먹 이미지를 번갈아 표시합니다.
/// </summary>
public class PlayerUIController : MonoBehaviour
{
    [Header("UI Reference")]
    public RectTransform indicatorRect;

    [Header("Interaction Hand Sprites")]
    public Sprite openHandSprite;
    public Sprite fistHandSprite;

    [Header("Display Settings")]
    public Vector2 indicatorSize = new Vector2(50f, 60f);
    public Color handColor = Color.white;
    [Min(0.05f)] public float animationInterval = 0.28f;

    private Image indicatorImage;
    private GameObject indicatorObject;
    private float animationTimer;
    private bool showFist;
    private bool isVisible;

    private void Awake()
    {
        ResolveIndicator();
        HideIndicator();
    }

    private void Update()
    {
        if (indicatorImage == null || indicatorObject == null)
            ResolveIndicator();

        if (indicatorImage == null || indicatorObject == null)
            return;

        PlayerController player = PlayerController.Instance;
        bool hasValidTarget = player != null &&
                              player.CurrentHoverTarget != null &&
                              player.CurrentHoverTarget.gameObject.activeInHierarchy &&
                              player.IsTargetReady;

        if (!hasValidTarget || openHandSprite == null || fistHandSprite == null)
        {
            HideIndicator();
            return;
        }

        if (!isVisible)
        {
            isVisible = true;
            animationTimer = 0f;
            showFist = false;
            indicatorObject.SetActive(true);
            indicatorImage.enabled = true;
        }

        animationTimer += Time.unscaledDeltaTime;
        if (animationTimer >= animationInterval)
        {
            animationTimer %= animationInterval;
            showFist = !showFist;
        }

        indicatorImage.sprite = showFist ? fistHandSprite : openHandSprite;
        indicatorImage.color = handColor;
        indicatorImage.preserveAspect = true;
        indicatorImage.raycastTarget = false;
    }

    private void ResolveIndicator()
    {
        if (indicatorRect != null)
        {
            ConfigureIndicator(indicatorRect);
            return;
        }

        // GameObject.Find는 비활성 오브젝트를 찾지 못하므로 Canvas 하위까지 비활성 포함 검색합니다.
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            if (canvas == null || !canvas.gameObject.scene.IsValid())
                continue;

            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name == "Indicator")
                {
                    indicatorRect = child as RectTransform;
                    if (indicatorRect != null)
                    {
                        ConfigureIndicator(indicatorRect);
                        return;
                    }
                }
            }
        }

        // 이름이 변경된 UI 프리팹에서도 기존 Indicator를 찾을 수 있도록 최후에 전역 검색합니다.
        GameObject found = GameObject.Find("Indicator");
        if (found != null)
        {
            indicatorRect = found.GetComponent<RectTransform>();
            if (indicatorRect != null) ConfigureIndicator(indicatorRect);
        }
    }

    private void ConfigureIndicator(RectTransform rect)
    {
        indicatorRect = rect;
        indicatorObject = rect.gameObject;

        // UI_Canvas 원본 프리팹의 루트 스케일이 (0,0,0)이면 자식 손 이미지도 렌더링되지 않습니다.
        Canvas parentCanvas = rect.GetComponentInParent<Canvas>();
        if (parentCanvas != null)
        {
            Transform canvasTransform = parentCanvas.transform;
            Vector3 canvasScale = canvasTransform.localScale;
            if (Mathf.Approximately(canvasScale.x, 0f) ||
                Mathf.Approximately(canvasScale.y, 0f) ||
                Mathf.Approximately(canvasScale.z, 0f))
            {
                canvasTransform.localScale = Vector3.one;
            }
        }

        indicatorImage = rect.GetComponent<Image>();

        if (indicatorImage == null)
            indicatorImage = rect.gameObject.AddComponent<Image>();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = indicatorSize;
        rect.localScale = Vector3.one;

        // Sprite가 없는 기본 Image가 사각형으로 렌더링되는 현상을 막습니다.
        indicatorImage.sprite = null;
        indicatorImage.color = new Color(1f, 1f, 1f, 0f);
        indicatorImage.raycastTarget = false;
        indicatorImage.enabled = false;
        indicatorObject.SetActive(false);
        isVisible = false;
    }

    private void HideIndicator()
    {
        isVisible = false;
        animationTimer = 0f;

        if (indicatorImage != null)
        {
            indicatorImage.sprite = null;
            indicatorImage.color = new Color(1f, 1f, 1f, 0f);
            indicatorImage.enabled = false;
        }

        if (indicatorObject != null && indicatorObject.activeSelf)
            indicatorObject.SetActive(false);
    }
}

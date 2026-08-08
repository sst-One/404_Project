using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialCalibrationUI : MonoBehaviour
{
    public static TutorialCalibrationUI Instance { get; private set; }

    [Header("UI Panels")]
    public GameObject calibrationPanel;
    public GameObject phase1OriginPanel;
    public GameObject phase2SliderPanel;

    [Header("Sliders")]
    public Slider rotationSpeedSlider;
    public Slider leanThresholdSlider;
    public Slider reachThresholdSlider;
    public Slider deadzoneSlider;

    [Header("Toggles")]
    public Toggle invertYawToggle;

    [Header("Value Texts")]
    public TextMeshProUGUI rotationValueText;
    public TextMeshProUGUI leanValueText;
    public TextMeshProUGUI reachValueText;
    public TextMeshProUGUI deadzoneValueText;

    [Header("Buttons")]
    public Button btnMeasureOrigin;
    public Button btnTestRecalibrate;
    public Button btnSaveOnly;

    public bool IsInCalibrationTestMode { get; private set; } = false;

    private FirstPersonCameraLook cameraLook;

    private void Awake() { Instance = this; }

    private void Start()
    {
        cameraLook = FindObjectOfType<FirstPersonCameraLook>();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        GameObject spawnPoint = GameObject.Find("PlayerSpawnPoint");
        if (spawnPoint != null && cameraLook != null)
        {
            Transform playerRoot = cameraLook.transform.root;
            CharacterController cc = playerRoot.GetComponent<CharacterController>();

            if (cc != null) cc.enabled = false;
            playerRoot.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
            if (cc != null) cc.enabled = true;
        }

        if (rotationSpeedSlider != null)
        {
            rotationSpeedSlider.minValue = 1f;
            rotationSpeedSlider.maxValue = 20f;
        }

        if (invertYawToggle != null)
        {
            invertYawToggle.isOn = PlayerPrefs.GetInt("InvertYaw", 0) == 1;
            invertYawToggle.onValueChanged.AddListener(v =>
            {
                if (cameraLook != null) cameraLook.SetInvertYaw(v);
            });
        }

        if (calibrationPanel != null) calibrationPanel.SetActive(true);
        if (phase1OriginPanel != null) phase1OriginPanel.SetActive(true);
        if (phase2SliderPanel != null) phase2SliderPanel.SetActive(false);

        if (btnMeasureOrigin != null) btnMeasureOrigin.onClick.AddListener(OnMeasureOriginClicked);
        if (btnTestRecalibrate != null) btnTestRecalibrate.onClick.AddListener(OnTestRecalibrateClicked);

        if (btnSaveOnly != null)
        {
            btnSaveOnly.onClick.RemoveAllListeners();
            btnSaveOnly.onClick.AddListener(OnSaveAndStartGame);
        }
    }

    private void OnMeasureOriginClicked()
    {
        if (btnMeasureOrigin != null) btnMeasureOrigin.interactable = false;

        if (VisionTrackingManager.Instance != null)
        {
            VisionTrackingManager.Instance.StartCalibration();
            StartCoroutine(WaitForOriginCalibration());
        }
        else
        {
            Debug.LogError("[TutorialCalibrationUI] VisionTrackingManager를 찾을 수 없습니다.");
        }
    }

    private IEnumerator WaitForOriginCalibration()
    {
        yield return new WaitUntil(() => VisionTrackingManager.Instance.IsCalibrated);

        if (phase1OriginPanel != null) phase1OriginPanel.SetActive(false);
        if (phase2SliderPanel != null) phase2SliderPanel.SetActive(true);

        IsInCalibrationTestMode = true;
        InitializeSliders();
    }

    private void InitializeSliders()
    {
        if (VisionTrackingManager.Instance != null)
        {
            if (leanThresholdSlider != null)
            {
                leanThresholdSlider.value = VisionTrackingManager.Instance.leanDepthThreshold;
                UpdateLeanText(leanThresholdSlider.value);
                leanThresholdSlider.onValueChanged.AddListener(v =>
                {
                    VisionTrackingManager.Instance.leanDepthThreshold = v;
                    UpdateLeanText(v);
                });
            }
            if (reachThresholdSlider != null)
            {
                reachThresholdSlider.value = VisionTrackingManager.Instance.reachDepthThreshold;
                UpdateReachText(reachThresholdSlider.value);
                reachThresholdSlider.onValueChanged.AddListener(v =>
                {
                    VisionTrackingManager.Instance.reachDepthThreshold = v;
                    UpdateReachText(v);
                });
            }
        }
        if (cameraLook != null)
        {
            if (rotationSpeedSlider != null)
            {
                rotationSpeedSlider.value = cameraLook.headRotationSpeed;
                UpdateRotationText(rotationSpeedSlider.value);
                rotationSpeedSlider.onValueChanged.AddListener(v =>
                {
                    cameraLook.headRotationSpeed = v;
                    UpdateRotationText(v);
                });
            }
            if (deadzoneSlider != null)
            {
                deadzoneSlider.value = cameraLook.deadzoneRadius;
                UpdateDeadzoneText(deadzoneSlider.value);
                deadzoneSlider.onValueChanged.AddListener(v =>
                {
                    cameraLook.deadzoneRadius = v;
                    UpdateDeadzoneText(v);
                });
            }
        }
    }

    private void UpdateLeanText(float val) { if (leanValueText != null) leanValueText.text = string.Format("이동(기울임) 민감도: {0:F2}", val); }
    private void UpdateReachText(float val) { if (reachValueText != null) reachValueText.text = string.Format("조작(손뻗기) 민감도: {0:F2}", val); }
    private void UpdateRotationText(float val) { if (rotationValueText != null) rotationValueText.text = string.Format("화면 회전 감도: {0:F0}", val); }
    private void UpdateDeadzoneText(float val) { if (deadzoneValueText != null) deadzoneValueText.text = string.Format("미세 떨림 무시(데드존): {0:F3}", val); }

    private void OnTestRecalibrateClicked()
    {
        if (rotationSpeedSlider != null) rotationSpeedSlider.value = 10f;
        if (leanThresholdSlider != null) leanThresholdSlider.value = 0.2f;
        if (reachThresholdSlider != null) reachThresholdSlider.value = 0.3f;
        if (deadzoneSlider != null) deadzoneSlider.value = 2.0f;

        if (cameraLook != null) cameraLook.ResetView();
        if (VisionTrackingManager.Instance != null) VisionTrackingManager.Instance.RecalibrateOrigin();
    }

    private void OnSaveAndStartGame()
    {
        if (VisionTrackingManager.Instance != null)
        {
            PlayerPrefs.SetFloat("LeanThreshold", VisionTrackingManager.Instance.leanDepthThreshold);
            PlayerPrefs.SetFloat("ReachThreshold", VisionTrackingManager.Instance.reachDepthThreshold);
        }
        if (cameraLook != null)
        {
            PlayerPrefs.SetFloat("RotationSpeed", cameraLook.headRotationSpeed);
            PlayerPrefs.SetFloat("DeadzoneRadius", cameraLook.deadzoneRadius);
        }

        PlayerPrefs.SetInt("IsCalibrated", 1);
        PlayerPrefs.Save();

        if (calibrationPanel != null) calibrationPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        IsInCalibrationTestMode = false;

        if (GameFlowManager.Instance != null)
        {
            GameFlowManager.Instance.AdvanceToStage(GameStage.Stage1_Elevator);
        }
    }
}
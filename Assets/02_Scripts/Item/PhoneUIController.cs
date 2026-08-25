using UnityEngine;

public class PhoneUIController : MonoBehaviour
{
    [Header("Screen Root (전원 통제용)")]
    public GameObject screenRoot;
    public GameObject crackedScreenOverlay;

    [Header("Default & Disaster Screens")]
    public GameObject defaultScreen; // [복구] 기본 잠금/대기 화면
    public GameObject lockDay2;
    public GameObject lockDay3;
    public GameObject lockDay4;
    public GameObject lockDay5;

    public GameObject msgDay2;
    public GameObject msgDay3;
    public GameObject msgDay4;

    [Header("Call Screens (신고 화면)")]
    public GameObject dial112Screen;
    public GameObject callingScreen;
    public GameObject policeCallScreen;
    public GameObject policeReceiveScreen;

    private void Start()
    {
        TurnOffScreen();
    }

    public void TurnOnScreen() { if (screenRoot != null) screenRoot.SetActive(true); }
    public void TurnOffScreen() { if (screenRoot != null) screenRoot.SetActive(false); }

    public void HideAllScreens()
    {
        if (defaultScreen != null) defaultScreen.SetActive(false); // [복구]

        if (lockDay2 != null) lockDay2.SetActive(false);
        if (lockDay3 != null) lockDay3.SetActive(false);
        if (lockDay4 != null) lockDay4.SetActive(false);
        if (lockDay5 != null) lockDay5.SetActive(false);

        if (msgDay2 != null) msgDay2.SetActive(false);
        if (msgDay3 != null) msgDay3.SetActive(false);
        if (msgDay4 != null) msgDay4.SetActive(false);

        if (dial112Screen != null) dial112Screen.SetActive(false);
        if (callingScreen != null) callingScreen.SetActive(false);
        if (policeCallScreen != null) policeCallScreen.SetActive(false);
        if (policeReceiveScreen != null) policeReceiveScreen.SetActive(false);
    }

    // --- 공통 화면 제어 ---
    public void ShowDefaultScreen() { TurnOnScreen(); HideAllScreens(); if (defaultScreen != null) defaultScreen.SetActive(true); }

    // --- Day별 팝업 제어 ---
    public void ShowDay2Message() { TurnOnScreen(); HideAllScreens(); if (lockDay2 != null) lockDay2.SetActive(true); if (msgDay2 != null) msgDay2.SetActive(true); }
    public void ShowDay3Message() { TurnOnScreen(); HideAllScreens(); if (lockDay3 != null) lockDay3.SetActive(true); if (msgDay3 != null) msgDay3.SetActive(true); }
    public void ShowDay4Message() { TurnOnScreen(); HideAllScreens(); if (lockDay4 != null) lockDay4.SetActive(true); if (msgDay4 != null) msgDay4.SetActive(true); }

    // --- 통화 제어 ---
    public void ShowDial112() { TurnOnScreen(); HideAllScreens(); if (dial112Screen != null) dial112Screen.SetActive(true); }
    public void ShowCalling() { TurnOnScreen(); HideAllScreens(); if (callingScreen != null) callingScreen.SetActive(true); }

    // [복구] 메서드명 원복 (ShowPoliceSuccess -> ShowPoliceCall)
    public void ShowPoliceCall() { TurnOnScreen(); HideAllScreens(); if (policeCallScreen != null) policeCallScreen.SetActive(true); }

    public void ShowPoliceReceive() { TurnOnScreen(); HideAllScreens(); if (lockDay5 != null) lockDay5.SetActive(true); if (policeReceiveScreen != null) policeReceiveScreen.SetActive(true); }

    public void SetCrackedScreen(bool isCracked)
    {
        if (crackedScreenOverlay != null) crackedScreenOverlay.SetActive(isCracked);
    }
}
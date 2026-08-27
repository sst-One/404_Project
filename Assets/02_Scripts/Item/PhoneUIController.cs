using UnityEngine;

public class PhoneUIController : MonoBehaviour
{
    [Header("Cracked Screen Overlay")]
    public GameObject crackedScreenOverlay;

    [Header("UI Screens")]
    public GameObject defaultScreen;
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

    private void Awake()
    {
        HideAllScreens();
    }

    public void HideAllScreens()
    {
        if (defaultScreen != null) defaultScreen.SetActive(false);
        if (crackedScreenOverlay != null) crackedScreenOverlay.SetActive(false);

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

    public void ShowDefaultScreen() { HideAllScreens(); if (defaultScreen != null) defaultScreen.SetActive(true); }

    public void ShowLockDay2() { HideAllScreens(); if (lockDay2 != null) lockDay2.SetActive(true); }
    public void ShowLockDay3() { HideAllScreens(); if (lockDay3 != null) lockDay3.SetActive(true); }
    public void ShowLockDay4() { HideAllScreens(); if (lockDay4 != null) lockDay4.SetActive(true); }
    public void ShowLockDay5() { HideAllScreens(); if (lockDay5 != null) lockDay5.SetActive(true); }

    public void ShowMsgDay2() { HideAllScreens(); if (msgDay2 != null) msgDay2.SetActive(true); }
    public void ShowMsgDay3() { HideAllScreens(); if (msgDay3 != null) msgDay3.SetActive(true); }
    public void ShowMsgDay4() { HideAllScreens(); if (msgDay4 != null) msgDay4.SetActive(true); }

    public void ShowDial112Screen() { HideAllScreens(); if (dial112Screen != null) dial112Screen.SetActive(true); }
    public void ShowCallingScreen() { HideAllScreens(); if (callingScreen != null) callingScreen.SetActive(true); }
    public void ShowPoliceCallScreen() { HideAllScreens(); if (policeCallScreen != null) policeCallScreen.SetActive(true); }
    public void ShowPoliceReceiveScreen() { HideAllScreens(); if (policeReceiveScreen != null) policeReceiveScreen.SetActive(true); }

    public void SetCrackedScreen(bool isCracked)
    {
        if (crackedScreenOverlay != null) crackedScreenOverlay.SetActive(isCracked);
    }
}
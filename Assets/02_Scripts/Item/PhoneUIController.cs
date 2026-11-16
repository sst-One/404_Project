using TMPro;
using UnityEngine.UI;
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

    [Header("Optional message body labels")]
    public TMP_Text day2MessageText;
    public TMP_Text day3MessageText;
    public TMP_Text day4MessageText;
    public TMP_Text policeReceiveText;

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
    public void ShowLockDay5()
    {
        HideAllScreens();
        // UI-009 is the cracked lock screen. UI-029 is the incoming message.
        GameObject screen = defaultScreen != null ? defaultScreen : lockDay5;
        if (screen != null) screen.SetActive(true);
    }

    public void ShowMsgDay2() { HideAllScreens(); if (msgDay2 != null) msgDay2.SetActive(true); }
    public void ShowMsgDay3() { HideAllScreens(); if (msgDay3 != null) msgDay3.SetActive(true); }
    public void ShowMsgDay4() { HideAllScreens(); if (msgDay4 != null) msgDay4.SetActive(true); }

    public void ShowDial112Screen() { HideAllScreens(); if (dial112Screen != null) dial112Screen.SetActive(true); }
    public void ShowCallingScreen() { HideAllScreens(); if (callingScreen != null) callingScreen.SetActive(true); }
    public void ShowPoliceCallScreen() { HideAllScreens(); if (policeCallScreen != null) policeCallScreen.SetActive(true); }
    public void ShowPoliceReceiveScreen() { ShowPoliceReceiveScreen(NarrativeData.Day5_Police); }
    public void ShowPoliceReceiveScreen(string message)
    {
        HideAllScreens();
        if (policeReceiveText != null) policeReceiveText.text = NarrativeData.FormatDialogueText(message);
        if (policeReceiveScreen != null) policeReceiveScreen.SetActive(true);
    }

    public void SetPoliceReceiveText(string message)
    {
        if (policeReceiveText != null) policeReceiveText.text = NarrativeData.FormatDialogueText(message);
    }

    public void ShowPoliceMessage(string message)
    {
        ShowPoliceReceiveScreen(FormatPoliceMessage(message, false));
    }

    public void SetPoliceMessageText(string message, bool outgoing = false)
    {
        SetPoliceReceiveText(FormatPoliceMessage(message, outgoing));
    }

    private static string FormatPoliceMessage(string message, bool outgoing)
    {
        return (outgoing ? "문자 메시지 · 나" : "문자 메시지 · 박 경위") + "\n\n" + message;
    }

    public void SetCrackedScreen(bool isCracked)
    {
        if (crackedScreenOverlay == null) return;
        bool alreadyCracked = policeReceiveScreen != null && policeReceiveScreen.activeSelf &&
            policeReceiveScreen.TryGetComponent(out RawImage messageImage) &&
            crackedScreenOverlay.TryGetComponent(out RawImage crackImage) &&
            messageImage.texture == crackImage.texture;
        crackedScreenOverlay.SetActive(isCracked && !alreadyCracked);
    }
}

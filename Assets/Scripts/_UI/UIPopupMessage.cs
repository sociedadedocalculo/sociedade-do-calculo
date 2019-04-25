using System;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupMessage : MonoBehaviour
{
    public GameObject panel;
    public Text messageText;

    public void Show(string message)
    {
        // append error if visible, set otherwise. then show it.
        if (panel.activeSelf) messageText.text += ";\n" + message;
        else messageText.text = message;
        panel.SetActive(true);
    }

    internal void Show()
    {
        throw new NotImplementedException();
    }
}

// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using UnityEngine;
using UnityEngine.UI;

// =======================================================================================
// UCE UI PROMPT
// =======================================================================================
public class UCE_UI_Prompt : MonoBehaviour
{
    public GameObject panel;
    public Text messageText;
    public bool forceUseChat;

    // -----------------------------------------------------------------------------------
    // Show
    // -----------------------------------------------------------------------------------
    public void Show(string message)
    {
        messageText.text = message;
        panel.SetActive(true);
    }

    // -----------------------------------------------------------------------------------
}

// =======================================================================================
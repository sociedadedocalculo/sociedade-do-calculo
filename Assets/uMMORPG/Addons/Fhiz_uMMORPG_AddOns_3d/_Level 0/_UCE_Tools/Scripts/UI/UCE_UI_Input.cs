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
// UCE UI INPUT
// =======================================================================================
public class UCE_UI_Input : MonoBehaviour
{
    public GameObject panel;
    public Text messageText;
    public Text amountText;
    public Slider amountSlider;
    public Button buttonConfirm;

    private UCE_UI_InputCallback instance;

    // -----------------------------------------------------------------------------------
    // Show
    // -----------------------------------------------------------------------------------
    public void Show(string message, int minAmount, int maxAmount, UCE_UI_InputCallback callbackObject)
    {
        instance = callbackObject;
        messageText.text = message;
        amountSlider.value = 0;
        amountSlider.minValue = minAmount;
        amountSlider.maxValue = maxAmount;
        amountText.text = amountSlider.value.ToString() + "/" + maxAmount.ToString();
        panel.SetActive(true);
    }

    // -----------------------------------------------------------------------------------
    // SliderValueChanged
    // -----------------------------------------------------------------------------------
    public void SliderValueChanged()
    {
        amountText.text = amountSlider.value.ToString() + "/" + amountSlider.maxValue.ToString();
    }

    // -----------------------------------------------------------------------------------
    // Confirm
    // -----------------------------------------------------------------------------------
    public void Confirm()
    {
        instance.ConfirmInput((int)amountSlider.value);
        panel.SetActive(false);
    }

    // -----------------------------------------------------------------------------------
    // Hide
    // -----------------------------------------------------------------------------------
    public void Hide()
    {
        panel.SetActive(false);
    }

    // -----------------------------------------------------------------------------------
}

// =======================================================================================
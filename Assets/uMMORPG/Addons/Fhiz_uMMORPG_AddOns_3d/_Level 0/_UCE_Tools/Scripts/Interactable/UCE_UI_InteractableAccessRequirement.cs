// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Globalization;

// ===================================================================================
// UCE UI INTERACTABLE ACCESS REQUIREMENT
// ===================================================================================
public partial class UCE_UI_InteractableAccessRequirement : UCE_UI_Requirement
{
	
	[Header("[COSTS]")]
	public string labelGoldCost						= " - Gold cost per use: ";
    public string labelCoinCost 					= " - Coins cost per use: ";
#if _FHIZHONORSHOP
    public string labelRequiredHonorCurrency 		= " - Honor Currency cost: ";
#endif
  
    protected UCE_Interactable interactable;
	
    // -----------------------------------------------------------------------------------
    // Show
    // -----------------------------------------------------------------------------------
    public void Show(UCE_Interactable _interactable)
    {
    
        Player player = Player.localPlayer;
        if (!player) return;

        interactable = _interactable;
        requirements = interactable.interactionRequirements;
		
        for (int i = 0; i < content.childCount; ++i)
        {
            Destroy(content.GetChild(i).gameObject);
        }

        updateTextbox();

        interactButton.interactable = interactable.interactionRequirements.checkRequirements(player) || interactable.IsUnlocked();

        if (interactable.interactionText != "")
            interactButton.GetComponentInChildren<Text>().text = interactable.interactionText;

        interactButton.onClick.SetListener(() =>
        {
            interactable.ConfirmAccess();
            Hide();
        });

        panel.SetActive(true);
        
    }
		
   	// -----------------------------------------------------------------------------------
    // Update
    // -----------------------------------------------------------------------------------
    protected override void Update()
    {
        if (!panel.activeSelf) return;

        Player player = Player.localPlayer;
        if (!player) return;

        if (!UCE_Tools.UCE_CheckSelectionHandling(interactable.gameObject))
        {
            Hide();
        }
    }
    
    // -----------------------------------------------------------------------------------
    // updateTextbox
    // -----------------------------------------------------------------------------------
    protected override void updateTextbox()
    {
        Player player = Player.localPlayer;
        if (!player) return;
		
		base.updateTextbox();
		
		// ------------ Costs
		
		UCE_InteractionRequirements ir = (UCE_InteractionRequirements)requirements;
	
		if (ir.goldCost > 0)
			AddMessage(labelGoldCost + ir.goldCost.ToString(), player.Getgold() >= ir.goldCost ? textColor : errorColor);

		if (ir.coinCost > 0)
			AddMessage(labelCoinCost + ir.coinCost.ToString(), player.coins >= ir.coinCost ? textColor : errorColor);

#if _FHIZHONORSHOP
		if (ir.honorCurrencyCost.Length > 0)
		{
			AddMessage(labelRequiredHonorCurrency, textColor);
			foreach (UCE_HonorShopCurrencyDrop currency in ir.honorCurrencyCost)
			{
				if (player.UCE_GetHonorCurrency(currency.honorCurrency) < currency.amount)
				{
					AddMessage(currency.honorCurrency.name + " x" + currency.amount.ToString(), errorColor);
				}
				else
				{
					AddMessage(currency.honorCurrency.name + " x" + currency.amount.ToString(), textColor);
				}
			}
		}
#endif
	
    }


    // -----------------------------------------------------------------------------------
}

// =======================================================================================
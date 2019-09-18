// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using System.Linq;
using UnityEngine;

// =======================================================================================
// INTERACTION REQUIREMENTS CLASS
// THIS CLASS IS PRIMARILY FOR OBJECTS THE PLAYER CAN CHOOSE TO INTERACT WITH
// =======================================================================================
[System.Serializable]
public partial class UCE_InteractionRequirements : UCE_Requirements
{
    [Header("[-=-=-=- UCE COSTS [Removed after checking Requirements] -=-=-=-]")]
    [Tooltip("[Optional] These items will be removed from players inventory")]
    public UCE_ItemRequirement[] removeItems;

    [Tooltip("[Optional] Gold cost to interact")]
    public long goldCost = 0;

    [Tooltip("[Optional] Coins cost to interact")]
    public long coinCost = 0;

    [Tooltip("[Optional] Health cost to interact")]
    public int healthCost = 0;

    [Tooltip("[Optional] Mana cost to interact")]
    public int manaCost = 0;

#if _FHIZHONORSHOP

    [Tooltip("[Optional] Honor Currency costs to interact")]
    public UCE_HonorShopCurrencyDrop[] honorCurrencyCost;
#endif

    [Header("[-=-=-=- UCE REWARDS [awarded after checks & costs (repetitive)] -=-=-=-]")]
    public int expRewardMin = 0;
    public int expRewardMax = 0;
    public int skillExpRewardMin = 0;
    public int skillExpRewardMax = 0;

    // -----------------------------------------------------------------------------------
    // checkRequirements
    // -----------------------------------------------------------------------------------
    public override bool checkRequirements(Player player)
    {
        bool valid = true;

        valid = base.checkRequirements(player);

        valid = checkCosts(player, valid);

        return valid;
    }

    // -----------------------------------------------------------------------------------
    // checkCosts
    // -----------------------------------------------------------------------------------
    public bool checkCosts(Player player, bool valid)
    {
        valid = (removeItems.Length == 0 || player.UCE_checkHasItems(removeItems)) ? valid : false;
        valid = (goldCost == 0 || player.gold >= goldCost) ? valid : false;
        valid = (coinCost == 0 || player.coins >= coinCost) ? valid : false;
        valid = (healthCost == 0 || player.health >= healthCost) ? valid : false;
        valid = (manaCost == 0 || player.mana >= manaCost) ? valid : false;
#if _FHIZHONORSHOP
        valid = (player.UCE_CheckHonorCurrencyCost(honorCurrencyCost)) ? valid : false;
#endif

        return valid;
    }

    // -----------------------------------------------------------------------------------
    // hasCosts
    // -----------------------------------------------------------------------------------
    public bool hasCosts()
    {
        return removeItems.Length > 0 ||
                goldCost > 0 ||
                coinCost > 0 ||
                healthCost > 0 ||
                manaCost > 0
#if _FHIZHONORSHOP
                || honorCurrencyCost.Any(x => x.amount > 0)
#endif
                ;
    }

    // -----------------------------------------------------------------------------------
    // payCosts
    // -----------------------------------------------------------------------------------
    public void payCosts(Player player)
    {
        if (checkRequirements(player))
        {
            player.UCE_removeItems(removeItems, true);

            player.gold -= goldCost;
            player.coins -= coinCost;

            player.mana -= manaCost;

            if (player.health > healthCost)
                player.health -= healthCost;
            else
                player.health = 1;

#if _FHIZHONORSHOP
            player.UCE_PayHonorCurrencyCost(honorCurrencyCost);
#endif
        }
    }

    // -----------------------------------------------------------------------------------
    // grantRewards
    // -----------------------------------------------------------------------------------
    public void grantRewards(Player player)
    {
        if (checkRequirements(player))
        {
            player.experience += Random.Range(expRewardMin, expRewardMax);
            player.skillExperience += Random.Range(skillExpRewardMin, skillExpRewardMax);
        }
    }

    // -----------------------------------------------------------------------------------
}
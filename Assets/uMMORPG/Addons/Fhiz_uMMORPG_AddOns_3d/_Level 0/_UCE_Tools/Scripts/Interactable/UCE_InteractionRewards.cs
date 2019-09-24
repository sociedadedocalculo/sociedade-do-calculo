// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using UnityEngine;

// =======================================================================================
// ACCESS REWARDS CLASS
// =======================================================================================
[System.Serializable]
public partial class UCE_InteractionRewards
{
    [Header("[-=-=-=- UCE INTERACTION REWARDS -=-=-=-]")]
    [Tooltip("[Optional] Items awarded after sucessful interaction")]
    public UCE_ItemReward[] items;

    [Tooltip("[Optional] Minimum Experience awarded after sucessful interaction")]
    public int minExperience;

    [Tooltip("[Optional] Maximum Experience awarded after sucessful interaction")]
    public int maxExperience;

    [Tooltip("[Optional] Minimum SkillExperience awarded after sucessful interaction")]
    public int minSkillExperience;

    [Tooltip("[Optional] Maximum SkillExperience awarded after sucessful interaction")]
    public int maxSkillExperience;

    [Tooltip("[Optional] Minimum Experience awarded after sucessful interaction")]
    public int minGold;

    [Tooltip("[Optional] Maximum Experience awarded after sucessful interaction")]
    public int maxGold;

    [Tooltip("[Optional] Minimum Experience awarded after sucessful interaction")]
    public int minCoins;

    [Tooltip("[Optional] Maximum Experience awarded after sucessful interaction")]
    public int maxCoins;

#if _FHIZHONORSHOP
    [Tooltip("[Optional] Honor Currency rewarded")]
    public UCE_HonorShopCurrencyCost[] honorCurrencyReward;
#endif

#if _FHIZTRAVEL
    public UCE_Unlockroute[] unlockTravelroutes;
#endif

    public string labelGainGold = "You got gold: ";
    public string labelGainCoins = "You got coins: ";
    public string labelGainExperience = "You got experience: ";
    public string labelGainSkillExperience = "You got skill experience: ";
    public string labelGainItem = "You got: ";

    // -----------------------------------------------------------------------------------
    // gainRewards
    // -----------------------------------------------------------------------------------
    public void gainRewards(Player player)
    {
        int g = UnityEngine.Random.Range(minGold, maxGold);
        int c = UnityEngine.Random.Range(minCoins, maxCoins);
        int e = UnityEngine.Random.Range(minExperience, maxExperience);
        int s = UnityEngine.Random.Range(minSkillExperience, maxSkillExperience);

        if (g > 0)
        {
            player.Setgold(player.Getgold() + g);
            player.UCE_TargetAddMessage(labelGainGold + g.ToString());
        }

        if (c > 0)
        {
            player.coins += c;
            player.UCE_TargetAddMessage(labelGainCoins + c.ToString());
        }

        if (e > 0)
        {
            player.experience += e;
            player.UCE_TargetAddMessage(labelGainExperience + e.ToString());
        }

        if (s > 0)
        {
            player.skillExperience += s;
            player.UCE_TargetAddMessage(labelGainSkillExperience + s.ToString());
        }

        // -- reward honor currency
#if _FHIZHONORSHOP
        foreach (UCE_HonorShopCurrencyCost currency in honorCurrencyReward)
            player.UCE_AddHonorCurrency(currency.honorCurrency, currency.amount);
#endif

		// -- unlock travelroutes
#if _FHIZTRAVEL
        foreach (UCE_Unlockroute route in unlockTravelroutes)
    		player.UCE_UnlockTravelroute(route);
#endif

        // -- reward items
        foreach (UCE_ItemReward item in items)
        {
            if (UnityEngine.Random.value <= item.probability)
            {
                if (player.InventoryCanAdd(new Item(item.item), item.amount))
                {
                    player.InventoryAdd(new Item(item.item), item.amount);
                    player.UCE_TargetAddMessage(labelGainItem + item.item.name);
                }
            }
        }
    }

    // -----------------------------------------------------------------------------------
}

// =======================================================================================
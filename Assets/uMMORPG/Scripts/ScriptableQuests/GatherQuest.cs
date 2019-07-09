// a simple gather quest example
using UnityEngine;
using System.Text;

[CreateAssetMenu(menuName="uMMORPG Quest/Gather Quest", order=999)]
public class GatherQuest : ScriptableQuest
{
    [Header("Fulfillment")]
    public ScriptableItem gatherItem;
    public int gatherAmount;

    // fulfillment /////////////////////////////////////////////////////////////
    public override bool IsFulfilled(Player player, Quest quest)
    {
        return gatherItem != null &&
               player.InventoryCount(new Item(gatherItem)) >= gatherAmount;
    }

    public override void OnCompleted(Player player, Quest quest)
    {
        // remove gathered items from player's inventory
        if (gatherItem != null)
            player.InventoryRemove(new Item(gatherItem), gatherAmount);
    }

    // tooltip /////////////////////////////////////////////////////////////////
    public override string ToolTip(Player player, Quest quest)
    {
        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(base.ToolTip(player, quest));
        tip.Replace("{GATHERAMOUNT}", gatherAmount.ToString());
        if (gatherItem != null)
        {
            int gathered = player.InventoryCount(new Item(gatherItem));
            tip.Replace("{GATHERITEM}", gatherItem.name);
            tip.Replace("{GATHERED}", Mathf.Min(gathered, gatherAmount).ToString());
        }
        return tip.ToString();
    }
}

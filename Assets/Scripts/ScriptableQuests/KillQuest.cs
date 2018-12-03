// a simple kill quest example.
// inherit from KillQuest and overwrite OnKilled for more advanced goals like
// 'kill a player 10 levels above you' or 'kill a pet in a guild war' etc.
using UnityEngine;
using System.Text;

[CreateAssetMenu(menuName="uMMORPG Quest/Kill Quest", order=999)]
public class KillQuest : ScriptableQuest
{
    [Header("Fulfillment")]
    public Monster killTarget;
    public int killAmount;

    // events //////////////////////////////////////////////////////////////////
    public override void OnKilled(Player player, int questIndex, Entity victim)
    {
        // not done yet, and same name as prefab? (hence same monster?)
        Quest quest = player.quests[questIndex];
        if (quest.field0 < killAmount && victim.name == killTarget.name)
        {
            // increase int field in quest (up to 'amount')
            ++quest.field0;
            player.quests[questIndex] = quest;
        }
    }

    // fulfillment /////////////////////////////////////////////////////////////
    public override bool IsFulfilled(Player player, Quest quest)
    {
        return quest.field0 >= killAmount;
    }

    // tooltip /////////////////////////////////////////////////////////////////
    public override string ToolTip(Player player, Quest quest)
    {
        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(base.ToolTip(player, quest));
        tip.Replace("{KILLTARGET}", killTarget != null ? killTarget.name : "");
        tip.Replace("{KILLAMOUNT}", killAmount.ToString());
        tip.Replace("{KILLED}", quest.field0.ToString());
        return tip.ToString();
    }
}

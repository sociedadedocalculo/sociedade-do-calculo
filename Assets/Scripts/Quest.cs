// The Quest struct only contains the dynamic quest properties, so that the
// static properties can be read from the scriptable object. The benefits are
// low bandwidth and easy Player database saving (saves always refer to the
// scriptable quest, so we can change that any time).
//
// Quests have to be structs in order to work with SyncLists.
using System;
using System.Text;
using UnityEngine.Networking;

[Serializable]
public partial struct Quest
{
    // hashcode used to reference the real ScriptableQuest (can't link to data
    // directly because synclist only supports simple types). and syncing a
    // string's hashcode instead of the string takes WAY less bandwidth.
    public int hash;

    // dynamic stats
    public int killed;
    public bool completed; // after finishing it at the npc and getting rewards

    // constructors
    public Quest(ScriptableQuest data)
    {
        hash = data.name.GetStableHashCode();
        killed = 0;
        completed = false;
    }

    // wrappers for easier access
    public ScriptableQuest data { get { return ScriptableQuest.dict[hash]; } }
    public string name { get { return data.name; } }
    public int requiredLevel { get { return data.requiredLevel; } }
    public string predecessor { get { return data.predecessor != null ? data.predecessor.name : ""; } }
    public long rewardGold { get { return data.rewardGold; } }
    public long rewardExperience { get { return data.rewardExperience; } }
    public ScriptableItem rewardItem { get { return data.rewardItem; } }
    public Monster killTarget { get { return data.killTarget; } }
    public int killAmount { get { return data.killAmount; } }
    public ScriptableItem gatherItem { get { return data.gatherItem; } }
    public int gatherAmount { get { return data.gatherAmount; } }

    // fill in all variables into the tooltip
    // this saves us lots of ugly string concatenation code. we can't do it in
    // ScriptableQuest because some variables can only be replaced here, hence we
    // would end up with some variables not replaced in the string when calling
    // Tooltip() from the data.
    // -> note: each tooltip can have any variables, or none if needed
    // -> example usage:
    /*
    <b>{NAME}</b>
    Description here...

    Tasks:
    * Kill {KILLTARGET}: {KILLED}/{KILLAMOUNT}
    * Gather {GATHERITEM}: {GATHERED}/{GATHERAMOUNT}

    Rewards:
    * {REWARDGOLD} Gold
    * {REWARDEXPERIENCE} Experience
    * {REWARDITEM}

    {STATUS}
    */
    public string ToolTip(int gathered = 0)
    {
        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(data.toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{KILLTARGET}", killTarget != null ? killTarget.name : "");
        tip.Replace("{KILLAMOUNT}", killAmount.ToString());
        tip.Replace("{GATHERITEM}", gatherItem != null ? gatherItem.name : "");
        tip.Replace("{GATHERAMOUNT}", gatherAmount.ToString());
        tip.Replace("{REWARDGOLD}", rewardGold.ToString());
        tip.Replace("{REWARDEXPERIENCE}", rewardExperience.ToString());
        tip.Replace("{REWARDITEM}", rewardItem != null ? rewardItem.name : "");
        tip.Replace("{KILLED}", killed.ToString());
        tip.Replace("{GATHERED}", gathered.ToString());
        tip.Replace("{STATUS}", IsFulfilled(gathered) ? "<i>Concluído!</i>" : "");

        // addon system hooks
        Utils.InvokeMany(typeof(Quest), this, "ToolTip_", tip);

        return tip.ToString();
    }

    // a quest is fulfilled if all requirements were met and it can be completed
    // at the npc
    public bool IsFulfilled(int gathered)
    {
        return killed >= killAmount && gathered >= gatherAmount;
    }
}

public class SyncListQuest : SyncListStruct<Quest> { }

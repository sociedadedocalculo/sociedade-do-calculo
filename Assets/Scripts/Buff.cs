// Buffs are like Skills, but for the Buffs list.
using System;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Buff
{
    // hashcode used to reference the real ScriptableSkill (can't link to data
    // directly because synclist only supports simple types). and syncing a
    // string's hashcode instead of the string takes WAY less bandwidth.
    public int hash;

    // dynamic stats (cooldowns etc.)
    public int level;
    public float buffTimeEnd; // server time

    // constructors
    public Buff(BuffSkill data, int level)
    {
        hash = data.name.GetStableHashCode();
        this.level = level;
        buffTimeEnd = NetworkTime.time + data.buffTime.Get(level); // start buff immediately
    }

    // wrappers for easier access
    public BuffSkill data { get { return (BuffSkill)ScriptableSkill.dict[hash]; } }
    public string name { get { return data.name; } }
    public Sprite image { get { return data.image; } }
    public float buffTime { get { return data.buffTime.Get(level); } }
    public int buffsHealthMax { get { return data.buffsHealthMax.Get(level); } }
    public int buffsManaMax { get { return data.buffsManaMax.Get(level); } }
    public int buffsDamage { get { return data.buffsDamage.Get(level); } }
    public int buffsDefense { get { return data.buffsDefense.Get(level); } }
    public float buffsBlockChance { get { return data.buffsBlockChance.Get(level); } }
    public float buffsCriticalChance { get { return data.buffsCriticalChance.Get(level); } }
    public float buffsHealthPercentPerSecond { get { return data.buffsHealthPercentPerSecond.Get(level); } }
    public float buffsManaPercentPerSecond { get { return data.buffsManaPercentPerSecond.Get(level); } }
    public int maxLevel { get { return data.maxLevel; } }

    // tooltip - runtime part
    public string ToolTip()
    {
        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(data.ToolTip(level));

        // addon system hooks
        Utils.InvokeMany(typeof(Buff), this, "ToolTip_", tip);

        return tip.ToString();
    }

    public float BuffTimeRemaining()
    {
        // how much time remaining until the buff ends? (using server time)
        return NetworkTime.time >= buffTimeEnd ? 0 : buffTimeEnd - NetworkTime.time;
    }
}

public class SyncListBuff : SyncListStruct<Buff> { }

// Base type for buff skill templates.
// => there may be target buffs, targetless buffs, aoe buffs, etc.
//    but they all have to fit into the buffs list
using System.Text;
using UnityEngine;
using Mirror;

public abstract class BuffSkill : ScriptableSkill
{
    public LevelBasedFloat buffTime = new LevelBasedFloat { baseValue = 60 };

    public LevelBasedInt buffsHealthMax;
    public LevelBasedInt buffsManaMax;
    public LevelBasedInt buffsDamage;
    public LevelBasedInt buffsDefense;
    public LevelBasedFloat buffsBlockChance; // range [0,1]
    public LevelBasedFloat buffsCriticalChance; // range [0,1]
    public LevelBasedFloat buffsHealthPercentPerSecond; // 0.1=10%; can be negative too
    public LevelBasedFloat buffsManaPercentPerSecond; // 0.1=10%; can be negative too

    public BuffSkillEffect effect;

    // helper function to spawn the skill effect on someone
    // (used by all the buff implementations and to load them after saving)
    public void SpawnEffect(Entity caster, Entity spawnTarget)
    {
        if (effect != null)
        {
            GameObject go = Instantiate(effect.gameObject, spawnTarget.transform.position, Quaternion.identity);
            go.GetComponent<BuffSkillEffect>().caster = caster;
            go.GetComponent<BuffSkillEffect>().target = spawnTarget;
            go.GetComponent<BuffSkillEffect>().buffName = name;
            NetworkServer.Spawn(go);
        }
    }

    // tooltip
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{BUFFTIME}", Utils.PrettySeconds(buffTime.Get(skillLevel)));
        tip.Replace("{BUFFSHEALTHMAX}", buffsHealthMax.Get(skillLevel).ToString());
        tip.Replace("{BUFFSMANAMAX}", buffsManaMax.Get(skillLevel).ToString());
        tip.Replace("{BUFFSDAMAGE}", buffsDamage.Get(skillLevel).ToString());
        tip.Replace("{BUFFSDEFENSE}", buffsDefense.Get(skillLevel).ToString());
        tip.Replace("{BUFFSBLOCKCHANCE}", Mathf.RoundToInt(buffsBlockChance.Get(skillLevel) * 100).ToString());
        tip.Replace("{BUFFSCRITICALCHANCE}", Mathf.RoundToInt(buffsCriticalChance.Get(skillLevel) * 100).ToString());
        tip.Replace("{BUFFSHEALTHPERCENTPERSECOND}", Mathf.RoundToInt(buffsHealthPercentPerSecond.Get(skillLevel) * 100).ToString());
        tip.Replace("{BUFFSMANAPERCENTPERSECOND}", Mathf.RoundToInt(buffsManaPercentPerSecond.Get(skillLevel) * 100).ToString());
        return tip.ToString();
    }
}

// Base type for bonus skill templates.
// => can be used for passive skills, buffs, etc.
using System.Text;
using UnityEngine;
using Mirror;

public abstract class BonusSkill : ScriptableSkill
{
    public LinearInt bonusHealthMax;
    public LinearInt bonusManaMax;
    public LinearInt bonusDamage;
    public LinearInt bonusDefense;
    public LinearFloat bonusBlockChance; // range [0,1]
    public LinearFloat bonusCriticalChance; // range [0,1]
    public LinearFloat bonusHealthPercentPerSecond; // 0.1=10%; can be negative too
    public LinearFloat bonusManaPercentPerSecond; // 0.1=10%; can be negative too
    public LinearFloat bonusSpeed; // can be negative too

    // tooltip
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{BONUSHEALTHMAX}", bonusHealthMax.Get(skillLevel).ToString());
        tip.Replace("{BONUSMANAMAX}", bonusManaMax.Get(skillLevel).ToString());
        tip.Replace("{BONUSDAMAGE}", bonusDamage.Get(skillLevel).ToString());
        tip.Replace("{BONUSDEFENSE}", bonusDefense.Get(skillLevel).ToString());
        tip.Replace("{BONUSBLOCKCHANCE}", Mathf.RoundToInt(bonusBlockChance.Get(skillLevel) * 100).ToString());
        tip.Replace("{BONUSCRITICALCHANCE}", Mathf.RoundToInt(bonusCriticalChance.Get(skillLevel) * 100).ToString());
        tip.Replace("{BONUSHEALTHPERCENTPERSECOND}", Mathf.RoundToInt(bonusHealthPercentPerSecond.Get(skillLevel) * 100).ToString());
        tip.Replace("{BONUSMANAPERCENTPERSECOND}", Mathf.RoundToInt(bonusManaPercentPerSecond.Get(skillLevel) * 100).ToString());
        tip.Replace("{BONUSSPEED}", bonusSpeed.Get(skillLevel).ToString("F2"));
        return tip.ToString();
    }
}

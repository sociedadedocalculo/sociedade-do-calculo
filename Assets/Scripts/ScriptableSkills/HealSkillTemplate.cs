// Base type for heal skill templates.
// => there may be target heal, targetless heal, aoe heal, etc.
using System.Text;
using UnityEngine;
using Mirror;

public abstract class HealSkillTemplate : ScriptableSkill
{
    public LevelBasedInt healsHealth;
    public LevelBasedInt healsMana;
    public OneTimeTargetSkillEffect effect;

    // helper function to spawn the skill effect on someone
    // (used by all the buff implementations and to load them after saving)
    public void SpawnEffect(Entity caster, Entity spawnTarget)
    {
        if (effect != null)
        {
            GameObject go = Instantiate(effect.gameObject, spawnTarget.transform.position, Quaternion.identity);
            go.GetComponent<OneTimeTargetSkillEffect>().caster = caster;
            go.GetComponent<OneTimeTargetSkillEffect>().target = spawnTarget;
            NetworkServer.Spawn(go);
        }
    }

    // tooltip /////////////////////////////////////////////////////////////////
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{HEALSHEALTH}", healsHealth.Get(skillLevel).ToString());
        tip.Replace("{HEALSMANA}", healsMana.Get(skillLevel).ToString());
        return tip.ToString();
    }
}

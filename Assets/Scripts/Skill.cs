// The Skill struct only contains the dynamic skill properties, so that the
// static properties can be read from the scriptable object. The benefits are
// low bandwidth and easy Player database saving (saves always refer to the
// scriptable skill, so we can change that any time).
//
// Skills have to be structs in order to work with SyncLists.
//
// We implemented the cooldowns in a non-traditional way. Instead of counting
// and increasing the elapsed time since the last cast, we simply set the
// 'end' Time variable to NetworkTime.time + cooldown after casting each time.
// This way we don't need an extra Update method that increases the elapsed time
// for each skill all the time.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Skill
{
    // hashcode used to reference the real ItemTemplate (can't link to template
    // directly because synclist only supports simple types). and syncing a
    // string's hashcode instead of the string takes WAY less bandwidth.
    public int hash;

    // dynamic stats (cooldowns etc.)
    public int level; // 0 if not learned, >0 if learned
    public double castTimeEnd; // server time. double for long term precision.
    public double cooldownEnd; // server time. double for long term precision.

    // constructors
    public Skill(ScriptableSkill data)
    {
        hash = data.name.GetStableHashCode();

        // learned only if learned by default
        level = data.learnDefault ? 1 : 0;

        // ready immediately
        castTimeEnd = cooldownEnd = NetworkTime.time;
    }

    // wrappers for easier access
    public ScriptableSkill data
    {
        get
        {
            // show a useful error message if the key can't be found
            // note: ScriptableSkill.OnValidate 'is in resource folder' check
            //       causes Unity SendMessage warnings and false positives.
            //       this solution is a lot better.
            if (!ScriptableSkill.dict.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableSkill with hash=" + hash + ". Make sure that all ScriptableSkills are in the Resources folder so they are loaded properly.");
            return ScriptableSkill.dict[hash];
        }
    }
    public string name { get { return data.name; } }
    public float castTime { get { return data.castTime.Get(level); } }
    public float cooldown { get { return data.cooldown.Get(level); } }
    public float castRange { get { return data.castRange.Get(level); } }
    public int manaCosts { get { return data.manaCosts.Get(level); } }
    public bool followupDefaultAttack { get { return data.followupDefaultAttack; } }
    public Sprite image { get { return data.image; } }
    public bool learnDefault { get { return data.learnDefault; } }
    public bool showCastBar { get { return data.showCastBar; } }
    public bool cancelCastIfTargetDied { get { return data.cancelCastIfTargetDied; } }
    public int maxLevel { get { return data.maxLevel; } }
    public ScriptableSkill predecessor { get { return data.predecessor; } }
    public int predecessorLevel { get { return data.predecessorLevel; } }
    public bool requiresWeapon { get { return data.requiresWeapon; } }
    public int upgradeRequiredLevel { get { return data.requiredLevel.Get(level+1); } }
    public long upgradeRequiredSkillExperience { get { return data.requiredSkillExperience.Get(level+1); } }

    // events
    public bool CheckSelf(Entity caster, bool checkSkillReady=true)
    {
        return (!checkSkillReady || IsReady()) &&
               data.CheckSelf(caster, level);
    }
    public bool CheckTarget(Entity caster) { return data.CheckTarget(caster); }
    public bool CheckDistance(Entity caster, out Vector3 destination) { return data.CheckDistance(caster, level, out destination); }
    public void Apply(Entity caster) { data.Apply(caster, level); }

    // tooltip - dynamic part
    public string ToolTip(bool showRequirements = false)
    {
        // unlearned skills (level 0) should still show tooltip for level 1
        int showLevel = Mathf.Max(level, 1);

        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(data.ToolTip(showLevel, showRequirements));

        // addon system hooks
        Utils.InvokeMany(typeof(Skill), this, "ToolTip_", tip);

        // only show upgrade if learned and not max level yet
        if (0 < level && level < maxLevel)
        {
            tip.Append("\n<i>Upgrade:</i>\n" +
                       "<i>  Required Level: " + upgradeRequiredLevel + "</i>\n" +
                       "<i>  Required Skill Exp.: " + upgradeRequiredSkillExperience + "</i>\n");
        }

        return tip.ToString();
    }

    public float CastTimeRemaining()
    {
        // how much time remaining until the casttime ends? (using server time)
        return NetworkTime.time >= castTimeEnd ? 0 : (float)(castTimeEnd - NetworkTime.time);
    }

    public bool IsCasting()
    {
        // we are casting a skill if the casttime remaining is > 0
        return CastTimeRemaining() > 0;
    }

    public float CooldownRemaining()
    {
        // how much time remaining until the cooldown ends? (using server time)
        return NetworkTime.time >= cooldownEnd ? 0 : (float)(cooldownEnd - NetworkTime.time);
    }

    public bool IsReady()
    {
        return CooldownRemaining() == 0;
    }
}

public class SyncListSkill : SyncListSTRUCT<Skill> {}

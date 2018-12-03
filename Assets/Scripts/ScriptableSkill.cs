// Saves the skill info in a ScriptableObject that can be used ingame by
// referencing it from a MonoBehaviour. It only stores an skill's static data.
//
// We also add each one to a dictionary automatically, so that all of them can
// be found by name without having to put them all in a database. Note that we
// have to put them all into the Resources folder and use Resources.LoadAll to
// load them. This is important because some skills may not be referenced by any
// entity ingame (e.g. after a special event). But all skills should still be
// loadable from the database, even if they are not referenced by anyone
// anymore. So we have to use Resources.Load. (before we added them to the dict
// in OnEnable, but that's only called for those that are referenced in the
// game. All others will be ignored by Unity.)
//
// Entity animation controllers will need one bool parameter for each skill name
// and they can use the same animation for different skill templates by using
// multiple transitions. (this is way easier than keeping track of a skillindex)
//
// A Skill can be created by right clicking the Resources folder and selecting
// Create -> uMMORPG Skill. Existing skills can be found in the Resources folder
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public abstract partial class ScriptableSkill : ScriptableObject
{
    [Header("Info")]
    public bool followupDefaultAttack;
    [SerializeField, TextArea(1, 30)] protected string toolTip; // not public, use ToolTip()
    public Sprite image;
    public bool learnDefault; // normal attack etc.
    public bool showCastBar;
    public bool cancelCastIfTargetDied; // direct hit may want to cancel if target died. buffs doesn't care. etc.

    [Header("Requirements")]
    public ScriptableSkill predecessor; // this skill has to be learned first
    public int predecessorLevel = 1; // level of predecessor skill that is required
    public bool requiresWeapon; // some might need empty-handed casting
    public LevelBasedInt requiredLevel; // required player level
    public LevelBasedLong requiredSkillExperience;

    [Header("Properties")]
    public int maxLevel = 1;
    public LevelBasedInt manaCosts;
    public LevelBasedFloat castTime;
    public LevelBasedFloat cooldown;
    public LevelBasedFloat castRange;

    [Header("Sound")]
    public AudioClip castSound;

    // the skill casting process ///////////////////////////////////////////////
    // 1. self check: alive, enough mana, cooldown ready etc.?
    // (most skills can only be cast while alive. some maybe while dead or only
    //  if we have ammo, etc.)
    public virtual bool CheckSelf(Entity caster, int skillLevel)
    {
        // has a weapon (important for projectiles etc.), no cooldown, hp, mp?
        return caster.health > 0 &&
               caster.mana >= manaCosts.Get(skillLevel) &&
               (!requiresWeapon || caster.HasCastWeapon());
    }

    // 2. target check: can we cast this skill 'here' or on this 'target'?
    // => e.g. sword hit checks if target can be attacked
    //         skill shot checks if the position under the mouse is valid etc.
    //         buff checks if it's a friendly player, etc.
    // ===> IMPORTANT: this function HAS TO correct the target if necessary,
    //      e.g. for a buff that is cast on 'self' even though we target a NPC
    //      while casting it
    public abstract bool CheckTarget(Entity caster);

    // 3. distance check: do we need to walk somewhere to cast it?
    //    e.g. on a monster that's far away
    //    => returns 'true' if distance is fine, 'false' if we need to move
    // (has corrected target already)
    public abstract bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination);

    // 4. apply skill: deal damage, heal, launch projectiles, etc.
    // (has corrected target already)
    public abstract void Apply(Entity caster, int skillLevel);

    // events for client sided effects /////////////////////////////////////////
    // [Client]
    public virtual void OnCastStarted(Entity caster)
    {
        if (caster.audioSource != null && castSound != null)
            caster.audioSource.PlayOneShot(castSound);
    }

    // [Client]
    public virtual void OnCastFinished(Entity caster) {}

    // OnCastCanceled doesn't seem worth the Rpc bandwidth, since skill effects
    // can check if caster.currentSkill == -1

    // tooltip /////////////////////////////////////////////////////////////////
    // fill in all variables into the tooltip
    // this saves us lots of ugly string concatenation code.
    // (dynamic ones are filled in Skill.cs)
    // -> note: each tooltip can have any variables, or none if needed
    // -> example usage:
    /*
    <b>{NAME}</b>
    Description here...

    Damage: {DAMAGE}
    Cast Time: {CASTTIME}
    Cooldown: {COOLDOWN}
    Cast Range: {CASTRANGE}
    AoE Radius: {AOERADIUS}
    Mana Costs: {MANACOSTS}
    */
    public virtual string ToolTip(int level, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{LEVEL}", level.ToString());
        tip.Replace("{CASTTIME}", Utils.PrettySeconds(castTime.Get(level)));
        tip.Replace("{COOLDOWN}", Utils.PrettySeconds(cooldown.Get(level)));
        tip.Replace("{CASTRANGE}", castRange.Get(level).ToString());
        tip.Replace("{MANACOSTS}", manaCosts.Get(level).ToString());

        // only show requirements if necessary
        if (showRequirements)
        {
            tip.Append("\n<b><i>Required Level: " + requiredLevel.Get(1) + "</i></b>\n" +
                       "<b><i>Required Skill Exp.: " + requiredSkillExperience.Get(1) + "</i></b>\n");
            if (predecessor != null)
                tip.Append("<b><i>Required Skill: " + predecessor.name + " Lv. " + predecessorLevel + " </i></b>\n");
        }

        return tip.ToString();
    }

    // caching /////////////////////////////////////////////////////////////////
    // we can only use Resources.Load in the main thread. we can't use it when
    // declaring static variables. so we have to use it as soon as 'dict' is
    // accessed for the first time from the main thread.
    // -> we save the hash so the dynamic item part doesn't have to contain and
    //    sync the whole name over the network
    static Dictionary<int, ScriptableSkill> cache;
    public static Dictionary<int, ScriptableSkill> dict
    {
        get
        {
            // load if not loaded yet
            return cache ?? (cache = Resources.LoadAll<ScriptableSkill>("").ToDictionary(
                skill => skill.name.GetStableHashCode(), skill => skill)
            );
        }
    }
}

// Saves the quest info in a ScriptableObject that can be used ingame by
// referencing it from a MonoBehaviour. It only stores an quest's static data.
//
// We also add each one to a dictionary automatically, so that all of them can
// be found by name without having to put them all in a database. Note that we
// have to put them all into the Resources folder and use Resources.LoadAll to
// load them. This is important because some quests may not be referenced by any
// entity ingame (e.g. after a special event). But all quests should still be
// loadable from the database, even if they are not referenced by anyone
// anymore. So we have to use Resources.Load. (before we added them to the dict
// in OnEnable, but that's only called for those that are referenced in the
// game. All others will be ignored be Unity.)
//
// A Quest can be created by right clicking the Resources folder and selecting
// Create -> uMMORPG Quest. Existing quests can be found in the Resources folder
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class ScriptableQuest : ScriptableObject
{
    [Header("General")]
    [SerializeField, TextArea(1, 30)] protected string toolTip; // not public, use ToolTip()

    [Header("Requirements")]
    public int requiredLevel; // player.level
    public ScriptableQuest predecessor; // this quest has to be completed first

    [Header("Rewards")]
    public long rewardGold;
    public long rewardExperience;
    public ScriptableItem rewardItem;

    // events to hook into /////////////////////////////////////////////////////
    public virtual void OnKilled(Player player, int questIndex, Entity victim) {}
    public virtual void OnLocation(Player player, int questIndex, Collider location) {}

    // fulfillment /////////////////////////////////////////////////////////////
    // we pass the Quest instead of an index for ease of use and because we are
    // read-only here anyway
    public abstract bool IsFulfilled(Player player, Quest quest);

    // OnComplete is called when the quest is completed at the npc.
    // -> can be used to remove quest items from the inventory, etc.
    public virtual void OnCompleted(Player player, Quest quest) {}

    // tooltip /////////////////////////////////////////////////////////////////
    // fill in all variables into the tooltip
    // this saves us lots of ugly string concatenation code.
    // (dynamic ones are filled in Quest.cs)
    // -> note: each tooltip can have any variables, or none if needed
    // -> pass dynamic Quest part so we can interpret intField0 etc. as whatever
    //    we are tracking. this way the inheriting ScriptableQuest can show it
    //    as a number, or as Yes/No or as a bitmask checklist, etc.
    // -> pass Player so we can count inventory items for gather quests, etc.
    // -> example usage:
    /*
    <b>{NAME}</b>
    Description here...

    Tasks:
    * Gather Something.

    Rewards:
    * {REWARDGOLD} Gold
    * {REWARDEXPERIENCE} Experience
    * {REWARDITEM}

    {STATUS}
    */
    public virtual string ToolTip(Player player, Quest quest)
    {
        StringBuilder tip = new StringBuilder(toolTip);
        tip.Replace("{NAME}", name);
        tip.Replace("{REWARDGOLD}", rewardGold.ToString());
        tip.Replace("{REWARDEXPERIENCE}", rewardExperience.ToString());
        tip.Replace("{REWARDITEM}", rewardItem != null ? rewardItem.name : "");
        return tip.ToString();
    }

    // caching /////////////////////////////////////////////////////////////////
    // we can only use Resources.Load in the main thread. we can't use it when
    // declaring static variables. so we have to use it as soon as 'dict' is
    // accessed for the first time from the main thread.
    // -> we save the hash so the dynamic item part doesn't have to contain and
    //    sync the whole name over the network
    static Dictionary<int, ScriptableQuest> cache;
    public static Dictionary<int, ScriptableQuest> dict
    {
        get
        {
            // load if not loaded yet
            return cache ?? (cache = Resources.LoadAll<ScriptableQuest>("").ToDictionary(
                quest => quest.name.GetStableHashCode(), quest => quest)
            );
        }
    }
}

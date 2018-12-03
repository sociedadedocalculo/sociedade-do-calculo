// only usable items need minLevel and usage functions
using System.Text;
using UnityEngine;

public abstract class UsableItem : ScriptableItem
{
    [Header("Usage")]
    public int minLevel; // level required to use the item

    // item cooldowns need to be global so we can't use the potion in slot 0 and
    // then the one in slot 1 immediately after.
    // -> cooldown buffs are the common solution in MMOs and they allow for
    //    heal-over-time if needed too
    // -> should use 'Health Potion Cooldown' buff for all health potions, etc.
    [Header("Cooldown Buff")]
    public TargetBuffSkill cooldownBuff;

    // usage ///////////////////////////////////////////////////////////////////
    // [Server] and [Client] CanUse check for UI, Commands, etc.
    public virtual bool CanUse(Player player, int inventoryIndex)
    {
        // check level etc. and make sure that cooldown buff elapsed (if any)
        return player.level >= minLevel &&
               (cooldownBuff == null ||
                player.GetBuffIndexByName(cooldownBuff.name) == -1);
    }

    // [Server] Use logic: make sure to call base.Use() in overrides too.
    public virtual void Use(Player player, int inventoryIndex)
    {
        // start cooldown buff (if any)
        if (cooldownBuff != null)
        {
            // set target to player before applying buff
            Entity oldTarget = player.target;
            player.target = player;

            // apply the buff with skill level 1
            cooldownBuff.Apply(player, 1);

            // restore target
            player.target = oldTarget;
        }
    }

    // [Client] OnUse Rpc callback for effects, sounds, etc.
    // -> can't pass slotIndex because .Use might clear it before getting here already
    public virtual void OnUsed(Player player) {}

    // tooltip /////////////////////////////////////////////////////////////////
    public override string ToolTip()
    {
        StringBuilder tip = new StringBuilder(base.ToolTip());
        tip.Replace("{MINLEVEL}", minLevel.ToString());
        return tip.ToString();
    }
}

// only usable items need minLevel and usage functions
using System.Text;
using UnityEngine;

public abstract class UsableItem : ScriptableItem
{
    [Header("Usage")]
    public int minLevel; // level required to use the item

    // usage ///////////////////////////////////////////////////////////////////
    // [Server] and [Client] CanUse check for UI, Commands, etc.
    public virtual bool CanUse(Player player, int inventoryIndex)
    {
        return player.level >= minLevel;
    }

    // [Server] Use logic
    public abstract void Use(Player player, int inventoryIndex);

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

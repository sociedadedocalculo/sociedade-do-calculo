// summonable entity types that are bound to a player (pet, mount, ...)
using UnityEngine;
using Mirror;

public abstract class Summonable : Entity
{
    // 'Player' can't be SyncVar so we use [SyncVar] GameObject and wrap it
    [SyncVar] GameObject _owner;
    public Player owner
    {
        get { return _owner != null  ? _owner.GetComponent<Player>() : null; }
        set { _owner = value != null ? value.gameObject : null; }
    }

    // sync with owner's item //////////////////////////////////////////////////
    protected virtual ItemSlot SyncStateToItemSlot(ItemSlot slot)
    {
        slot.item.summonedHealth = health;
        slot.item.summonedLevel = level;

        // remove item if died?
        if (((SummonableItem)slot.item.data).removeItemIfDied && health == 0)
            --slot.amount;

        return slot;
    }

    // to save computations we don't sync to it all the time, it's enough to
    // sync in:
    // * OnDestroy when unsummoning the pet
    // * On experience gain so that level ups and exp are saved properly
    // * OnDeath so that people can't cheat around reviving pets
    // => after a server crash the health/mana might not be exact, but that's a
    //    good price to pay to save computations in each Update tick
    [Server]
    public void SyncToOwnerItem()
    {
        // owner might be null if server shuts down and owner was destroyed before
        if (owner != null)
        {
            // find the item (amount might be 0 already if a mount died, etc.)
            int index = owner.inventory.FindIndex(slot => slot.amount > 0 && slot.item.summoned == gameObject);
            if (index != -1)
                owner.inventory[index] = SyncStateToItemSlot(owner.inventory[index]);
        }
    }
}

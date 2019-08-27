// Inventories need a slot type to hold Item + Amount. This is better than
// storing .amount in 'Item' because then we can use Item.Equals properly
// any workarounds to ignore the .amount.
//
// Note: always check .amount > 0 before accessing .item.
//       set .amount=0 to clear it.
using System;
using System.Text;
using UnityEngine;
using Mirror;
using UnityEngine.Networking;

[Serializable]
public partial struct ItemSlot
{
    public Item item;
    public int amount;

    // constructors
    public ItemSlot(Item item, int amount=1)
    {
        this.item = item;
        this.amount = amount;
    }

    // helper functions to increase/decrease amount more easily
    // -> returns the amount that we were able to increase/decrease by
    public int DecreaseAmount(int reduceBy)
    {
        // as many as possible
        int limit = Mathf.Clamp(reduceBy, 0, amount);
        amount -= limit;
        return limit;
    }

    public int IncreaseAmount(int increaseBy)
    {
        // as many as possible
        int limit = Mathf.Clamp(increaseBy, 0, item.maxStack - amount);
        amount += limit;
        return limit;
    }

    // tooltip
    public string ToolTip()
    {
        if (amount == 0) return "";

        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(item.ToolTip());
        tip.Replace("{AMOUNT}", amount.ToString());
        return tip.ToString();
    }
}

#pragma warning disable CS0618 // O tipo ou membro é obsoleto
public class SyncListItemSlot : SyncList<ItemSlot>
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
{
    public override bool Equals(object obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return base.ToString();
    }

#pragma warning disable CS0618 // O tipo ou membro é obsoleto
    protected override ItemSlot DeserializeItem(UnityEngine.Networking.NetworkReader reader)
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
    {
        throw new NotImplementedException();
    }

#pragma warning disable CS0618 // O tipo ou membro é obsoleto
    protected override void SerializeItem(UnityEngine.Networking.NetworkWriter writer, ItemSlot item)
#pragma warning restore CS0618 // O tipo ou membro é obsoleto
    {
        throw new NotImplementedException();
    }
}

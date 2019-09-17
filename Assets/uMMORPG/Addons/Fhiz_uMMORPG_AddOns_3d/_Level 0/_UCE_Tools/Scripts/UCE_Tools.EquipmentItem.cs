// =======================================================================================
// EQUIPMENT ITEM
// =======================================================================================
public partial class EquipmentItem
{
    // -----------------------------------------------------------------------------------
    // CanUnequip (Swapping)
    // -----------------------------------------------------------------------------------
    public bool CanUnequip(Player player, int inventoryIndex, int equipmentIndex)
    {
        MutableWrapper<bool> bValid = new MutableWrapper<bool>(true);
        Utils.InvokeMany(typeof(EquipmentItem), this, "CanUnequip_", player, inventoryIndex, equipmentIndex, bValid);
        return bValid.Value;
    }

    // -----------------------------------------------------------------------------------
    // CanUnequipClick (Clicking)
    // -----------------------------------------------------------------------------------
    public bool CanUnequipClick(Player player, EquipmentItem item)
    {
        MutableWrapper<bool> bValid = new MutableWrapper<bool>(true);
        Utils.InvokeMany(typeof(EquipmentItem), this, "CanUnequipClick_", player, item, bValid);
        return bValid.Value;
    }

    // -----------------------------------------------------------------------------------
}

// =======================================================================================
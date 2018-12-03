// The Item struct only contains the dynamic item properties, so that the static
// properties can be read from the scriptable object.
//
// Items have to be structs in order to work with SyncLists.
//
// Use .Equals to compare two items. Comparing the name is NOT enough for cases
// where dynamic stats differ. E.g. two pets with different levels shouldn't be
// merged.
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Mirror;

[Serializable]
public partial struct Item
{
    // hashcode used to reference the real ScriptableItem (can't link to data
    // directly because synclist only supports simple types). and syncing a
    // string's hashcode instead of the string takes WAY less bandwidth.
    public int hash;

    // dynamic stats (cooldowns etc. later)
    public GameObject petSummoned; // pet that's currently summoned
    public int petHealth; // stored in item while pet unsummoned
    public int petLevel; // stored in item while pet unsummoned
    public long petExperience; // stored in item while pet unsummoned

    // constructors
    public Item(ScriptableItem data)
    {
        hash = data.name.GetStableHashCode();
        petSummoned = null;
        petHealth = data is PetItem ? ((PetItem)data).petPrefab.healthMax : 0;
        petLevel = data is PetItem ? 1 : 0;
        petExperience = 0;
    }

    // wrappers for easier access
    public ScriptableItem data
    {
        get
        {
            // show a useful error message if the key can't be found
            // note: ScriptableItem.OnValidate 'is in resource folder' check
            //       causes Unity SendMessage warnings and false positives.
            //       this solution is a lot better.
            if (!ScriptableItem.dict.ContainsKey(hash))
                throw new KeyNotFoundException("There is no ScriptableItem with hash=" + hash + ". Make sure that all ScriptableItems are in the Resources folder so they are loaded properly.");
            return ScriptableItem.dict[hash];
        }
    }
    public string name { get { return data.name; } }
    public int maxStack { get { return data.maxStack; } }
    public long buyPrice { get { return data.buyPrice; } }
    public long sellPrice { get { return data.sellPrice; } }
    public long itemMallPrice { get { return data.itemMallPrice; } }
    public bool sellable { get { return data.sellable; } }
    public bool tradable { get { return data.tradable; } }
    public bool destroyable { get { return data.destroyable; } }
    public Sprite image { get { return data.image; } }

    // tooltip
    public string ToolTip()
    {
        // we use a StringBuilder so that addons can modify tooltips later too
        // ('string' itself can't be passed as a mutable object)
        StringBuilder tip = new StringBuilder(data.ToolTip());
        tip.Replace("{PETHEALTH}", petHealth.ToString());
        tip.Replace("{PETLEVEL}", petLevel.ToString());
        tip.Replace("{PETEXPERIENCE}", petExperience.ToString());

        // addon system hooks
        Utils.InvokeMany(typeof(Item), this, "ToolTip_", tip);

        return tip.ToString();
    }
}

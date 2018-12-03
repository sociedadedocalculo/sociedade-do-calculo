using System.Text;
using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Item/Pet", order=999)]
public class PetItem : UsableItem
{
    [Header("Pet")]
    public Pet petPrefab;

    // usage
    public override bool CanUse(Player player, int inventoryIndex)
    {
        // summon only if:
        //  no other is summoned yet
        //  pet not dead (dead pet item has to be revived first)
        //  player level at least pet level to avoid power leveling
        //    with someone else's high level pet
        return base.CanUse(player, inventoryIndex) &&
               petPrefab != null &&
               player.activePet == null &&
               player.inventory[inventoryIndex].item.petHealth > 0 &&
               player.inventory[inventoryIndex].item.petLevel <= player.level;
    }

    public override void Use(Player player, int inventoryIndex)
    {
        // always call base function too
        base.Use(player, inventoryIndex);

        // summon right next to the player
        ItemSlot slot = player.inventory[inventoryIndex];
        GameObject go = Instantiate(petPrefab.gameObject, player.petDestination, Quaternion.identity);
        Pet pet = go.GetComponent<Pet>();
        pet.name = petPrefab.name; // avoid "(Clone)"
        pet.owner = player;
        pet.health = slot.item.petHealth;
        pet.level = slot.item.petLevel;
        pet.experience = slot.item.petExperience;

        NetworkServer.Spawn(go);
        player.activePet = go.GetComponent<Pet>(); // set syncvar to go after spawning

        // set item summoned pet reference so we know it can't be sold etc.
        slot.item.petSummoned = go;
        player.inventory[inventoryIndex] = slot;
    }
}

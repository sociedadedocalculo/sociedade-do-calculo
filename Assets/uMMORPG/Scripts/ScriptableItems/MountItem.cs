using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Item/Mount", order=999)]
public class MountItem : SummonableItem
{
    // usage
    public override bool CanUse(Player player, int inventoryIndex)
    {
        // summonable checks if we can summon it already,
        // we just need to check if we have no active mount summoned yet
        // OR if this is the active mount, so we unsummon it
        return base.CanUse(player, inventoryIndex) &&
               (player.activeMount == null || player.activeMount.gameObject == player.inventory[inventoryIndex].item.summoned);
    }

    public override void Use(Player player, int inventoryIndex)
    {
        // always call base function too
        base.Use(player, inventoryIndex);

        // summon
        if (player.activeMount == null)
        {
            // summon at player position
            ItemSlot slot = player.inventory[inventoryIndex];
            GameObject go = Instantiate(summonPrefab.gameObject, player.transform.position, player.transform.rotation);
            Mount mount = go.GetComponent<Mount>();
            mount.name = summonPrefab.name; // avoid "(Clone)"
            mount.owner = player;
            mount.health = slot.item.summonedHealth;

            NetworkServer.Spawn(go);
            player.activeMount = go.GetComponent<Mount>(); // set syncvar to go after spawning

            // set item summoned pet reference so we know it can't be sold etc.
            slot.item.summoned = go;
            player.inventory[inventoryIndex] = slot;
        }
        // unsummon
        else
        {
            // destroy from world. item.summoned and activePet will be null.
            NetworkServer.Destroy(player.activeMount.gameObject);
        }
    }
}

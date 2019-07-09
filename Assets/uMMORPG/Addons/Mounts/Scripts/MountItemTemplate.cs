using UnityEngine;
using UnityEngine.Networking;
[CreateAssetMenu(menuName="uMMORPG Item/Mount", order=999)]
public class MountItemTemplate : UsableItemTemplate
{
	[Header("Mount Stuff")]
	public GameObject mountPrefab;
    public float speed;
	public float mountDeathAnimLength;

	public override void Use (Player player, int inventoryIndex)
	{
		this.OnUseInventoryItem_Mount (player, inventoryIndex);
	}
		
	void OnUseInventoryItem_Mount(Player player, int index)
	{
		player.mountPrefab = mountPrefab;
		player.speed = speed;
		player.mountDeathAnimLength = mountDeathAnimLength;
		try
		{
			if (player.inventory[index].item.name.StartsWith("Mount") && (player.state == "IDLE" || player.state == "MOVING") &&
				0 <= index && index < player.inventory.Count && player.inventory[index].amount > 0)
			{
				if (player.mount == null && 
					player.mountDead == false){
					player.mountPlayer();
				}else {
					player.UnmountPlayer();
				}

			}
		} catch {}
	}
}


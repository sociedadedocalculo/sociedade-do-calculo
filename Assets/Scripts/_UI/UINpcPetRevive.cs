using UnityEngine;
using UnityEngine.UI;

public partial class UINpcPetRevive : MonoBehaviour
{
    public static UINpcPetRevive singleton;
    public GameObject panel;
    public UIDragAndDropable itemSlot;
    public Text costsText;
    public Button reviveButton;
    [HideInInspector] public int itemIndex = -1;

    public UINpcPetRevive() { singleton = this; }

    void Update()
    {
        Player player = Player.localPlayer;
        if (!player) return;

        // use collider point(s) to also work with big entities
        if (player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            Npc npc = (Npc)player.target;

            // revive
            if (itemIndex != -1 && itemIndex < player.inventory.Count &&
                player.inventory[itemIndex].amount > 0 &&
                player.inventory[itemIndex].item.data is PetItem)
            {
                ItemSlot slot = player.inventory[itemIndex];
                PetItem itemData = (PetItem)slot.item.data;
                if (itemData.petPrefab != null)
                {
                    itemSlot.GetComponent<Image>().color = Color.white;
                    itemSlot.GetComponent<Image>().sprite = slot.item.image;
                    itemSlot.GetComponent<UIShowToolTip>().enabled = true;
                    itemSlot.GetComponent<UIShowToolTip>().text = slot.ToolTip();
                    costsText.text = itemData.petPrefab.revivePrice.ToString();
                    reviveButton.interactable = slot.item.petHealth == 0 && player.gold >= itemData.petPrefab.revivePrice;
                    reviveButton.onClick.SetListener(() => {
                        player.CmdNpcRevivePet(itemIndex);
                        itemIndex = -1;
                    });
                }
            }
            else
            {
                itemSlot.GetComponent<Image>().color = Color.clear;
                itemSlot.GetComponent<Image>().sprite = null;
                itemSlot.GetComponent<UIShowToolTip>().enabled = false;
                costsText.text = "0";
                reviveButton.interactable = false;
            }
        }
        else panel.SetActive(false); // hide
    }
}

// =======================================================================================
// ADVANCED HARVESTING - UI
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if _AdvancedHarvesting

// =======================================================================================
// 	UI ADVANCED HARVESTING
// =======================================================================================
public partial class UIAdvancedHarvestingLoot : MonoBehaviour {

    public GameObject panel;
    public UILootSlot itemSlotPrefab;
    public Transform content;

    void Update() {
        var player = Utils.ClientLocalPlayer();
        if (!player) return;

        // use collider point(s) to also work with big entities
        if (panel.activeSelf &&
            player.UMO3d_AdvancedHarvesting_ValidateResourceNode() ) {
            
            /*
            player.target != null &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange &&
            player.target is SimpleResourceNode ) {
            */
            
            var target = (SimpleResourceNode)player.target;
            
            // instantiate/destroy enough slots
            // (we only want to show the non-empty slots)
            var items = target.inventory.Where(item => item.valid).ToList();
            UIUtils.BalancePrefabs(itemSlotPrefab.gameObject, items.Count, content);

            // refresh all valid items
            for (int i = 0; i < items.Count; ++i) {
                var slot = content.GetChild(i).GetComponent<UILootSlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                int itemIndex = player.target.inventory.FindIndex(
                    item => item.valid && item.name == items[i].name
                );

                // refresh
                slot.button.interactable = player.InventoryCanAddAmount(items[i].template, items[i].amount);
                slot.button.onClick.SetListener(() => {
                    player.Cmd_UMO3d_AdvancedHarvesting_TakeResources(itemIndex);
                });
                slot.tooltip.text = items[i].ToolTip();
                slot.image.color = Color.white;
                slot.image.sprite = items[i].image;
                slot.nameText.text = items[i].name;
                slot.amountOverlay.SetActive(items[i].amount > 1);
                slot.amountText.text = items[i].amount.ToString();
            }
        } else panel.SetActive(false); // hide

    }

    public void Show() { panel.SetActive(true); }
}

#endif

// =======================================================================================
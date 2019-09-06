// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;
using UnityEngine.UI;

public partial class UIEquipment : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.E;
    public GameObject panel;
    public UIEquipmentSlot slotPrefab;
    public Transform content;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            // only update the panel if it's active
            if (panel.activeSelf)
            {
                // instantiate/destroy enough slots
                UIUtils.BalancePrefabs(slotPrefab.gameObject, player.equipment.Count, content);

                // refresh all
                for (int i = 0; i < player.equipment.Count; ++i)
                {
                    UIEquipmentSlot slot = content.GetChild(i).GetComponent<UIEquipmentSlot>();
                    slot.dragAndDropable.name = i.ToString(); // drag and drop slot
                    ItemSlot itemSlot = player.equipment[i];

                    // set category overlay in any case. we use the last noun in the
                    // category string, for example EquipmentWeaponBow => Bow
                    // (disabled if no category, e.g. for archer shield slot)
                    slot.categoryOverlay.SetActive(player.equipmentInfo[i].requiredCategory != "");
                    string overlay = Utils.ParseLastNoun(player.equipmentInfo[i].requiredCategory);
                    slot.categoryText.text = overlay != "" ? overlay : "?";

                    if (itemSlot.amount > 0)
                    {
                        // refresh valid item
                        slot.tooltip.enabled = true;
                        slot.tooltip.text = itemSlot.ToolTip();
                        slot.dragAndDropable.dragable = true;
                        slot.image.color = Color.white;
                        slot.image.sprite = itemSlot.item.image;
                        slot.amountOverlay.SetActive(itemSlot.amount > 1);
                        slot.amountText.text = itemSlot.amount.ToString();
                    }
                    else
                    {
                        // refresh invalid item
                        slot.tooltip.enabled = false;
                        slot.dragAndDropable.dragable = false;
                        slot.image.color = Color.clear;
                        slot.image.sprite = null;
                        slot.amountOverlay.SetActive(false);
                    }
                }
            }
        }
        else panel.SetActive(false);
    }
}

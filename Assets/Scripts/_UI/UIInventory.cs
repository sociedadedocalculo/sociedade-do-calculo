// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;
using UnityEngine.UI;

public partial class UIInventory : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.I;
    public GameObject panel;
    public UIInventorySlot slotPrefab;
    public Transform content;
    public Text goldText;
    public UIDragAndDropable trash;
    public Image trashImage;
    public GameObject trashOverlay;
    public Text trashAmountText;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // hotkey (not while typing in chat, etc.)
        if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
            panel.SetActive(!panel.activeSelf);

        // only update the panel if it's active
        if (panel.activeSelf)
        {
            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.inventory.Count, content);

            // refresh all items
            for (int i = 0; i < player.inventory.Count; ++i)
            {
                UIInventorySlot slot = content.GetChild(i).GetComponent<UIInventorySlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                ItemSlot itemSlot = player.inventory[i];

                if (itemSlot.amount > 0)
                {
                    // refresh valid item
                    int icopy = i; // needed for lambdas, otherwise i is Count
                    slot.button.onClick.SetListener(() => {
                        if (itemSlot.item.data is UsableItem &&
                            ((UsableItem)itemSlot.item.data).CanUse(player, icopy))
                            player.CmdUseInventoryItem(icopy);
                    });
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
                    slot.button.onClick.RemoveAllListeners();
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.amountOverlay.SetActive(false);
                }
            }

            // gold
            goldText.text = player.gold.ToString();

            // trash (tooltip always enabled, dropable always true)
            trash.dragable = player.trash.amount > 0;
            if (player.trash.amount > 0)
            {
                // refresh valid item
                trashImage.color = Color.white;
                trashImage.sprite = player.trash.item.image;
                trashOverlay.SetActive(player.trash.amount > 1);
                trashAmountText.text = player.trash.amount.ToString();
            }
            else
            {
                // refresh invalid item
                trashImage.color = Color.clear;
                trashImage.sprite = null;
                trashOverlay.SetActive(false);
            }
        }
    }
}

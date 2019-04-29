using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UICrafting : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.C;
    public GameObject panel;
    public UICraftingIngredientSlot ingredientSlotPrefab;
    public Transform ingredientContent;
    public Image resultSlotImage;
    public UIShowToolTip resultSlotToolTip;
    public Button craftButton;

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
            UIUtils.BalancePrefabs(ingredientSlotPrefab.gameObject, player.craftingIndices.Count, ingredientContent);

            // refresh all
            for (int i = 0; i < player.craftingIndices.Count; ++i)
            {
                UICraftingIngredientSlot slot = ingredientContent.GetChild(i).GetComponent<UICraftingIngredientSlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                int itemIndex = player.craftingIndices[i];

                if (0 <= itemIndex && itemIndex < player.inventory.Count &&
                    player.inventory[itemIndex].amount > 0)
                {
                    ItemSlot itemSlot = player.inventory[itemIndex];

                    // refresh valid item
                    slot.tooltip.enabled = true;
                    slot.tooltip.text = itemSlot.ToolTip();
                    slot.dragAndDropable.dragable = true;
                    slot.image.color = Color.white;
                    slot.image.sprite = itemSlot.item.image;
                }
                else
                {
                    // reset the index because it's invalid
                    player.craftingIndices[i] = -1;

                    // refresh invalid item
                    slot.tooltip.enabled = false;
                    slot.dragAndDropable.dragable = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                }
            }

            // find valid indices => item templates => matching recipe
            List<int> validIndices = player.craftingIndices.Where(
                index => 0 <= index && index < player.inventory.Count &&
                       player.inventory[index].amount > 0
            ).ToList();
            List<ScriptableItem> items = validIndices.Select(index => player.inventory[index].item.data).ToList();
            ScriptableRecipe recipe = ScriptableRecipe.dict.Values.ToList().Find(r => r.CanCraftWith(items)); // good enough for now
            if (recipe != null)
            {
                // refresh valid recipe
                Item item = new Item(recipe.result);
                resultSlotToolTip.enabled = true;
                resultSlotToolTip.text = new ItemSlot(item).ToolTip(); // ItemSlot so that {AMOUNT} is replaced too
                resultSlotImage.color = Color.white;
                resultSlotImage.sprite = recipe.result.image;
            }
            else
            {
                // refresh invalid recipe
                resultSlotToolTip.enabled = false;
                resultSlotImage.color = Color.clear;
                resultSlotImage.sprite = null;
            }

            // craft button
            craftButton.interactable = recipe != null && player.InventoryCanAdd(new Item(recipe.result), 1);
            craftButton.onClick.SetListener(() => {
                player.CmdCraft(validIndices.ToArray());
            });
        }
    }
}

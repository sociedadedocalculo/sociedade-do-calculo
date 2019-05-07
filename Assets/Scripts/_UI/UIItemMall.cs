using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UIItemMall : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.X;
    public GameObject panel;
    public Button categorySlotPrefab;
    public Transform categoryContent;
    public ScrollRect scrollRect;
    public UIItemMallSlot itemSlotPrefab;
    public Transform itemContent;
    public string buyUrl = "http://unity3d.com/";
    int currentCategory = 0;
    public Text nameText;
    public Text levelText;
    public Text currencyAmountText;
    public Button buyButton;
    public InputField couponInput;
    public Button couponButton;
    public GameObject inventoryPanel;

    void ScrollToBeginning()
    {
        // update first so we don't ignore recently added messages, then scroll
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 1;
    }

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
            // instantiate/destroy enough category slots
            UIUtils.BalancePrefabs(categorySlotPrefab.gameObject, player.itemMallCategories.Length, categoryContent);

            // refresh all category buttons
            for (int i = 0; i < player.itemMallCategories.Length; ++i)
            {
                Button button = categoryContent.GetChild(i).GetComponent<Button>();
                button.interactable = i != currentCategory;
                button.GetComponentInChildren<Text>().text = player.itemMallCategories[i].category;
                int icopy = i; // needed for lambdas, otherwise i is Count
                button.onClick.SetListener(() => {
                    // set new category and then scroll to the top again
                    currentCategory = icopy;
                    ScrollToBeginning();
                });
            }

            if (player.itemMallCategories.Length > 0)
            {
                // instantiate/destroy enough item slots for that category
                ScriptableItem[] items = player.itemMallCategories[currentCategory].items;
                UIUtils.BalancePrefabs(itemSlotPrefab.gameObject, items.Length, itemContent);

                // refresh all items in that category
                for (int i = 0; i < items.Length; ++i)
                {
                    UIItemMallSlot slot = itemContent.GetChild(i).GetComponent<UIItemMallSlot>();
                    ScriptableItem itemData = items[i];

                    // refresh item
                    slot.tooltip.text = new Item(itemData).ToolTip();
                    slot.image.color = Color.white;
                    slot.image.sprite = itemData.image;
                    slot.nameText.text = itemData.name;
                    slot.priceText.text = itemData.itemMallPrice.ToString();
                    slot.unlockButton.interactable = player.health > 0 && player.coins >= itemData.itemMallPrice;
                    int icopy = i; // needed for lambdas, otherwise i is Count
                    slot.unlockButton.onClick.SetListener(() => {
                        player.CmdUnlockItem(currentCategory, icopy);
                        inventoryPanel.SetActive(true); // better feedback
                    });
                }
            }

            // overview
            nameText.text = player.name;
            levelText.text = "Lv. " + player.level;
            currencyAmountText.text = player.coins.ToString();
            buyButton.onClick.SetListener(() => { Application.OpenURL(buyUrl); });
            couponInput.interactable = NetworkTime.time >= player.nextRiskyActionTime;
            couponButton.interactable = NetworkTime.time >= player.nextRiskyActionTime;
            couponButton.onClick.SetListener(() => {
                if (!Utils.IsNullOrWhiteSpace(couponInput.text))
                    player.CmdEnterCoupon(couponInput.text);
                couponInput.text = "";
            });
        }
    }
}

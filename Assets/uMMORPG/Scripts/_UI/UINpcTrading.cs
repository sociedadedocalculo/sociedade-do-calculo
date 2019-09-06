using UnityEngine;
using UnityEngine.UI;

public partial class UINpcTrading : MonoBehaviour
{
    public static UINpcTrading singleton;
    public GameObject panel;
    public UINpcTradingSlot slotPrefab;
    public Transform content;
    public UIDragAndDropable buySlot;
    public InputField buyAmountInput;
    public Text buyCostsText;
    public Button buyButton;
    public UIDragAndDropable sellSlot;
    public InputField sellAmountInput;
    public Text sellCostsText;
    public Button sellButton;
    [HideInInspector] public int buyIndex = -1;
    [HideInInspector] public int sellIndex = -1;

    public UINpcTrading()
    {
        // assign singleton only once (to work with DontDestroyOnLoad when
        // using Zones / switching scenes)
        if (singleton == null) singleton = this;
    }

    void Update()
    {
        Player player = Player.localPlayer;

        // use collider point(s) to also work with big entities
        if (player != null &&
            player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            Npc npc = (Npc)player.target;

            // items for sale
            UIUtils.BalancePrefabs(slotPrefab.gameObject, npc.saleItems.Length, content);
            for (int i = 0; i < npc.saleItems.Length; ++i)
            {
                UINpcTradingSlot slot = content.GetChild(i).GetComponent<UINpcTradingSlot>();
                ScriptableItem itemData = npc.saleItems[i];

                // show item in UI
                int icopy = i;
                slot.button.onClick.SetListener(() => {
                    buyIndex = icopy;
                });
                slot.image.color = Color.white;
                slot.image.sprite = itemData.image;
                slot.tooltip.enabled = true;
                slot.tooltip.text = new ItemSlot(new Item(itemData)).ToolTip(); // with slot for {AMOUNT}
            }

            // buy
            if (buyIndex != -1 && buyIndex < npc.saleItems.Length)
            {
                ScriptableItem itemData = npc.saleItems[buyIndex];

                // make valid amount, calculate price
                int amount = buyAmountInput.text.ToInt();
                amount = Mathf.Clamp(amount, 1, itemData.maxStack);
                long price = amount * itemData.buyPrice;

                // show buy panel with item in UI
                buyAmountInput.text = amount.ToString();
                buySlot.GetComponent<Image>().color = Color.white;
                buySlot.GetComponent<Image>().sprite = itemData.image;
                buySlot.GetComponent<UIShowToolTip>().enabled = true;
                buySlot.GetComponent<UIShowToolTip>().text = new ItemSlot(new Item(itemData)).ToolTip(); // with slot for {AMOUNT}
                buySlot.dragable = true;
                buyCostsText.text = price.ToString();
                buyButton.interactable = amount > 0 && price <= player.gold &&
                                         player.InventoryCanAdd(new Item(itemData), amount);
                buyButton.onClick.SetListener(() => {
                    player.CmdNpcBuyItem(buyIndex, amount);
                    buyIndex = -1;
                    buyAmountInput.text = "1";
                });
            }
            else
            {
                // show default buy panel in UI
                buySlot.GetComponent<Image>().color = Color.clear;
                buySlot.GetComponent<Image>().sprite = null;
                buySlot.GetComponent<UIShowToolTip>().enabled = false;
                buySlot.dragable = false;
                buyCostsText.text = "0";
                buyButton.interactable = false;
            }

            // sell
            if (sellIndex != -1 && sellIndex < player.inventory.Count &&
                player.inventory[sellIndex].amount > 0)
            {
                ItemSlot itemSlot = player.inventory[sellIndex];

                // make valid amount, calculate price
                int amount = sellAmountInput.text.ToInt();
                amount = Mathf.Clamp(amount, 1, itemSlot.amount);
                long price = amount * itemSlot.item.sellPrice;

                // show sell panel with item in UI
                sellAmountInput.text = amount.ToString();
                sellSlot.GetComponent<Image>().color = Color.white;
                sellSlot.GetComponent<Image>().sprite = itemSlot.item.image;
                sellSlot.GetComponent<UIShowToolTip>().enabled = true;
                sellSlot.GetComponent<UIShowToolTip>().text = itemSlot.ToolTip();
                sellSlot.dragable = true;
                sellCostsText.text = price.ToString();
                sellButton.interactable = amount > 0;
                sellButton.onClick.SetListener(() => {
                    player.CmdNpcSellItem(sellIndex, amount);
                    sellIndex = -1;
                    sellAmountInput.text = "1";
                });
            }
            else
            {
                // show default sell panel in UI
                sellSlot.GetComponent<Image>().color = Color.clear;
                sellSlot.GetComponent<Image>().sprite = null;
                sellSlot.GetComponent<UIShowToolTip>().enabled = false;
                sellSlot.dragable = false;
                sellCostsText.text = "0";
                sellButton.interactable = false;
            }
        }
        else panel.SetActive(false);
    }
}

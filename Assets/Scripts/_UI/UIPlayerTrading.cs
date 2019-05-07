// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using UnityEngine;
using UnityEngine.UI;

public partial class UIPlayerTrading : MonoBehaviour
{
    public GameObject panel;

    public UIPlayerTradingSlot slotPrefab;

    public Transform otherContent;
    public Text otherStatusText;
    public InputField otherGoldInput;

    public Transform myContent;
    public Text myStatusText;
    public InputField myGoldInput;

    public Button lockButton;
    public Button acceptButton;
    public Button cancelButton;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // only if trading, otherwise set inactive
        if (player.state == "TRADING" && player.target != null && player.target is Player)
        {
            panel.SetActive(true);
            Player other = (Player)player.target;

            // OTHER ///////////////////////////////////////////////////////////
            // status text
            if (other.tradeStatus == TradeStatus.Accepted) otherStatusText.text = "[ACCEPTED]";
            else if (other.tradeStatus == TradeStatus.Locked) otherStatusText.text = "[LOCKED]";
            else otherStatusText.text = "";

            // gold input
            otherGoldInput.text = other.tradeOfferGold.ToString();

            // items
            UIUtils.BalancePrefabs(slotPrefab.gameObject, other.tradeOfferItems.Count, otherContent);
            for (int i = 0; i < other.tradeOfferItems.Count; ++i)
            {
                UIPlayerTradingSlot slot = otherContent.GetChild(i).GetComponent<UIPlayerTradingSlot>();
                int inventoryIndex = other.tradeOfferItems[i];

                slot.dragAndDropable.dragable = false;
                slot.dragAndDropable.dropable = false;

                if (0 <= inventoryIndex && inventoryIndex < other.inventory.Count &&
                    other.inventory[inventoryIndex].amount > 0)
                {
                    ItemSlot itemSlot = other.inventory[inventoryIndex];

                    // refresh valid item
                    slot.tooltip.enabled = true;
                    slot.tooltip.text = itemSlot.ToolTip();
                    slot.image.color = Color.white;
                    slot.image.sprite = itemSlot.item.image;
                    slot.amountOverlay.SetActive(itemSlot.amount > 1);
                    slot.amountText.text = itemSlot.amount.ToString();
                }
                else
                {
                    // refresh invalid item
                    slot.tooltip.enabled = false;
                    slot.image.color = Color.clear;
                    slot.image.sprite = null;
                    slot.amountOverlay.SetActive(false);
                }
            }

            // SELF ////////////////////////////////////////////////////////////
            // status text
            if (player.tradeStatus == TradeStatus.Accepted) myStatusText.text = "[ACCEPTED]";
            else if (player.tradeStatus == TradeStatus.Locked) myStatusText.text = "[LOCKED]";
            else myStatusText.text = "";

            // gold input
            if (player.tradeStatus == TradeStatus.Free)
            {
                myGoldInput.interactable = true;
                myGoldInput.onValueChanged.SetListener(val => {
                    long goldOffer = Utils.Clamp(val.ToLong(), 0, player.gold);
                    myGoldInput.text = goldOffer.ToString();
                    player.CmdTradeOfferGold(goldOffer);
                });
            }
            else
            {
                myGoldInput.interactable = false;
                myGoldInput.text = player.tradeOfferGold.ToString();
            }

            // items
            UIUtils.BalancePrefabs(slotPrefab.gameObject, player.tradeOfferItems.Count, myContent);
            for (int i = 0; i < player.tradeOfferItems.Count; ++i)
            {
                UIPlayerTradingSlot slot = myContent.GetChild(i).GetComponent<UIPlayerTradingSlot>();
                slot.dragAndDropable.name = i.ToString(); // drag and drop index
                int inventoryIndex = player.tradeOfferItems[i];

                if (0 <= inventoryIndex && inventoryIndex < player.inventory.Count &&
                    player.inventory[inventoryIndex].amount > 0)
                {
                    ItemSlot itemSlot = player.inventory[inventoryIndex];

                    // refresh valid item
                    slot.tooltip.enabled = true;
                    slot.tooltip.text = itemSlot.ToolTip();
                    slot.dragAndDropable.dragable = player.tradeStatus == TradeStatus.Free;
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

            // buttons /////////////////////////////////////////////////////////
            // lock
            lockButton.interactable = player.tradeStatus == TradeStatus.Free;
            lockButton.onClick.SetListener(() => {
                player.CmdTradeOfferLock();
            });

            // accept (only if both have locked the trade & if not accepted yet)
            // accept (if not accepted yet & other has locked or accepted)
            acceptButton.interactable = player.tradeStatus == TradeStatus.Locked &&
                                        other.tradeStatus != TradeStatus.Free;
            acceptButton.onClick.SetListener(() => {
                player.CmdTradeOfferAccept();
            });

            // cancel
            cancelButton.onClick.SetListener(() => {
                player.CmdTradeCancel();
            });
        }
        else
        {
            panel.SetActive(false);
#if !UNITY_2017_2 // workaround for Unity 2017.2 UI bug #960900
            myGoldInput.text = "0"; // reset
#endif
        }
    }
}

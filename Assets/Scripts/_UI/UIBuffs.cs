using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public partial class UIBuffs : MonoBehaviour
{
    public UIBuffSlot slotPrefab;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // instantiate/destroy enough slots
        UIUtils.BalancePrefabs(slotPrefab.gameObject, player.buffs.Count, transform);

        // refresh all
        for (int i = 0; i < player.buffs.Count; ++i)
        {
            UIBuffSlot slot = transform.GetChild(i).GetComponent<UIBuffSlot>();

            // refresh
            slot.image.color = Color.white;
            slot.image.sprite = player.buffs[i].image;
            slot.tooltip.text = player.buffs[i].ToolTip();
            slot.slider.maxValue = player.buffs[i].buffTime;
            slot.slider.value = player.buffs[i].BuffTimeRemaining();
        }
    }
}
using UnityEngine;
using UnityEngine.UI;

public partial class UISkillbar : MonoBehaviour
{
    public GameObject panel;
    public UISkillbarSlot slotPrefab;
    public Transform content;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        panel.SetActive(player != null); // hide while not in the game world
        if (!player) return;

        // instantiate/destroy enough slots
        UIUtils.BalancePrefabs(slotPrefab.gameObject, player.skillbar.Length, content);

        // refresh all
        for (int i = 0; i < player.skillbar.Length; ++i)
        {
            UISkillbarSlot slot = content.GetChild(i).GetComponent<UISkillbarSlot>();
            slot.dragAndDropable.name = i.ToString(); // drag and drop index

            // hotkey overlay (without 'Alpha' etc.)
            string pretty = player.skillbar[i].hotKey.ToString().Replace("Alpha", "");
            slot.hotkeyText.text = pretty;

            // skill, inventory item or equipment item?
            int skillIndex = player.GetSkillIndexByName(player.skillbar[i].reference);
            int invIndex = player.GetInventoryIndexByName(player.skillbar[i].reference);
            int equipIndex = player.GetEquipmentIndexByName(player.skillbar[i].reference);
            if (skillIndex != -1)
            {
                Skill skill = player.skills[skillIndex];

                // hotkey pressed and not typing in any input right now?
                if (Input.GetKeyDown(player.skillbar[i].hotKey) &&
                    !UIUtils.AnyInputActive() &&
                    player.CastCheckSelf(skill)) // checks mana, cooldowns, etc.) {
                {
                    player.CmdUseSkill(skillIndex);
                }

                // refresh skill slot
                slot.button.interactable = player.CastCheckSelf(skill); // check mana, cooldowns, etc.
                slot.button.onClick.SetListener(() => {
                    player.CmdUseSkill(skillIndex);
                });
                slot.tooltip.enabled = true;
                slot.tooltip.text = skill.ToolTip();
                slot.dragAndDropable.dragable = true;
                slot.image.color = Color.white;
                slot.image.sprite = skill.image;
                float cooldown = skill.CooldownRemaining();
                slot.cooldownOverlay.SetActive(cooldown > 0);
                slot.cooldownText.text = cooldown.ToString("F0");
                slot.cooldownCircle.fillAmount = skill.cooldown > 0 ? cooldown / skill.cooldown : 0;
                slot.amountOverlay.SetActive(false);
            }
            else if (invIndex != -1)
            {
                ItemSlot itemSlot = player.inventory[invIndex];

                // hotkey pressed and not typing in any input right now?
                if (Input.GetKeyDown(player.skillbar[i].hotKey) && !UIUtils.AnyInputActive())
                    player.CmdUseInventoryItem(invIndex);

                // refresh inventory slot
                slot.button.onClick.SetListener(() => {
                    player.CmdUseInventoryItem(invIndex);
                });
                slot.tooltip.enabled = true;
                slot.tooltip.text = itemSlot.ToolTip();
                slot.dragAndDropable.dragable = true;
                slot.image.color = Color.white;
                slot.image.sprite = itemSlot.item.image;
                slot.cooldownOverlay.SetActive(false);
                slot.cooldownCircle.fillAmount = 0;
                slot.amountOverlay.SetActive(itemSlot.amount > 1);
                slot.amountText.text = itemSlot.amount.ToString();
            }
            else if (equipIndex != -1)
            {
                ItemSlot itemSlot = player.equipment[equipIndex];

                // refresh equipment slot
                slot.button.onClick.RemoveAllListeners();
                slot.tooltip.enabled = true;
                slot.tooltip.text = itemSlot.ToolTip();
                slot.dragAndDropable.dragable = true;
                slot.image.color = Color.white;
                slot.image.sprite = itemSlot.item.image;
                slot.cooldownOverlay.SetActive(false);
                slot.cooldownCircle.fillAmount = 0;
                slot.amountOverlay.SetActive(itemSlot.amount > 1);
                slot.amountText.text = itemSlot.amount.ToString();
            }
            else
            {
                // clear the outdated reference
                player.skillbar[i].reference = "";

                // refresh empty slot
                slot.button.onClick.RemoveAllListeners();
                slot.tooltip.enabled = false;
                slot.dragAndDropable.dragable = false;
                slot.image.color = Color.clear;
                slot.image.sprite = null;
                slot.cooldownOverlay.SetActive(false);
                slot.cooldownCircle.fillAmount = 0;
                slot.amountOverlay.SetActive(false);
            }
        }
    }
}

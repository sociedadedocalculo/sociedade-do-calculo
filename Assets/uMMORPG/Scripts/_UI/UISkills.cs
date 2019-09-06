// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public partial class UISkills : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.R;
    public GameObject panel;
    public UISkillSlot slotPrefab;
    public Transform content;
    public Text skillExperienceText;

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
                // (we only care about non status skills)
                UIUtils.BalancePrefabs(slotPrefab.gameObject, player.skills.Count, content);

                // refresh all
                for (int i = 0; i < player.skills.Count; ++i)
                {
                    UISkillSlot slot = content.GetChild(i).GetComponent<UISkillSlot>();
                    Skill skill = player.skills[i];

                    bool isPassive = skill.data is PassiveSkill;

                    // set state
                    slot.dragAndDropable.name = i.ToString();
                    slot.dragAndDropable.dragable = skill.level > 0 && !isPassive;

                    // click event
                    slot.button.interactable = skill.level > 0 &&
                                               !isPassive &&
                                               player.CastCheckSelf(skill); // checks mana, cooldown etc.
                    int icopy = i;
                    slot.button.onClick.SetListener(() => {
                        // try use the skill or walk closer if needed
                        player.TryUseSkill(icopy);
                    });

                    // image
                    if (skill.level > 0)
                    {
                        slot.image.color = Color.white;
                        slot.image.sprite = skill.image;
                    }

                    // description
                    slot.descriptionText.text = skill.ToolTip(showRequirements: skill.level == 0);

                    // learn / upgrade
                    if (skill.level < skill.maxLevel && player.CanUpgradeSkill(skill))
                    {
                        slot.upgradeButton.gameObject.SetActive(true);
                        slot.upgradeButton.GetComponentInChildren<Text>().text = skill.level == 0 ? "Learn" : "Upgrade";
                        slot.upgradeButton.onClick.SetListener(() => { player.CmdUpgradeSkill(icopy); });
                    }
                    else slot.upgradeButton.gameObject.SetActive(false);

                    // cooldown overlay
                    float cooldown = skill.CooldownRemaining();
                    slot.cooldownOverlay.SetActive(skill.level > 0 && cooldown > 0);
                    slot.cooldownText.text = cooldown.ToString("F0");
                    slot.cooldownCircle.fillAmount = skill.cooldown > 0 ? cooldown / skill.cooldown : 0;
                }

                // skill experience
                skillExperienceText.text = player.skillExperience.ToString();
            }
        }
        else panel.SetActive(false);
    }
}

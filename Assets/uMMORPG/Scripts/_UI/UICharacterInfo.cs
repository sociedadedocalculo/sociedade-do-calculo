// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using UnityEngine;
using UnityEngine.UI;

public partial class UICharacterInfo : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.T;
    public GameObject panel;
    public Text damageText;
    public Text defenseText;
    public Text healthText;
    public Text manaText;
    public Text criticalChanceText;
    public Text blockChanceText;
    public Text speedText;
    public Text levelText;
    public Text currentExperienceText;
    public Text maximumExperienceText;
    public Text skillExperienceText;
    public Text strengthText;
    public Text intelligenceText;
    public Button strengthButton;
    public Button intelligenceButton;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            // only refresh the panel while it's active
            if (panel.activeSelf)
            {
                damageText.text = player.damage.ToString();
                defenseText.text = player.defense.ToString();
                healthText.text = player.healthMax.ToString();
                manaText.text = player.manaMax.ToString();
                criticalChanceText.text = (player.criticalChance * 100).ToString("F0") + "%";
                blockChanceText.text = (player.blockChance * 100).ToString("F0") + "%";
                speedText.text = player.speed.ToString();
                levelText.text = player.level.ToString();
                currentExperienceText.text = player.experience.ToString();
                maximumExperienceText.text = player.experienceMax.ToString();
                skillExperienceText.text = player.skillExperience.ToString();

                strengthText.text = player.strength.ToString();
                strengthButton.interactable = player.AttributesSpendable() > 0;
                strengthButton.onClick.SetListener(() => {
                    player.CmdIncreaseStrength();
                });

                intelligenceText.text = player.intelligence.ToString();
                intelligenceButton.interactable = player.AttributesSpendable() > 0;
                intelligenceButton.onClick.SetListener(() => {
                    player.CmdIncreaseIntelligence();
                });
            }
        }
        else panel.SetActive(false);
    }
}

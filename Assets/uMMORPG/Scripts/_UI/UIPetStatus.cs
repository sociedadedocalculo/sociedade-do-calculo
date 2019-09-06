using UnityEngine;
using UnityEngine.UI;

public class UIPetStatus : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Slider experienceSlider;
    public Text nameText;
    public Text levelText;
    public Button autoAttackButton;
    public Button defendOwnerButton;
    public Button unsummonButton;

    void Update()
    {
        Player player = Player.localPlayer;

        if (player != null && player.activePet != null)
        {
            Pet pet = player.activePet;
            panel.SetActive(true);

            healthSlider.value = pet.HealthPercent();
            healthSlider.GetComponent<UIShowToolTip>().text = "Health: " + pet.health + " / " + pet.healthMax;

            experienceSlider.value = pet.ExperiencePercent();
            experienceSlider.GetComponent<UIShowToolTip>().text = "Experience: " + pet.experience + " / " + pet.experienceMax;

            nameText.text = pet.name;
            levelText.text = "Lv." + pet.level.ToString();

            autoAttackButton.GetComponentInChildren<Text>().fontStyle = pet.autoAttack ? FontStyle.Bold : FontStyle.Normal;
            autoAttackButton.onClick.SetListener(() => {
                player.CmdPetSetAutoAttack(!pet.autoAttack);
            });

            defendOwnerButton.GetComponentInChildren<Text>().fontStyle = pet.defendOwner ? FontStyle.Bold : FontStyle.Normal;
            defendOwnerButton.onClick.SetListener(() => {
                player.CmdPetSetDefendOwner(!pet.defendOwner);
            });

            //unsummonButton.interactable = player.CanUnsummonPet(); <- looks too annoying if button flashes rapidly
            unsummonButton.onClick.SetListener(() => {
                if (player.CanUnsummonPet()) player.CmdPetUnsummon();
            });
        }
        else panel.SetActive(false);
    }
}

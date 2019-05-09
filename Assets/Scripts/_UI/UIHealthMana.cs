using UnityEngine;
using UnityEngine.UI;

public partial class UIHealthMana : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text healthStatus;
    public Slider manaSlider;
    public Text manaStatus;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        panel.SetActive(player != null); // hide while not in the game world
        if (!player) return;

        healthSlider.value = player.HealthPercent();
        healthStatus.text = player.health + " / " + player.healthMax;

        manaSlider.value = player.ManaPercent();
        manaStatus.text = player.mana + " / " + player.manaMax;
    }
}

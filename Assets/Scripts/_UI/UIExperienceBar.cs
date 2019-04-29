using UnityEngine;
using UnityEngine.UI;

public partial class UIExperienceBar : MonoBehaviour
{
    public GameObject panel;
    public Slider slider;
    public Text statusText;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        panel.SetActive(player != null); // hide while not in the game world
        if (!player) return;

        slider.value = player.ExperiencePercent();
        statusText.text = "Lv." + player.level + " (" + (player.ExperiencePercent() * 100).ToString("F2") + "%)";
    }
}

using UnityEngine;
using UnityEngine.UI;

public partial class UINpcGuildManagement : MonoBehaviour {
    public GameObject panel;
    public Text createPriceText;
    public InputField createNameInput;
    public Button createButton;
    public Button terminateButton;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // use collider point(s) to also work with big entities
        if (player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            createNameInput.interactable = !player.InGuild() &&
                                           player.gold >= Guild.CreationPrice;
            createNameInput.characterLimit = Guild.NameMaxLength;

            createPriceText.text = Guild.CreationPrice.ToString();

            createButton.interactable = !player.InGuild() && Guild.IsValidGuildName(createNameInput.text);
            createButton.onClick.SetListener(() => {
                player.CmdCreateGuild(createNameInput.text);
                createNameInput.text = ""; // clear the input afterwards
            });

            terminateButton.interactable = player.guild.CanTerminate(player.name);
            terminateButton.onClick.SetListener(() => {
                player.CmdTerminateGuild();
            });
        }
        else panel.SetActive(false); // hide
    }
}

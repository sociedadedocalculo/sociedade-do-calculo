// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using UnityEngine;
using UnityEngine.UI;

public partial class UIGuildInvite : MonoBehaviour
{
    public GameObject panel;
    public Text nameText;
    public Button acceptButton;
    public Button declineButton;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // only if there is an invite
        if (player.guildInviteFrom != "")
        {
            panel.SetActive(true);
            nameText.text = player.guildInviteFrom;
            acceptButton.onClick.SetListener(() => {
                player.CmdGuildInviteAccept();
            });
            declineButton.onClick.SetListener(() => {
                player.CmdGuildInviteDecline();
            });
        }
        else panel.SetActive(false); // hide
    }
}

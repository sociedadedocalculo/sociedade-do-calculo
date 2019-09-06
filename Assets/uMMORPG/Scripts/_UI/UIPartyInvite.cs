// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using UnityEngine;
using UnityEngine.UI;

public partial class UIPartyInvite : MonoBehaviour
{
    public GameObject panel;
    public Text nameText;
    public Button acceptButton;
    public Button declineButton;

    void Update()
    {
        Player player = Player.localPlayer;

        if (player != null && player.partyInviteFrom != "")
        {
            panel.SetActive(true);
            nameText.text = player.partyInviteFrom;
            acceptButton.onClick.SetListener(() => {
                player.CmdPartyInviteAccept();
            });
            declineButton.onClick.SetListener(() => {
                player.CmdPartyInviteDecline();
            });
        }
        else panel.SetActive(false); // hide
    }
}

// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using UnityEngine;
using UnityEngine.UI;

public partial class UITarget : MonoBehaviour
{
    public GameObject panel;
    public Slider healthSlider;
    public Text nameText;
    public Button tradeButton;
    public Button guildInviteButton;
    public Button partyInviteButton;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // show nextTarget > target
        // => feels best in situations where we select another target while
        //    casting a skill on the existing target.
        // => '.target' doesn't change while casting, but the UI gives the
        //    illusion that we already targeted something else
        // => this is also great for skills that change the target while casting,
        //    e.g. a buff that is cast on 'self' even though we target an 'npc.
        //    this way the player doesn't see the target switching.
        // => this is how most MMORPGs do it too.
        Entity target = player.nextTarget ?? player.target;
        if (target != null && target != player)
        {
            float distance = Utils.ClosestDistance(player.collider, target.collider);

            // name and health
            panel.SetActive(true);
            healthSlider.value = target.HealthPercent();
            nameText.text = target.name;

            // trade button
            if (target is Player)
            {
                tradeButton.gameObject.SetActive(true);
                tradeButton.interactable = player.CanStartTradeWith(target);
                tradeButton.onClick.SetListener(() => {
                    player.CmdTradeRequestSend();
                });
            }
            else tradeButton.gameObject.SetActive(false);

            // guild invite button
            if (target is Player && player.InGuild())
            {
                guildInviteButton.gameObject.SetActive(true);
                guildInviteButton.interactable = !((Player)target).InGuild() &&
                                                 player.guild.CanInvite(player.name, target.name) &&
                                                 NetworkTime.time >= player.nextRiskyActionTime &&
                                                 distance <= player.interactionRange;
                guildInviteButton.onClick.SetListener(() => {
                    player.CmdGuildInviteTarget();
                });
            }
            else guildInviteButton.gameObject.SetActive(false);

            // party invite button
            if (target is Player)
            {
                partyInviteButton.gameObject.SetActive(true);
                partyInviteButton.interactable = (!player.InParty() || player.party.CanInvite(player.name)) &&
                                                 !((Player)target).InParty() &&
                                                 NetworkTime.time >= player.nextRiskyActionTime &&
                                                 distance <= player.interactionRange;
                partyInviteButton.onClick.SetListener(() => {
                    player.CmdPartyInvite(target.name);
                });
            }
            else partyInviteButton.gameObject.SetActive(false);
        }
        else panel.SetActive(false); // hide
    }
}

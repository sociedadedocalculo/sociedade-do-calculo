// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UINpcDialogue : MonoBehaviour
{
    public static UINpcDialogue singleton;
    public GameObject panel;
    public Text welcomeText;
    public Button tradingButton;
    public Button teleportButton;
    public Button questsButton;
    public Button guildButton;
    public Button petReviveButton;
    public GameObject npcTradingPanel;
    public GameObject npcQuestPanel;
    public GameObject npcGuildPanel;
    public GameObject npcPetRevivePanel;
    public GameObject inventoryPanel;

    public UINpcDialogue() { singleton = this; }

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // use collider point(s) to also work with big entities
        if (panel.activeSelf &&
            player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            Npc npc = (Npc)player.target;

            // welcome text
            welcomeText.text = npc.welcome;

            // trading button
            tradingButton.gameObject.SetActive(npc.saleItems.Length > 0);
            tradingButton.onClick.SetListener(() => {
                npcTradingPanel.SetActive(true);
                inventoryPanel.SetActive(true); // better feedback
                panel.SetActive(false);
            });

            // teleport button
            teleportButton.gameObject.SetActive(npc.teleportTo != null);
            if (npc.teleportTo != null)
                teleportButton.GetComponentInChildren<Text>().text = "Teleport: " + npc.teleportTo.name;
            teleportButton.onClick.SetListener(() => {
                player.CmdNpcTeleport();
            });

            // filter out the quests that are available for the player
            List<ScriptableQuest> questsAvailable = npc.QuestsVisibleFor(player);
            questsButton.gameObject.SetActive(questsAvailable.Count > 0);
            questsButton.onClick.SetListener(() => {
                npcQuestPanel.SetActive(true);
                panel.SetActive(false);
            });

            // guild
            guildButton.gameObject.SetActive(npc.offersGuildManagement);
            guildButton.onClick.SetListener(() => {
                npcGuildPanel.SetActive(true);
                panel.SetActive(false);
            });

            // pet revive
            petReviveButton.gameObject.SetActive(npc.offersPetRevive);
            petReviveButton.onClick.SetListener(() => {
                npcPetRevivePanel.SetActive(true);
                inventoryPanel.SetActive(true); // better feedback
                panel.SetActive(false);
            });
        }
        else panel.SetActive(false); // hide
    }

    public void Show() { panel.SetActive(true); }
}

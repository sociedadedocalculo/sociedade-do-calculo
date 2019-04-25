using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UINpcPergunta : MonoBehaviour
{
    public GameObject panel;
    public UINpcQuestSlot slotPrefab;
    public Transform content;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // npc quest
        // use collider point(s) to also work with big entities
        if (player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            Npc npc = (Npc)player.target;

            // instantiate/destroy enough slots
            List<ScriptableQuest> questsAvailable = npc.QuestsVisibleFor(player);
            UIUtils.BalancePrefabs(slotPrefab.gameObject, questsAvailable.Count, content);

            // refresh all
            for (int i = 0; i < questsAvailable.Count; ++i)
            {
                UINpcQuestSlot slot = content.GetChild(i).GetComponent<UINpcQuestSlot>();

                // find quest index in original npc quest list (unfiltered)
                int npcIndex = Array.FindIndex(npc.quests, q => q.name == questsAvailable[i].name);

                // find quest index in player quest list
                int questIndex = player.GetQuestIndexByName(npc.quests[npcIndex].name);
                if (questIndex != -1)
                {
                    // running quest: shows description with current progress
                    // instead of static one
                    Quest quest = player.quests[questIndex];
                    ScriptableItem reward = npc.quests[npcIndex].rewardItem;
                    int gathered = quest.gatherItem != null ? player.InventoryCount(new Item(quest.gatherItem)) : 0;
                    bool hasSpace = reward == null || player.InventoryCanAdd(new Item(reward), 1);

                    // description + not enough space warning (if needed)
                    slot.descriptionText.text = quest.ToolTip(gathered);
                    if (!hasSpace)
                        slot.descriptionText.text += "\n<color=red>Not enough inventory space!</color>";

                    slot.actionButton.interactable = player.CanCompleteQuest(quest.name);
                    slot.actionButton.GetComponentInChildren<Text>().text = "Complete";
                    slot.actionButton.onClick.SetListener(() => {
                        player.CmdCompleteQuest(npcIndex);
                        panel.SetActive(false);
                    });
                }
                else
                {
                    // new quest
                    slot.descriptionText.text = new Quest(npc.quests[npcIndex]).ToolTip();
                    slot.actionButton.interactable = true;
                    slot.actionButton.GetComponentInChildren<Text>().text = "Aceita";
                    slot.actionButton.onClick.SetListener(() => {
                        player.CmdAcceptQuest(npcIndex);
                    });
                }
            }
        }
        else panel.SetActive(false); // hide
    }
}

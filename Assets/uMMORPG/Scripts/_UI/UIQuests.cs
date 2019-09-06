// Note: this script has to be on an always-active UI parent, so that we can
// always react to the hotkey.
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UIQuests : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.Q;
    public GameObject panel;
    public Transform content;
    public UIQuestSlot slotPrefab;

    public string expandPrefix = "[+] ";
    public string hidePrefix = "[-] ";

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            // hotkey (not while typing in chat, etc.)
            if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
                panel.SetActive(!panel.activeSelf);

            // only update the panel if it's active
            if (panel.activeSelf)
            {
                // only show active quests, no completed ones
                List<Quest> activeQuests = player.quests.Where(q => !q.completed).ToList();

                // instantiate/destroy enough slots
                UIUtils.BalancePrefabs(slotPrefab.gameObject, activeQuests.Count, content);

                // refresh all
                for (int i = 0; i < activeQuests.Count; ++i)
                {
                    UIQuestSlot slot = content.GetChild(i).GetComponent<UIQuestSlot>();
                    Quest quest = activeQuests[i];

                    // name button
                    GameObject descriptionPanel = slot.descriptionText.gameObject;
                    string prefix = descriptionPanel.activeSelf ? hidePrefix : expandPrefix;
                    slot.nameButton.GetComponentInChildren<Text>().text = prefix + quest.name;
                    slot.nameButton.onClick.SetListener(() => {
                        descriptionPanel.SetActive(!descriptionPanel.activeSelf);
                    });

                    // description
                    slot.descriptionText.text = quest.ToolTip(player);
                }
            }
        }
        else panel.SetActive(false);
    }
}

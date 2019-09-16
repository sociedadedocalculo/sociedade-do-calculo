using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINpcDungeonDialogue : MonoBehaviour {

    public Transform panel;
    public Transform buttonsPanel;
    public Button dungeonNameButtonPrefab;

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        var player = Utils.ClientLocalPlayer();
        if (!player) return;

        if(player.target is Npc)
        {
            //var dungeons = GameObject.Find("NetworkManager").GetComponent<NetworkManagerMMO>().Dungeons();
            var dungeons = (player.target as Npc).AvaibleDungeons;

            UIUtils.BalancePrefabs(dungeonNameButtonPrefab.gameObject, dungeons.Count, buttonsPanel);

            // refresh buttons
            for (int i = 0; i < dungeons.Count; i++)
            {
                var button = buttonsPanel.GetChild(i).GetComponent<Button>();
                var text = button.transform.GetChild(0).GetComponent<Text>();

                button.name = dungeons[i].name;
                text.text = dungeons[i].name;

                button.onClick.SetListener(() => {
                    player.CmdJoinDungeon(button.name);
                    panel.gameObject.SetActive(false);
                });
            }
        }
        else
        {
            panel.gameObject.SetActive(false);
        }
        
    }
}

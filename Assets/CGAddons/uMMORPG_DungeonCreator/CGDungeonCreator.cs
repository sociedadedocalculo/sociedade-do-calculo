using Mono.Data.Sqlite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CGDungeonCreator {

    public static long curID = 0;

    public static GameObject dungeonsStartPoint;    
}

public partial class UINpcDialogue
{
    [SerializeField]
    Button dungeonButton;
    [SerializeField]
    Button dungeonExitButton;
    [SerializeField]
    GameObject dungeonPanel;

    void Update_DungeonCreator()
    {
        var player = Utils.ClientLocalPlayer();
        if (!player) return;

        if (panel.activeSelf &&
            player.target != null && player.target is Npc &&
            Utils.ClosestDistance(player.collider, player.target.collider) <= player.interactionRange)
        {
            var npc = (Npc)player.target;

            if (npc.offersDungeonEntering)
            {
                dungeonButton.gameObject.SetActive(true);
                dungeonButton.onClick.SetListener(() => {
                    dungeonPanel.SetActive(true);
                    panel.SetActive(false);
                });
            }
            else
            {
                dungeonButton.gameObject.SetActive(false);
            }

            if (npc.offersDungeonExiting)
            {
                dungeonExitButton.gameObject.SetActive(true);
                dungeonExitButton.onClick.SetListener(() => {
                    player.CmdExitDungeon();
                    panel.SetActive(false);
                });
            }
            else
            {
                dungeonExitButton.gameObject.SetActive(false);
            }
        }
    }

}

public partial class Npc
{
    [Header("Dungeon Creator")]
    public bool offersDungeonEntering = false;

    [Header("Dungeon Creator")]
    public bool offersDungeonExiting = false;

    [Header("Dungeon Creator")]
    public List<CGDungeon> AvaibleDungeons;
}

public partial class Player
{
    public Vector3 BeforeDungeonPos = new Vector3();
    public Vector3 DungeonPos = new Vector3();

    [SyncVar_]
    public string CurDungeonName = "";

    public long DungeonID = -1;

    [Command(channel = Channels.DefaultUnreliable)]
    public void CmdJoinDungeon(string dungeonName)
    {
        Debug.Log("Player want to join " + dungeonName);

        if (DungeonID != -1)
        {
            TargetSendDungeonMessage(connectionToClient, "You have dungeon instance, exit from it first.", Color.red);
            
            return;
        }

        CGDungeon dun = GameObject.Find("NetworkManager").GetComponent<NetworkManagerMMO>().Dungeons().Find(x => x.name == dungeonName);

        if (dun == null)
        {
            TargetSendDungeonMessage(connectionToClient, "Dungeon not exist.", Color.red);
            
            return;
        }

#if _PartyAddon
        if (dun.RequireParty)
        {
            if (partyMembers.Count == 0)
            {
                TargetSendDungeonMessage(connectionToClient, "This dungeon need party!", Color.red);

                return;
            }

            if (partyMembers[0].name != name)
            {
                TargetSendDungeonMessage(connectionToClient, "You are not party leader!", Color.red);

                return;
            }

            if (partyMembers.Count < dun.minPartyMemberCount)
            {
                TargetSendDungeonMessage(connectionToClient, "You don't have enought player on your party! Need " + (dun.minPartyMemberCount - partyMembers.Count) + " more player", Color.red);

                return;
            }

            if (partyMembers.Count > dun.maxPartyMemberCount)
            {
                TargetSendDungeonMessage(connectionToClient, "You have too much player on your party! Max party member count :" + dun.maxPartyMemberCount, Color.red);

                return;
            }

            if (gold < dun.RequiredGold)
            {
                TargetSendDungeonMessage(connectionToClient, "Need " + dun.RequiredGold + " gold for this dungeon. Your money is not enought.", Color.red);

                return;
            }

            bool levelsOK = true;

            foreach (var p in partyMembers)
            {
                if (Utils.FindPlayerFromName(p.name).level < dun.RequiredLevel)
                {
                    levelsOK = false;
                }
            }

            if (!levelsOK)
            {
                TargetSendDungeonMessage(connectionToClient, "Min level for this dungeon is " + dun.RequiredLevel + ". Your party members are not strong.", Color.red);

                return;
            }

            if (dun.reqItems.Count > 0)
            {
                List<bool> itemsExist = new List<bool>();

                foreach (var item in dun.reqItems)
                {
                    if (InventoryCountAmount(item.name) > 0)
                    {
                        itemsExist.Add(true);
                    }
                    else
                    {
                        itemsExist.Add(false);
                    }
                }

                if (itemsExist.Contains(false))
                {
                    TargetSendDungeonMessage(connectionToClient, "Not Enought Item", Color.red);
                    
                    return;
                }
                else
                {
                    foreach (var item in dun.reqItems)
                    {
                        InventoryRemoveAmount(item.name, 1);
                    }
                }
            }

            gold -= dun.RequiredGold;

            var _dungeon = GameObject.Instantiate(dun.gameObject, CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID, Quaternion.identity);
            _dungeon.transform.position = CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID;

            NetworkServer.Spawn(_dungeon);
            var _dunScript = _dungeon.GetComponent<CGDungeon>();
            _dunScript.ID = CGDungeonCreator.curID;

            foreach (var mem in partyMembers)
            {
                Utils.FindPlayerFromName(mem.name).JoinDungeon(_dunScript);
            }

            CGDungeonCreator.curID++;

            return;
        }

        if(partyMembers.Count> 1)
        {
            if (level < dun.RequiredLevel)
            {
                TargetSendDungeonMessage(connectionToClient, "Min level for this dungeon is " + dun.RequiredLevel + ". You are not strong.", Color.red);

                return;
            }

            if (gold < dun.RequiredGold)
            {
                TargetSendDungeonMessage(connectionToClient, "Need " + dun.RequiredGold + " gold for this dungeon. Your money is not enought.", Color.red);
                
                return;
            }

            if (dun.reqItems.Count > 0)
            {
                List<bool> itemsExist = new List<bool>();

                foreach (var item in dun.reqItems)
                {
                    if (InventoryCountAmount(item.name) > 0)
                    {
                        itemsExist.Add(true);
                    }
                    else
                    {
                        itemsExist.Add(false);
                    }
                }

                if (itemsExist.Contains(false))
                {
                    TargetSendDungeonMessage(connectionToClient, "Not Enought Item", Color.red);

                    return;
                }
            }
            foreach (var item in dun.reqItems)
            {
                InventoryRemoveAmount(item.name, 1);
            }

            gold -= dun.RequiredGold;

            var _dungeon = GameObject.Instantiate(dun.gameObject, CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID, Quaternion.identity);
            _dungeon.transform.position = CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID;

            NetworkServer.Spawn(_dungeon);
            var _dunScript = _dungeon.GetComponent<CGDungeon>();
            _dunScript.ID = CGDungeonCreator.curID;

            foreach (var mem in partyMembers)
            {
                Utils.FindPlayerFromName(mem.name).JoinDungeon(_dunScript);
            }

            CGDungeonCreator.curID++;

            return;
        }
#endif
        if (level < dun.RequiredLevel)
            {
                TargetSendDungeonMessage(connectionToClient, "Min level for this dungeon is " + dun.RequiredLevel + ". You are not strong.", Color.red);
            
                return;
            }

            if (gold < dun.RequiredGold)
            {
                TargetSendDungeonMessage(connectionToClient, "Need " + dun.RequiredGold + " gold for this dungeon. Your money is not enought.", Color.red);

                return;
            }

            if (dun.reqItems.Count > 0)
            {
                List<bool> itemsExist = new List<bool>();

                foreach (var item in dun.reqItems)
                {
                    if (InventoryCountAmount(item.name) > 0)
                    {
                        itemsExist.Add(true);
                    }
                    else
                    {
                        itemsExist.Add(false);
                    }
                }

                if (itemsExist.Contains(false))
                {
                    TargetSendDungeonMessage(connectionToClient, "Not Enought Item", Color.red);
                    return;
                }
            }
            foreach (var item in dun.reqItems)
            {
                InventoryRemoveAmount(item.name, 1);
            }

            gold -= dun.RequiredGold;

            var dungeon = GameObject.Instantiate(dun.gameObject, CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID, Quaternion.identity);
            dungeon.transform.position = CGDungeonCreator.dungeonsStartPoint.transform.position + CGDungeonCreator.dungeonsStartPoint.transform.up * 2000 * CGDungeonCreator.curID;

            NetworkServer.Spawn(dungeon);
            var dunScript = dungeon.GetComponent<CGDungeon>();
            dunScript.ID = CGDungeonCreator.curID;

            JoinDungeon(dunScript);

            CGDungeonCreator.curID++;

            return;
    }

    public void JoinDungeon(CGDungeon dun)
    {
        dun.PlayerNames.Add(name);
        DungeonID = CGDungeonCreator.curID;
        BeforeDungeonPos = transform.position;
        TargetSendDungeonMessage(connectionToClient, "Enreting the dungeon", Color.green);
        StartCoroutine(TeleportPlayer(dun.StartPoint.transform.position));
    }

    [Command(channel = Channels.DefaultUnreliable)]
    public void CmdExitDungeon()
    {
        DungeonPos = transform.position;
        agent.Warp(BeforeDungeonPos);

        CGDungeon dun = FindObjectsOfType<CGDungeon>().ToList().Find(x => x.ID == DungeonID);
        dun.PlayerNames.Remove(name);       

        if(dun.PlayerNames.Count == 0)
        {
            NetworkServer.Destroy(dun.gameObject);
        }

        DungeonID = -1;
    }



    public IEnumerator TeleportPlayer(Vector3 pos)
    {
        Debug.Log("sa");
        yield return new WaitForSeconds(1);
        agent.Warp(pos);
    }

    [TargetRpc]
    public void TargetSendDungeonMessage(NetworkConnection target, string message, Color color)
    {
#if _UIInfoBox
        CGUIInfo.instance.AddMessage(new InfoText(message, color));
#else
        GetComponent<Chat>().AddMessageClient_DungeonCreator(new MessageInfo("", "", message, "", color));
#endif
    }
}

public partial class Chat
{
    [Client]
    public void AddMessageClient_DungeonCreator(MessageInfo mi)
    {
        FindObjectOfType<UIChat>().AddMessage(mi);
    }
}


public partial class Database
{

    static void Initialize_DungeonCreator()
    {

        ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS 'PlayerDungeonDatas' (
                            name TEXT NOT NULL PRIMARY KEY,
							x REAL NOT NULL,
                            y REAL NOT NULL,
                            z REAL NOT NULL,
                            dungeonID INTEGER NOT NULL
                            )");
    }

    static void CharacterLoad_DungeonCreator(Player player)
    {
        // any saved data for that slot?
        var table = ExecuteReader("SELECT x, y, z, dungeonID FROM PlayerDungeonDatas WHERE name=@name", new SqliteParameter("@name", player.name));
        if (table.Count == 1)
        {
            var row = table[0];
            float x = (float)row[0];
            float y = (float)row[1];
            float z = (float)row[2];
            int dungeonID = Convert.ToInt32((long)row[3]);

            CGDungeon dun = GameObject.FindObjectsOfType<CGDungeon>().ToList().Find(d => d.ID == dungeonID);
            if(dun == null)
            {
                player.BeforeDungeonPos = new Vector3(x, y, z);
                player.agent.Warp(new Vector3(player.BeforeDungeonPos.x, player.BeforeDungeonPos.y, player.BeforeDungeonPos.z));
                player.DungeonID = -1;
            }
            else
            {
                player.BeforeDungeonPos = new Vector3(x, y, z);
                player.DungeonID = dungeonID;
                player.DungeonPos = player.transform.position;
            }
        }
    }


    public static void CharacterSave_DungeonCreator(Player player)
    {
        ExecuteNonQuery("DELETE FROM PlayerDungeonDatas WHERE name=@name", new SqliteParameter("@name", player.name));

        if (player.DungeonID != -1)
        {
            ExecuteNonQuery("INSERT INTO PlayerDungeonDatas VALUES (@name, @x, @y, @z, @dungeonID)",
                                new SqliteParameter("@name", player.name),
                                new SqliteParameter("@x", player.BeforeDungeonPos.x),
                                new SqliteParameter("@y", player.BeforeDungeonPos.y),
                                new SqliteParameter("@z", player.BeforeDungeonPos.z),
                                new SqliteParameter("@dungeonID", player.DungeonID));
        }
    }
}

public partial class NetworkManagerMMO
{
    public static CGDungeonCreator dunCreator;

    public void OnStartServer_DungeonCreator()
    {
        dunCreator = new CGDungeonCreator();
        CGDungeonCreator.dungeonsStartPoint = GameObject.Find("DungeonsStartPoint");
    }

    public List<CGDungeon> Dungeons()
    {
        List<CGDungeon> _temp = new List<CGDungeon>();

        foreach (var dun in spawnPrefabs)
        {
            if (dun.GetComponent<CGDungeon>() != null)
            {
                _temp.Add(dun.GetComponent<CGDungeon>());
            }
        }
        return _temp;
    }


}
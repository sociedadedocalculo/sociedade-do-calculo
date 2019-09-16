using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Networking;

public class CGDungeon : NetworkBehaviour {

    
    public long ID = -1;

    public int RequiredLevel = 0;
    public int RequiredGold = 0;
    public List<ItemTemplate> reqItems = new List<ItemTemplate>();

    public GameObject StartPoint;
    public GameObject ExitNPCPoint;
    public GameObject ExitNPC;

    public List<string> PlayerNames = new List<string>();

    public List<DungeonSpawnDatas> spawnDatas = new List<DungeonSpawnDatas>();

#if _PartyAddon
    public bool RequireParty = false;
    public int minPartyMemberCount = 2;
    public int maxPartyMemberCount = 2;
#endif

    // Use this for initialization
    void Start () {
        if(isServer)
        {
            var exitNPC = Instantiate(ExitNPC, ExitNPCPoint.transform.position, ExitNPCPoint.transform.rotation);            
            exitNPC.transform.rotation = ExitNPCPoint.transform.rotation;
            exitNPC.transform.parent = transform;
            NetworkServer.Spawn(exitNPC);
            exitNPC.GetComponent<NavMeshAgent>().Warp(ExitNPCPoint.transform.position);

            foreach(var sp in spawnDatas)
            {
                if(sp.spawnPoint != null && sp.enemy != null)
                {
                    var mon = Instantiate(sp.enemy.gameObject, sp.spawnPoint.transform.position, sp.spawnPoint.transform.rotation);
                    mon .transform.rotation = sp.spawnPoint.transform.rotation;
                    mon .transform.parent = transform;
                    NetworkServer.Spawn(mon);
                    mon .GetComponent<NavMeshAgent>().Warp(sp.spawnPoint.transform.position);
                }
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

[System.Serializable]
public class DungeonSpawnDatas
{
    public GameObject spawnPoint;
    public Entity enemy;
}
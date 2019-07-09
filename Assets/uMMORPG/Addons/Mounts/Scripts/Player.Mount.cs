using UnityEngine;
using UnityEngine.Networking;

public partial class Player  {

    [Header("Mount Stuff")]

    [SerializeField]
	public GameObject modelForMount;

	[HideInInspector]
	public Mount mount
	{
		get;
		private set;
	}

	[HideInInspector]
	public GameObject mountPrefab;
	[HideInInspector]
	public float speed;
	[HideInInspector]
	public float mountDeathAnimLength;
	[HideInInspector]
	public bool MOUNTED;

	[HideInInspector]
	public bool mountDead;
	private TimeCounter mountDeathCounter;
    private Vector3 prevModel;
    private float prevSpeed;
    private bool mounting;

#if UMMORPG_CHARACTER_CREATION
    void Start_Mount()
    {
        modelForMount = transform.Find("3D_Model").gameObject;
        if (modelForMount == null) Debug.LogWarning("Rename your model for your players to 3D_Model so that I can find your player's model.");
    }
#endif

	[Server]
	public void UnmountPlayer()
	{
        if (!mount) return;
        mounting = false;
		modelForMount.transform.localPosition = prevModel;
		agent.speed = prevSpeed;
		RpcClientUnmount();
		OnDeath_Mount();
		this.MOUNTED = false;
#if UMMORPG_PETS
		if (pet) pet.agent.speed = agent.speed;
#endif
	}

    [Server]
	public void mountPlayer()
    {
        var mountSpawn = Instantiate(mountPrefab, transform.position, transform.rotation, transform);
        NetworkServer.Spawn(mountSpawn);
        mount = mountSpawn.GetComponent<Mount>();
        mount.owner = this;
        mountDead = false;
        mounting = true;
        prevSpeed = agent.speed;
        prevModel = modelForMount.transform.localPosition;
        modelForMount.transform.position = mount.seat.transform.position;
		agent.speed *= speed;
        RpcClientMount(mountSpawn);
		this.MOUNTED = true;
#if UMMORPG_PETS
        if (pet) pet.agent.speed = agent.speed;
#endif
    }

    [Server]
	public void OnDeath_Mount()
	{
        if (!mount) return;
        mount.dead = true;
		mountDead = true;
		mountDeathCounter = new TimeCounter(mountDeathAnimLength);
		RpcClientOnDeath_Mount();
	}

//    [Command]
//    void CmdUnmount() {
//        if (aggroed) UnmountPlayer();
//    }

	[ClientRpc]
	void RpcClientMount(GameObject mountSpawn)
	{
        if (!mount) return;
		mountSpawn.transform.SetParent(transform);
		mountSpawn.transform.rotation = transform.rotation;
		mount = mountSpawn.GetComponent<Mount>();
		mount.owner = this;
		mountDead = false;
		mounting = true;
		modelForMount.transform.position = mount.seat.transform.position;
	}

    [ClientRpc]
    void RpcClientOnDeath_Mount() 
    {
		mount.dead = true;
		mountDead = true;
		mountDeathCounter = new TimeCounter(mountDeathAnimLength);
    }

    [ClientRpc]
    void RpcClientUnmount() 
    {
        mounting = false;
        modelForMount.transform.localPosition = prevModel;
        agent.speed = prevSpeed;
    }

//    [Client]
//    void UpdateClient_Mount() {
//        if (aggroed) CmdUnmount();
//    }

	void LateUpdate_Mount()
	{
		if (isClient && mount) animator.SetBool("MOUNTING", mounting);

		if (mountDead && mountDeathCounter.ready)
		{
			try
			{
				NetworkServer.Destroy(mount.gameObject);
				mountDeathCounter = new TimeCounter(mountDeathAnimLength);
				mount = null;
				mountDead = false;
			}
			catch { }
		}
	}

	void OnClientDisconnect_Mount()
	{
		if (mount != null) NetworkServer.Destroy(mount.collision.gameObject);
	}

}

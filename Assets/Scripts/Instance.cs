// Instance for instanced dungeons, etc.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

// requires NavMeshSurface so that we can duplicate and move it at runtime.
[RequireComponent(typeof(NavMeshSurface))]
public class Instance : MonoBehaviour
{
    [Header("Instance Definition")]
    public Transform entry;
    public int requiredLevel = 1;
    [HideInInspector] public Bounds bounds;

    [Tooltip("Only allow so many instances of this type to protect server resources.")]
    public int instanceLimit = 5;

    // all the instances that are based on this template.
    // dict <partyId, Instance>
    [HideInInspector] public Dictionary<int, Instance> instances = new Dictionary<int, Instance>();

    [Header("Party Check")]
    public LayerMask playerLayers = ~0; // Everything by default. Make sure to select 'Player' layer.
    public float partyCheckInterval = 30;
    double nextPartyCheckTime;
    bool localPlayerEnteredYet;

    // cache SpawnPoints so we don't have to call GetComponentsInChildren for
    // each new instance. automatically assigned in OnValidate!
    // => we can't keep scene monsters in there when instantiating because we
    //    would have duplicate sceneIds/netIds, which would get everything out
    //    of sync.
    //    (force overwriting sceneId to 0 can be done, but the duplicates still
    //     have 0 netIds. either way it's too hacky.)
    // => this way we also don't waste server resources because we don't create
    //    hundreds of monsters per instance template on start. only when needed.
    [Header("Spawn Points Cache")]
    public InstanceSpawnPoint[] spawnPoints;

    // reference to original instance template that this instance was created
    // from. useful to remove ourselves from the template's instance list later.
    Instance template;

    // the party that owns this instance. 0 means none.
    int partyId = 0;

    void Awake()
    {
        // no NetworkServer.active check here because it needs to work on Client
        // too.

        // calculate bounds of all child renderers once
        bounds = Utils.CalculateBoundsForAllRenderers(gameObject);
    }

    void OnValidate()
    {
        // no NetworkServer.active check here because it needs to work in Editor.

        // cache spawnpoints
        spawnPoints = GetComponentsInChildren<InstanceSpawnPoint>();

        // make sure that no child object is marked as 'static'. static objects
        // can't be duplicated and moved, which is what we need for instances.
        // (otherwise moved meshes might appear empty because they are still on
        //  the old position (static)).
        foreach (Transform tf in GetComponentsInChildren<Transform>())
            if (tf.gameObject.isStatic)
                Debug.LogWarning("Instance child " + tf.name + " shouldn't be static. It needs to be duplicated and moved to other positions when duplicating instances.");
    }

    HashSet<Player> FindAllPlayersInInstanceBounds()
    {
        // HashSet so we don't accidentally add a player twice in case the cast
        // detected two of his colliders.
        HashSet<Player> result = new HashSet<Player>();
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation, playerLayers);
        if (colliders != null)
        {
            foreach (Collider co in colliders)
            {
                Player player = co.GetComponentInParent<Player>();
                if (player != null)
                    result.Add(player);
            }
        }
        return result;
    }

    void DestroyAllNetworkIdentitiesInInstanceBounds()
    {
        Collider[] colliders = Physics.OverlapBox(bounds.center, bounds.extents, transform.rotation);
        if (colliders != null)
        {
            foreach (Collider co in colliders)
            {
                NetworkIdentity identity = co.GetComponentInParent<NetworkIdentity>();
                if (identity != null)
                    NetworkServer.Destroy(identity.gameObject);
            }
        }
    }

    void Update()
    {
        // run destroy checks if this is an instance with a valid party id
        // (not a template that lives in the scene)
        if (partyId > 0)
        {
            // server checks containing members in an interval
            if (NetworkServer.active)
            {
                // interval elapsed?
                if (NetworkTime.time >= nextPartyCheckTime)
                {
                    // find all players within instance bounds.
                    // players might be in this instance for all kinds of right and
                    // wrong reasons:
                    // * might be in the instance party
                    // * might have left the instance party
                    // * might have relogged and accidentally spawned in another party's
                    //   instance
                    HashSet<Player> playersInInstanceBounds = FindAllPlayersInInstanceBounds();

                    int playersRemaining = 0;
                    foreach (Player player in playersInInstanceBounds)
                    {
                        // in party for this instance? then count
                        if (player.party.partyId == partyId)
                        {
                            ++playersRemaining;
                        }
                        // otherwise kick from instance
                        else
                        {
                            Transform spawn = ((NetworkManagerMMO)NetworkManager.singleton).GetStartPositionFor(player.className);
                            player.agent.Warp(spawn.position);
                            Debug.Log("Removed player " + player.name + " with partyId=" + player.party.partyId + " from instance " + name + " with partyId=" + partyId);
                        }
                    }

                    // is no one is left then destroy the instance
                    if (playersRemaining == 0)
                    {
                        // destroy self
                        // TODO when does the client destroy it?
                        // TODO call networkserver.destroy for containing monsters, or not?
                        // TODO the monsters remain. either parent them when spawning, or destroy manually here.
                        Destroy(gameObject);
                        Debug.Log("Instance " + name + " destroyed because no members of party " + partyId + " are in it anymore.");
                    }

                    // reset interval
                    nextPartyCheckTime = NetworkTime.time + partyCheckInterval;
                }
            }
            // client creates instance when entering and destroys it when leaving.
            // simple as that.
            // => we need to wait until the player entered the instance at least
            //    once though. otherwise we might destroy it immediately while
            //    the server's agent.warp packet is still being delivered to the
            //    client.
            else if (Player.localPlayer != null)
            {
                // has the player entered the instance yet?
                if (bounds.Contains(Player.localPlayer.transform.position))
                {
                    localPlayerEnteredYet = true;
                }
                // not in instance anymore, but entered before?
                else if (localPlayerEnteredYet)
                {
                    Destroy(gameObject);
                    Debug.Log("Instance " + name + " destroyed for local player because he left the instance.");
                }
            }
        }
    }

    void OnDestroy()
    {
        // no NetworkServer.active check here because we want to remove it from
        // the instance list on client too!

        // is this an instance of a template? then remove from template's list
        // to free limits
        if (template != null)
            template.instances.Remove(partyId);

        // if server then despawn all networkidentities properly
        // -> there could be initially spawned monsters
        // -> there could be additional spawns like monster scroll spawns
        // => simply despawn any NetworkIdentity in bounds to be sure!
        if (NetworkServer.active)
            DestroyAllNetworkIdentitiesInInstanceBounds();
    }

    // create a new instance for a party
    // -> this has to be called on the server AND on the client.
    //    they both instantiate based on a template because we can't
    //    NetworkServer.Spawn the instance (it might contain monsters, and a
    //    NetworkIdentity can't be a child of another NetworkIdentity).
    public static Instance CreateInstance(Instance template, int partyId)
    {
        // instance for this party not created yet?
        if (!template.instances.ContainsKey(partyId))
        {
            // instance limit for this template not reached yet?
            if (template.instances.Count < template.instanceLimit)
            {
                // z-offset:
                // -> bounds.size works perfectly fine
                // -> + visRange so we don't waste bandwidth if two dungeons are so
                //      close that the proximity checkers would broadcast to both.
                // -> * partyId so they aren't inside each other. using partyId
                //      makes sure that the client knows where to instantiate it
                //      too. if we were to use template.instances.Count then
                //      the client would have to know about that value with a
                //      custom NetworkMessages. this would be very complicated.
                //      => using partyId guarantees that no one can ever log back
                //         into the game and spawn in another party's dungeon that
                //         is currently where he logged out before
                //      => if floating point precision becomes an issue then we
                //         can still recycle party ids in PartySystem.cs!
                float zOffset = (template.bounds.size.z + NetworkProximityGridChecker.visRange) * partyId;
                Debug.Log("Creating " + template.name + " Instance with zOffset=" + zOffset);

                // instantiate it
                Vector3 position = template.transform.position + new Vector3(0, 0, zOffset);
                GameObject go = Instantiate(template.gameObject, position, template.transform.rotation);
                Instance instance = go.GetComponent<Instance>();
                instance.template = template;

                // set party id
                // note: Update will start box casting immediately, but there is
                //       no race condition because the portal that calls
                //       CreateInstance will move a player into it immediately.
                //       (before next Update is called)
                instance.partyId = partyId;

                // add to list of the template's instances
                template.instances[partyId] = instance;

                // server spawns NetworkIdentities at spawn points
                if (NetworkServer.active && instance.spawnPoints != null)
                {
                    // spawn each of them
                    foreach (InstanceSpawnPoint spawnPoint in instance.spawnPoints)
                    {
                        GameObject spawned = Instantiate(spawnPoint.prefab.gameObject, spawnPoint.transform.position, spawnPoint.transform.rotation);
                        spawned.name = spawnPoint.prefab.name; // avoid "(Clone)"
                        NetworkServer.Spawn(spawned);
                    }
                }

                // return the instance
                return instance;
            }
        }
        else Debug.LogWarning("Instance " + template.name + " was already created for partyId=" + partyId + ". This should never happen.");

        return null;
    }
}

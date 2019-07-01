// UNET's current NetworkTransform is really laggy, so we make it smooth by
// simply synchronizing the agent's destination. We could also lerp between
// the transform positions, but this is much easier and saves lots of bandwidth.
//
// Using a NavMeshAgent also has the benefit that no rotation has to be synced
// while moving.
//
// Notes:
//
// - Teleportations have to be detected and synchronized properly
// - Caching the agent won't work because serialization sometimes happens
//   before awake/start
// - We also need the stopping distance, otherwise entities move too far.
using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
// unreliable is enough and we want
// to send changes immediately. everything else causes lags.
[NetworkSettings(channel = Channels.DefaultUnreliable, sendInterval = 0)]
public class NetworkNavMeshAgent : NetworkBehaviour
{
    public NavMeshAgent agent; // assign in Inspector (instead of GetComponent)
    Vector3 lastDestination; // for dirty bit
    Vector3 lastVelocity; // for dirty bit
    bool hadPath = false; // had path since last time? for warp detection
    Vector3 requiredVelocity; // to apply received velocity in Update constanly

    // look at a transform while only rotating on the Y axis (to avoid weird
    // tilts)
    public void LookAtY(Vector3 position)
    {
        transform.LookAt(new Vector3(position.x, transform.position.y, position.z));
    }

    void Update()
    {
        if (isServer)
        {
            // find out if destination changed on server
            if (agent.hasPath || agent.pathPending) hadPath = true;
            if (agent.destination != lastDestination || agent.velocity != lastVelocity)
                SetDirtyBit(1);
        }
        else if (isClient)
        {
            // apply velocity constantly, not just in OnDeserialize
            // (not on host because server handles it already anyway)
            if (requiredVelocity != Vector3.zero)
            {
                agent.ResetPath(); // needed after click movement before we can use .velocity
                agent.velocity = requiredVelocity;
                LookAtY(transform.position + requiredVelocity); // velocity doesn't set rotation
            }
        }
    }

    // server-side serialization
    //
    // I M P O R T A N T
    //
    // always read and write the same amount of bytes. never let any errors
    // happen. otherwise readstr/readbytes out of range bugs happen.
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        // click based movement needs destination. wasd needs velocity. we send
        // everything just to be sure and to deserialize more easily.
        writer.Write(transform.position); // for rubberbanding
        writer.Write(agent.speed);
        writer.Write(agent.stoppingDistance);
        writer.Write(agent.destination);
        writer.Write(agent.velocity);
        writer.Write(agent.hasPath); // for click/wasd detection
        writer.Write(agent.destination != lastDestination && !hadPath); // warped? avoid sliding to respawn point etc.

        // reset helpers
        lastDestination = agent.destination;
        lastVelocity = agent.velocity;
        hadPath = false;

        return true;
    }

    // client-side deserialization
    //
    // I M P O R T A N T
    //
    // always read and write the same amount of bytes. never let any errors
    // happen. otherwise readstr/readbytes out of range bugs happen.
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        Vector3 position = reader.ReadVector3();
        agent.speed = reader.ReadSingle();
        agent.stoppingDistance = reader.ReadSingle();
        Vector3 destination = reader.ReadVector3();
        Vector3 velocity = reader.ReadVector3();
        bool hasPath = reader.ReadBoolean();
        bool warped = reader.ReadBoolean();

        // OnDeserialize must always return so that next one is called too
        try
        {
            // only try to use agent while on navmesh
            // (it might not be while falling from the sky after joining)
            if (agent.isOnNavMesh)
            {
                // warp if necessary. distance check to filter out false positives
                if (warped && Vector3.Distance(transform.position, position) > agent.radius)
                    agent.Warp(position); // to pos is always smoother

                // rubberbanding: if we are too far off because of a rapid position
                // change or latency, then warp
                // -> agent moves 'speed' meter per seconds
                // -> if we are 2 speed units behind, then we teleport
                //    (using speed is better than using a hardcoded value)
                if (Vector3.Distance(transform.position, position) > agent.speed * 2)
                    agent.Warp(position);

                // click or wasd movement?
                if (hasPath)
                {
                    // set destination afterwards, so that we never stop going there
                    // even after being warped etc.
                    agent.destination = destination;
                    requiredVelocity = Vector3.zero; // reset just to be sure
                }
                else
                {
                    // apply required velocity in Update later
                    requiredVelocity = velocity;
                }
            }
        }
        catch { }
    }
}

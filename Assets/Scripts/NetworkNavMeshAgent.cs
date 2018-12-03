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
[NetworkSettings(sendInterval=0.1f)]
public class NetworkNavMeshAgent : NetworkBehaviour
{
    public NavMeshAgent agent; // assign in Inspector (instead of GetComponent)
    Vector3 requiredVelocity; // to apply received velocity in Update constanly

    // remember last serialized values for dirty bit
    Vector3 lastUpdatePosition;
    Vector3 lastSerializedDestination;
    Vector3 lastSerializedVelocity;

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
            // detect move mode
            bool hasPath = agent.hasPath || agent.pathPending; // might still be computed

            // click movement and destination changed since last sync?
            if (hasPath && agent.destination != lastSerializedDestination)
            {
                //Debug.LogWarning(name + " dirty because destination changed from: " + lastSerializedDestination + " to " + agent.destination + " hasPath=" + agent.hasPath + " pathPending=" + agent.pathPending);
                SetDirtyBit(1);
            }
            // wasd movement and velocity changed since last sync?
            else if (!hasPath && agent.velocity != lastSerializedVelocity)
            {
                //Debug.LogWarning(name + " dirty because velocity changed from: " + lastVelocity + " to " + agent.velocity);
                SetDirtyBit(1);
            }
            // neither click or wasd movement, but position changed further than 'speed'?
            // then we must have teleported, no other way to move this fast.
            else if (!hasPath && Vector3.Distance(transform.position, lastUpdatePosition) > agent.speed)
            {
                // warp detection is just about 100% correct, so let's send a
                // Rpc to warp the client and not leave it up to OnDeserialize's
                // guess wether or not we warped. this is worth it for corerctness.
                //Debug.Log(name + " teleported from: " + lastUpdatePosition + " to: " + transform.position);
                RpcWarped(transform.position);
            }

            lastUpdatePosition = transform.position;
        }
        else if (isClient)
        {
            // apply velocity constantly, not just in OnDeserialize
            // (not on host because server handles it already anyway)
            if (requiredVelocity != Vector3.zero)
            {
                agent.ResetMovement(); // needed after click movement before we can use .velocity
                agent.velocity = requiredVelocity;
                LookAtY(transform.position + requiredVelocity); // velocity doesn't set rotation
            }
        }
    }

    [ClientRpc]
    public void RpcWarped(Vector3 position)
    {
        agent.Warp(position);
    }

    // server-side serialization
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        // always send position so client knows if he's too far off and needs warp
        writer.Write(transform.position);

        // always send speed in case it's modified by something
        writer.Write(agent.speed);

        // click or wasd movement?
        // (no need to send everything all the time, saves bandwidth)
        bool hasPath = agent.hasPath || agent.pathPending;
        writer.Write(hasPath);
        if (hasPath)
        {
            // destination
            writer.Write(agent.destination);

            // always send stopping distance because monsters might stop early etc.
            writer.Write(agent.stoppingDistance);

            // remember last serialized path so we do it again if it changed.
            // (first OnSerialize never seems to detect path yet for whatever
            //  reason, so this way we can be 100% sure that it's called again
            //  as soon as the path was detected)
            lastSerializedDestination = agent.destination;
        }
        else
        {
            // velocity
            writer.Write(agent.velocity);

            // remember last serialized velocity
            lastSerializedVelocity = agent.velocity;
        }
        return true;
    }

    // client-side deserialization
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        // read position, speed and movement type
        Vector3 position = reader.ReadVector3();
        agent.speed = reader.ReadSingle();
        bool hasPath = reader.ReadBoolean();

        // click or wasd movement?
        if (hasPath)
        {
            // read destination and stopping distance
            Vector3 destination = reader.ReadVector3();
            float stoppingDistance = reader.ReadSingle();
            //Debug.Log("OnDeserialize: click: " + destination);

            // try setting destination if on navmesh
            // (might not be while falling from the sky after joining etc.)
            if (agent.isOnNavMesh)
            {
                agent.stoppingDistance = stoppingDistance;
                agent.destination = destination;
            }
            else Debug.LogWarning("NetworkNavMeshAgent.OnSerialize: agent not on NavMesh, name=" + name + " position=" + transform.position + " destination=" + destination);

            requiredVelocity = Vector3.zero; // reset just to be sure
        }
        else
        {
            // read velocity
            Vector3 velocity = reader.ReadVector3();
            //Debug.Log("OnDeserialize: wasd: " + velocity);

            // apply required velocity in Update later
            requiredVelocity = velocity;
        }

        // rubberbanding: if we are too far off because of a rapid position
        // change or latency, then warp
        // -> agent moves 'speed' meter per seconds
        // -> if we are speed * 2 units behind, then we teleport
        //    (using speed is better than using a hardcoded value)
        // -> we use speed * 2 for update/network latency tolerance. player
        //    might have moved quit a bit already before OnSerialize was called
        //    on the server.
        if (Vector3.Distance(transform.position, position) > agent.speed * 2 && agent.isOnNavMesh)
        {
            agent.Warp(position);
            //Debug.Log(name + " rubberbanding to " + position);
        }
    }
}

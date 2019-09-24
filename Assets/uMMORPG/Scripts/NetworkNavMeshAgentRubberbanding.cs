// Rubberband navmesh movement.
//
// How it works:
// - local player sends new position to server every 100ms
// - server validates the move
// - server broadcasts it to other clients
//   - clients apply it via agent.destination to get free interpolation
// - server also detects teleports to warp the client if needed
//
// The great part about this solution is that the client can move freely, but
// the server can still intercept with:
//   * agent.Warp()
//   * rubberbanding.ResetMovement()
// => all those calls are detected here and forced to the client.
//
// Note: no LookAtY needed because we move everything via .destination
// Note: only syncing .destination would save a lot of bandwidth, but it's way
//       too complicated to get right with both click AND wasd movement.
using UnityEngine;
using UnityEngine.AI;
using Mirror;

[RequireComponent(typeof(NavMeshAgent))]
public class NetworkNavMeshAgentRubberbanding : NetworkBehaviour
{
    public NavMeshAgent agent; // assign in Inspector (instead of GetComponent)
    public Entity entity;

    // remember last serialized values for dirty bit
    Vector3 lastServerPosition;
    Vector3 lastSentPosition;
    double lastSentTime; // double for long term precision

    // epsilon for float/vector3 comparison (needed because of imprecision
    // when sending over the network, etc.)
    const float epsilon = 0.1f;

    // validate a move (the 'rubber' part)
    bool ValidateMove(Vector3 position)
    {
        // there is virtually no way to cheat navmesh movement, since it will
        // never calcluate a path to a point that is not on the navmesh.
        // -> we only need to check if alive
        // -> and need to be IDLE or MOVING
        //    -> not while CASTING. the FSM resets path, but we don't event want
        //       to start it here. otherwise wasd movement could move a tiny bit
        //       while CASTING if Cmd sets destination and Player.UpateCASTING
        //       only resets it next frame etc.
        //    -> not while STUNNED.
        // -> maybe a distance check in case we get too far off from latency
        return entity.health > 0 &&
               (entity.State == "IDLE" || entity.State == "MOVING");
    }

    [Command]
    void CmdMoved(Vector3 position)
    {
        // rubberband (check if valid move)
        if (ValidateMove(position))
        {
            // set position via .destination to get free interpolation
            agent.stoppingDistance = 0;
            agent.destination = position;

            // set dirty to trigger a OnSerialize next time, so that other clients
            // know about the new position too
            SetDirtyBit(1);
        }
        else
        {
            // otherwise keep current position and set dirty so that OnSerialize
            // is trigger. it will warp eventually when getting too far away.
            SetDirtyBit(1);
        }
    }

    void Update()
    {
        // server should detect teleports / react if we got too far off
        // do this BEFORE isLocalPlayer actions so that agent.ResetPath can be
        // detected properly? otherwise localplayer wasdmovement cmd may
        // overwrite it
        if (isServer)
        {
            // position changed further than 'speed'?
            // then we must have teleported, no other way to move this fast.
            if (Vector3.Distance(transform.position, lastServerPosition) > agent.speed)
            {
                // set NetworkNavMeshAgent dirty so that onserialize is
                // triggered and the client receives the position change
                SetDirtyBit(1);
                //Debug.LogWarning(name + "(local=" + isLocalPlayer + ") teleported!");
            }

            lastServerPosition = transform.position;
        }

        // local player can move freely. detect position changes.
        if (isLocalPlayer)
        {
            // check position change every send interval
            if (NetworkTime.time >= lastSentTime + syncInterval &&
                Vector3.Distance(transform.position, lastSentPosition) > epsilon)
            {
                // host sets dirty without cmd/overwriting destination/etc.
                if (isServer)
                    SetDirtyBit(1);
                // client sends to server to broadcast/set destination/etc.
                else
                    CmdMoved(transform.position);

                lastSentTime = NetworkTime.time;
                lastSentPosition = transform.position;
            }
        }
    }

    // force reset movement on localplayer
    // => always call rubberbanding.ResetMovement instead of agent.ResetMovement
    //    when using Rubberbanding.
    // => there is no decent way to detect .ResetMovement on server while doing
    //    rubberband movement on client. it would always lead to false positives
    //    and accidental resets. this is the 100% safe way to do it here.
    [Server]
    public void ResetMovement()
    {
        // force reset on target
        TargetResetMovement(transform.position);

        // set dirty so onserialize notifies others
        SetDirtyBit(1);
    }

    // note: with rubberband movement, the server's player position always lags
    //       behind a bit. if server resets movement and then tells client to
    //       reset it, client will reset it while already behind ahead.
    // => solution: include reset position so we don't get out of sync.
    // -> if local player moves to B then player position on server is always
    //    a bit behind. if server resets movement then the player will stop
    //    abruptly where it is on server and on client, which is not the same
    //    yet. we need to stay in sync.
    [TargetRpc]
    void TargetResetMovement(Vector3 resetPosition)
    {
        // reset path and velocity
        //Debug.LogWarning(name + "(local=" + isLocalPlayer + ") TargetResetMovement @ " + resetPosition);
        agent.ResetMovement();
        agent.Warp(resetPosition);
    }

    // server-side serialization
    // used for the server to broadcast positions to other clients too
    public override bool OnSerialize(NetworkWriter writer, bool initialState)
    {
        // always send position so client knows if he's too far off and needs warp
        // we also need it for wasd movement anyway
        writer.WriteVector3(transform.position);

        // always send speed in case it's modified by something
        writer.WriteSingle(agent.speed);

        // note: we don't send stopping distance because we always use '0' here
        // (because we always send the latest position every sendInterval)
        return true;
    }

    // client-side deserialization
    public override void OnDeserialize(NetworkReader reader, bool initialState)
    {
        // read position, speed, movement type in any case, so that we read
        // exactly what we write
        Vector3 position = reader.ReadVector3();
        float speed = reader.ReadSingle();

        // we can only apply the position if the agent is on the navmesh
        // (might not be while falling from the sky after joining, etc.)
        if (agent.isOnNavMesh)
        {
            // we can only move the agent to a position that is on the navmesh.
            // (might not if the agent walked into an instance, server
            //  broadcasted the new position to us, and proximity checker hasn't
            //  yet realized that the agent is out of sight. so it's not
            //  destroyed yet)
            // => 0.1f distance for network imprecision that might happen.
            // => if it happens when we simply do nothing and hope that the next
            //    update will be on a navmesh again.
            //    (if we were to Destroy it then we might get out of sync if
            //     the agent comes back out of the instance and was in proximity
            //     range the whole time)
            // NOTE: we *could* also call agent.proxchecker.Hide() and later
            //       Show() if agent is on a valid navmesh again. but let's keep
            //       the agents in front of the portal instead so we see what's
            //       happening. it's highly unlikely that an instance will be in
            //       proximity range of a player not in that instance anyway.
            NavMeshHit hit;
            if (NavMesh.SamplePosition(position, out hit, 0.1f, NavMesh.AllAreas))
            {
                // ignore for local player since he can move freely
                if (!isLocalPlayer)
                {
                    agent.stoppingDistance = 0;
                    agent.speed = speed;
                    agent.destination = position;
                }

                // rubberbanding: if we are too far off because of a rapid position
                // change or latency or server side teleport, then warp
                // -> agent moves 'speed' meter per seconds
                // -> if we are speed * 2 units behind, then we teleport
                //    (using speed is better than using a hardcoded value)
                // -> we use speed * 2 for update/network latency tolerance. player
                //    might have moved quit a bit already before OnSerialize was called
                //    on the server.
                if (Vector3.Distance(transform.position, position) > agent.speed * 2 && agent.isOnNavMesh)
                {
                    agent.Warp(position);
                    //Debug.LogWarning(name + "(local=" + isLocalPlayer + ") rubberbanding to " + position);
                }
            }
            else Debug.Log("NetworkNavMeshAgent.OnDeserialize: new position not on NavMesh, name=" + name + " new position=" + position + ". This could happen if the agent was warped to a dungeon instance that isn't on the local player.");
        }
        else Debug.LogWarning("NetworkNavMeshAgent.OnDeserialize: agent not on NavMesh, name=" + name + " position=" + transform.position + " new position=" + position);
    }
}

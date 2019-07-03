using UnityEngine;
using Mirror;

[RequireComponent(typeof(Animator))]
public partial class Mount : Summonable
{
    [Header("Death")]
    public float deathTime = 2; // enough for animation
    double deathTimeEnd; // double for long term precision

    [Header("Seat Position")]
    public Transform seat;

    // networkbehaviour ////////////////////////////////////////////////////////
    protected override void Awake()
    {
        base.Awake();

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "Awake_");
    }

    public override void OnStartServer()
    {
        // call Entity's OnStartServer
        base.OnStartServer();

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "OnStartServer_");
    }

    protected override void Start()
    {
        base.Start();

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "Start_");
    }

    void LateUpdate()
    {
        // pass parameters to animation state machine
        // => passing the states directly is the most reliable way to avoid all
        //    kinds of glitches like movement sliding, attack twitching, etc.
        // => make sure to import all looping animations like idle/run/attack
        //    with 'loop time' enabled, otherwise the client might only play it
        //    once
        // => only play moving animation while the owner is actually moving. the
        //    MOVING state might be delayed to due latency or we might be in
        //    MOVING while a path is still pending, etc.
        if (isClient) // no need for animations on the server
        {
            // use owner's moving state for maximum precision (not if dead)
            // (if owner spawn reached us yet)
            animator.SetBool("MOVING", health > 0 && owner != null && owner.IsMoving());
            animator.SetBool("DEAD", state == "DEAD");
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "LateUpdate_");
    }

    void OnDestroy()
    {
        // Unity bug: isServer is false when called in host mode. only true when
        // called in dedicated mode. so we need a workaround:
        if (NetworkServer.active) // isServer
        {
            // keep player's pet item up to date
            SyncToOwnerItem();
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "OnDestroy_");
    }

    // always update pets. IsWorthUpdating otherwise only updates if has observers,
    // but pets should still be updated even if they are too far from any observers,
    // so that they continue to run to their owner.
    public override bool IsWorthUpdating() { return true; }

    // copy owner's position and rotation. no need for NetworkTransform.
    void CopyOwnerPositionAndRotation()
    {
        if (owner != null)
        {
            agent.Warp(owner.transform.position);
            transform.rotation = owner.transform.rotation;
        }
    }

    // finite state machine events /////////////////////////////////////////////
    bool EventOwnerDisappeared()
    {
        return owner == null;
    }

    bool EventOwnerDied()
    {
        return owner != null && owner.health == 0;
    }

    bool EventDied()
    {
        return health == 0;
    }

    bool EventDeathTimeElapsed()
    {
        return state == "DEAD" && NetworkTime.time >= deathTimeEnd;
    }

    // finite state machine - server ///////////////////////////////////////////
    [Server]
    string UpdateServer_IDLE()
    {
        // copy owner's position and rotation. no need for NetworkTransform.
        CopyOwnerPositionAndRotation();

        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventOwnerDied())
        {
            // die if owner died, so the mount doesn't stand around there forever
            health = 0;
        }
        if (EventDied())
        {
            // we died.
            OnDeath();
            return "DEAD";
        }
        if (EventDeathTimeElapsed()) {} // don't care

        return "IDLE"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_DEAD()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "DEAD";
        }
        if (EventDeathTimeElapsed())
        {
            // we were lying around dead for long enough now.
            // hide while respawning, or disappear forever
            NetworkServer.Destroy(gameObject);
            return "DEAD";
        }
        if (EventOwnerDied()) {} // don't care
        if (EventDied()) {} // don't care, of course we are dead

        return "DEAD"; // nothing interesting happened
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")    return UpdateServer_IDLE();
        if (state == "DEAD")    return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient()
    {
        if (state == "IDLE" || state == "MOVING")
        {
            // copy owner's position and rotation. no need for NetworkTransform.
            CopyOwnerPositionAndRotation();
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "UpdateClient_");
    }

    // death ///////////////////////////////////////////////////////////////////
    [Server]
    protected override void OnDeath()
    {
        // take care of entity stuff
        base.OnDeath();

        // set death end time. we set it now to make sure that everything works
        // fine even if a pet isn't updated for a while. so as soon as it's
        // updated again, the death/respawn will happen immediately if current
        // time > end time.
        deathTimeEnd = NetworkTime.time + deathTime;

        // keep player's pet item up to date
        SyncToOwnerItem();

        // addon system hooks
        Utils.InvokeMany(typeof(Mount), this, "OnDeath_");
    }

    // skills //////////////////////////////////////////////////////////////////
    public override bool CanAttack(Entity entity) { return false; }
}

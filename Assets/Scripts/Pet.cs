using System;
using UnityEngine;
using Mirror;
using TMPro;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(NetworkNavMeshAgent))]
public partial class Pet : Summonable
{
    [Header("Text Meshes")]
    public TextMeshPro ownerNameOverlay;

    [Header("Experience")] // note: int is not enough (can have > 2 mil. easily)
    public int maxLevel = 1;
    [SyncVar, SerializeField] long _experience = 0;
    public long experience
    {
        get { return _experience; }
        set
        {
            if (value <= _experience)
            {
                // decrease
                _experience = Math.Max(value, 0);
            }
            else
            {
                // increase with level ups
                // set the new value (which might be more than expMax)
                _experience = value;

                // now see if we leveled up (possibly more than once too)
                // (can't level up if already max level)
                // (can only level up to owner level so he can still summon it.)
                while (_experience >= experienceMax && level < maxLevel && level < owner.level)
                {
                    // subtract current level's required exp, then level up
                    _experience -= experienceMax;
                    ++level;

                    // keep player's pet item up to date
                    SyncToOwnerItem();

                    // addon system hooks
                    Utils.InvokeMany(typeof(Pet), this, "OnLevelUp_");
                }

                // set to expMax if there is still too much exp remaining
                if (_experience > experienceMax) _experience = experienceMax;
            }
        }
    }

    // required experience grows by 10% each level (like Runescape)
    [SerializeField] protected ExponentialLong _experienceMax = new ExponentialLong { multiplier = 100, baseValue = 1.1f };
    public long experienceMax { get { return _experienceMax.Get(level); } }

    [Header("Movement")]
    public float returnDistance = 25; // return to player if dist > ...
    // pets should follow their targets even if they run out of the movement
    // radius. the follow dist should always be bigger than the biggest archer's
    // attack range, so that archers will always pull aggro, even when attacking
    // from far away.
    public float followDistance = 20;
    // pet should teleport if the owner gets too far away for whatever reason
    public float teleportDistance = 30;
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f; // move as close as 0.8 * attackRange to a target

    // use owner's speed if found, so that the pet can still follow the
    // owner if he is riding a mount, etc.
    public override float speed => owner != null ? owner.speed : base.speed;

    [Header("Death")]
    public float deathTime = 2; // enough for animation
    double deathTimeEnd; // double for long term precision

    [Header("Behaviour")]
    [SyncVar] public bool defendOwner = true; // attack what attacks the owner
    [SyncVar] public bool autoAttack = true; // attack what the owner attacks

    // the last skill that was casted, to decide which one to cast next
    int lastSkill = -1;

    // sync to item ////////////////////////////////////////////////////////////
    protected override ItemSlot SyncStateToItemSlot(ItemSlot slot)
    {
        // pet also has experience, unlike summonable. sync that too.
        slot = base.SyncStateToItemSlot(slot);
        slot.item.summonedExperience = experience;
        return slot;
    }

    // networkbehaviour ////////////////////////////////////////////////////////
    protected override void Awake()
    {
        base.Awake();

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "Awake_");
    }

    public override void OnStartServer()
    {
        // call Entity's OnStartServer
        base.OnStartServer();

        // load skills based on skill templates
        foreach (ScriptableSkill skillData in skillTemplates)
            skills.Add(new Skill(skillData));

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "OnStartServer_");
    }

    protected override void Start()
    {
        base.Start();

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "Start_");
    }

    void LateUpdate()
    {
        // pass parameters to animation state machine
        // => passing the states directly is the most reliable way to avoid all
        //    kinds of glitches like movement sliding, attack twitching, etc.
        // => make sure to import all looping animations like idle/run/attack
        //    with 'loop time' enabled, otherwise the client might only play it
        //    once
        // => only play moving animation while the agent is actually moving. the
        //    MOVING state might be delayed to due latency or we might be in
        //    MOVING while a path is still pending, etc.
        // => skill names are assumed to be boolean parameters in animator
        //    so we don't need to worry about an animation number etc.
        if (isClient) // no need for animations on the server
        {
            animator.SetBool("MOVING", state == "MOVING" && agent.velocity != Vector3.zero);
            animator.SetBool("CASTING", state == "CASTING");
            animator.SetBool("STUNNED", state == "STUNNED");
            animator.SetBool("DEAD", state == "DEAD");
            foreach (Skill skill in skills)
                animator.SetBool(skill.name, skill.CastTimeRemaining() > 0);
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "LateUpdate_");
    }

    // OnDrawGizmos only happens while the Script is not collapsed
    void OnDrawGizmos()
    {
        // draw the movement area (around 'start' if game running,
        // or around current position if still editing)
        Vector3 startHelp = Application.isPlaying ? owner.petDestination : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startHelp, returnDistance);

        // draw the follow dist
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(startHelp, followDistance);
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
        Utils.InvokeMany(typeof(Pet), this, "OnDestroy_");
    }

    // always update pets. IsWorthUpdating otherwise only updates if has observers,
    // but pets should still be updated even if they are too far from any observers,
    // so that they continue to run to their owner.
    public override bool IsWorthUpdating() { return true; }

    // finite state machine events /////////////////////////////////////////////
    bool EventOwnerDisappeared()
    {
        return owner == null;
    }

    bool EventDied()
    {
        return health == 0;
    }

    bool EventDeathTimeElapsed()
    {
        return state == "DEAD" && NetworkTime.time >= deathTimeEnd;
    }

    bool EventTargetDisappeared()
    {
        return target == null;
    }

    bool EventTargetDied()
    {
        return target != null && target.health == 0;
    }

    bool EventTargetTooFarToAttack()
    {
        Vector3 destination;
        return target != null &&
               0 <= currentSkill && currentSkill < skills.Count &&
               !CastCheckDistance(skills[currentSkill], out destination);
    }

    bool EventTargetTooFarToFollow()
    {
        return target != null &&
               Vector3.Distance(owner.petDestination, target.collider.ClosestPoint(transform.position)) > followDistance;
    }

    bool EventNeedReturnToOwner()
    {
        return Vector3.Distance(owner.petDestination, transform.position) > returnDistance;
    }

    bool EventNeedTeleportToOwner()
    {
        return Vector3.Distance(owner.petDestination, transform.position) > teleportDistance;
    }

    bool EventAggro()
    {
        return target != null && target.health > 0;
    }

    bool EventSkillRequest()
    {
        return 0 <= currentSkill && currentSkill < skills.Count;
    }

    bool EventSkillFinished()
    {
        return 0 <= currentSkill && currentSkill < skills.Count &&
               skills[currentSkill].CastTimeRemaining() == 0;
    }

    bool EventMoveEnd()
    {
        return state == "MOVING" && !IsMoving();
    }

    bool EventStunned()
    {
        return NetworkTime.time <= stunTimeEnd;
    }

    // finite state machine - server ///////////////////////////////////////////
    [Server]
    string UpdateServer_IDLE()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            // we died.
            OnDeath();
            return "DEAD";
        }
        if (EventStunned())
        {
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            currentSkill = -1;
            return "IDLE";
        }
        if (EventNeedTeleportToOwner())
        {
            agent.Warp(owner.petDestination);
            return "IDLE";
        }
        if (EventNeedReturnToOwner())
        {
            // return to owner only while IDLE
            target = null;
            currentSkill = -1;
            agent.stoppingDistance = 0;
            agent.destination = owner.petDestination;
            return "MOVING";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            currentSkill = -1;
            agent.stoppingDistance = 0;
            agent.destination = owner.petDestination;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.stoppingDistance = CurrentCastRange() * attackToMoveRangeRatio;
            agent.destination = target.collider.ClosestPoint(transform.position);
            return "MOVING";
        }
        if (EventSkillRequest())
        {
            // we had a target in attack range before and trying to cast a skill
            // on it. check self (alive, mana, weapon etc.) and target
            Skill skill = skills[currentSkill];
            if (CastCheckSelf(skill) && CastCheckTarget(skill))
            {
                // start casting
                StartCastSkill(skill);
                return "CASTING";
            }
            else
            {
                // invalid target. stop trying to cast.
                target = null;
                currentSkill = -1;
                return "IDLE";
            }
        }
        if (EventAggro())
        {
            // target in attack range. try to cast a first skill on it
            if (skills.Count > 0) currentSkill = NextSkill();
            else Debug.LogError(name + " has no skills to attack with.");
            return "IDLE";
        }
        if (EventMoveEnd()) { } // don't care
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care

        return "IDLE"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_MOVING()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            // we died.
            OnDeath();
            agent.ResetMovement();
            return "DEAD";
        }
        if (EventStunned())
        {
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventMoveEnd())
        {
            // we reached our destination.
            return "IDLE";
        }
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            currentSkill = -1;
            agent.ResetMovement();
            return "IDLE";
        }
        if (EventNeedTeleportToOwner())
        {
            agent.Warp(owner.petDestination);
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            currentSkill = -1;
            agent.stoppingDistance = 0;
            agent.destination = owner.petDestination;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.stoppingDistance = CurrentCastRange() * attackToMoveRangeRatio;
            agent.destination = target.collider.ClosestPoint(transform.position);
            return "MOVING";
        }
        if (EventAggro())
        {
            // target in attack range. try to cast a first skill on it
            // (we may get a target while randomly wandering around)
            if (skills.Count > 0) currentSkill = NextSkill();
            else Debug.LogError(name + " has no skills to attack with.");
            agent.ResetMovement();
            return "IDLE";
        }
        if (EventNeedReturnToOwner()) { } // don't care
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care
        if (EventSkillRequest()) { } // don't care, finish movement first

        return "MOVING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_CASTING()
    {
        // keep looking at the target for server & clients (only Y rotation)
        if (target) LookAtY(target.transform.position);

        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            // we died.
            OnDeath();
            return "DEAD";
        }
        if (EventStunned())
        {
            currentSkill = -1;
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventTargetDisappeared())
        {
            // cancel if the target matters for this skill
            if (skills[currentSkill].cancelCastIfTargetDied)
            {
                currentSkill = -1;
                target = null;
                return "IDLE";
            }
        }
        if (EventTargetDied())
        {
            // cancel if the target matters for this skill
            if (skills[currentSkill].cancelCastIfTargetDied)
            {
                currentSkill = -1;
                target = null;
                return "IDLE";
            }
        }
        if (EventSkillFinished())
        {
            // finished casting. apply the skill on the target.
            FinishCastSkill(skills[currentSkill]);

            // did the target die? then clear it so that the monster doesn't
            // run towards it if the target respawned
            if (target.health == 0) target = null;

            // go back to IDLE
            lastSkill = currentSkill;
            currentSkill = -1;
            return "IDLE";
        }
        if (EventMoveEnd()) { } // don't care
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventNeedTeleportToOwner()) { } // don't care
        if (EventNeedReturnToOwner()) { } // don't care
        if (EventTargetTooFarToAttack()) { } // don't care, we were close enough when starting to cast
        if (EventTargetTooFarToFollow()) { } // don't care, we were close enough when starting to cast
        if (EventAggro()) { } // don't care, always have aggro while casting
        if (EventSkillRequest()) { } // don't care, that's why we are here

        return "CASTING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_STUNNED()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventOwnerDisappeared())
        {
            // owner might disconnect or get destroyed for some reason
            NetworkServer.Destroy(gameObject);
            return "IDLE";
        }
        if (EventDied())
        {
            // we died.
            OnDeath();
            currentSkill = -1; // in case we died while trying to cast
            return "DEAD";
        }
        if (EventStunned())
        {
            return "STUNNED";
        }

        // go back to idle if we aren't stunned anymore and process all new
        // events there too
        return "IDLE";
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
        if (EventSkillRequest()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventMoveEnd()) { } // don't care
        if (EventNeedTeleportToOwner()) { } // don't care
        if (EventNeedReturnToOwner()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care
        if (EventTargetDied()) { } // don't care
        if (EventTargetTooFarToFollow()) { } // don't care
        if (EventTargetTooFarToAttack()) { } // don't care
        if (EventAggro()) { } // don't care
        if (EventDied()) { } // don't care, of course we are dead

        return "DEAD"; // nothing interesting happened
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE") return UpdateServer_IDLE();
        if (state == "MOVING") return UpdateServer_MOVING();
        if (state == "CASTING") return UpdateServer_CASTING();
        if (state == "STUNNED") return UpdateServer_STUNNED();
        if (state == "DEAD") return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient()
    {
        if (state == "CASTING")
        {
            // keep looking at the target for server & clients (only Y rotation)
            if (target) LookAtY(target.transform.position);
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "UpdateClient_");
    }

    // overlays ////////////////////////////////////////////////////////////////
    protected override void UpdateOverlays()
    {
        base.UpdateOverlays();

        if (ownerNameOverlay != null)
        {
            if (owner != null)
            {
                ownerNameOverlay.text = owner.name;
                // find local player (null while in character selection)
                if (Player.localPlayer != null)
                {
                    // note: murderer has higher priority (a player can be a murderer and an
                    // offender at the same time)
                    if (owner.IsMurderer())
                        ownerNameOverlay.color = owner.nameOverlayMurdererColor;
                    else if (owner.IsOffender())
                        ownerNameOverlay.color = owner.nameOverlayOffenderColor;
                    // member of the same party
                    else if (Player.localPlayer.InParty() && Player.localPlayer.party.Contains(owner.name))
                        ownerNameOverlay.color = owner.nameOverlayPartyColor;
                    // otherwise default
                    else
                        ownerNameOverlay.color = owner.nameOverlayDefaultColor;
                }
            }
            else ownerNameOverlay.text = "?";
        }
    }

    // combat //////////////////////////////////////////////////////////////////
    // custom DealDamageAt function that also rewards experience if we killed
    // the monster
    [Server]
    public override void DealDamageAt(Entity entity, int amount, float stunChance = 0, float stunTime = 0)
    {
        // deal damage with the default function
        base.DealDamageAt(entity, amount, stunChance, stunTime);

        // a monster?
        if (entity is Monster)
        {
            // forward to owner to share rewards with everyone
            owner.OnDamageDealtToMonster((Monster)entity);
        }
        // a player?
        // (see murder code section comments to understand the system)
        else if (entity is Player)
        {
            // forward to owner for murderer detection etc.
            owner.OnDamageDealtToPlayer((Player)entity);
        }
        // a pet?
        // (see murder code section comments to understand the system)
        else if (entity is Pet)
        {
            owner.OnDamageDealtToPet((Pet)entity);
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Pet), this, "DealDamageAt_", entity, amount);
    }

    // experience //////////////////////////////////////////////////////////////
    public float ExperiencePercent()
    {
        return (experience != 0 && experienceMax != 0) ? (float)experience / (float)experienceMax : 0;
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by entities that attack us
    [ServerCallback]
    public override void OnAggro(Entity entity)
    {
        // are we alive, and is the entity alive and of correct type?
        if (entity != null && CanAttack(entity))
        {
            // no target yet(==self), or closer than current target?
            // => has to be at least 20% closer to be worth it, otherwise we
            //    may end up nervously switching between two targets
            // => we do NOT use Utils.ClosestDistance, because then we often
            //    also end up nervously switching between two animated targets,
            //    since their collides moves with the animation.
            if (target == null)
            {
                target = entity;
            }
            else
            {
                float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                if (newDistance < oldDistance * 0.8) target = entity;
            }

            // addon system hooks
            Utils.InvokeMany(typeof(Pet), this, "OnAggro_", entity);
        }
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
        Utils.InvokeMany(typeof(Pet), this, "OnDeath_");
    }

    // skills //////////////////////////////////////////////////////////////////
    // CanAttack check
    // we use 'is' instead of 'GetType' so that it works for inherited types too
    public override bool CanAttack(Entity entity)
    {
        return base.CanAttack(entity) &&
               (entity is Monster ||
                (entity is Player && entity != owner) ||
                (entity is Pet && ((Pet)entity).owner != owner) ||
                (entity is Mount && ((Mount)entity).owner != owner));
    }

    // helper function to get the current cast range (if casting anything)
    public float CurrentCastRange()
    {
        return 0 <= currentSkill && currentSkill < skills.Count ? skills[currentSkill].castRange : 0;
    }

    // helper function to decide which skill to cast
    // => we got through skills one after another, this is better than selecting
    //    a random skill because it allows for some planning like:
    //    'strong skeleton always starts with a stun' etc.
    int NextSkill()
    {
        // find the next ready skill, starting at 'lastSkill+1' (= next one)
        // and looping at max once through them all (up to skill.Count)
        //  note: no skills.count == 0 check needed, this works with empty lists
        //  note: also works if lastSkill is still -1 from initialization
        for (int i = 0; i < skills.Count; ++i)
        {
            int index = (lastSkill + 1 + i) % skills.Count;
            // could we cast this skill right now? (enough mana, skill ready, etc.)
            if (CastCheckSelf(skills[index]))
                return index;
        }
        return -1;
    }
}

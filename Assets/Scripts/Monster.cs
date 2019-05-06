// The Monster class has a few different features that all aim to make monsters
// behave as realistically as possible.
//
// - **States:** first of all, the monster has several different states like
// IDLE, ATTACKING, MOVING and DEATH. The monster will randomly move around in
// a certain movement radius and try to attack any players in its aggro range.
// _Note: monsters use NavMeshAgents to move on the NavMesh._
//
// - **Aggro:** To save computations, we let Unity take care of finding players
// in the aggro range by simply adding a AggroArea _(see AggroArea.cs)_ sphere
// to the monster's children in the Hierarchy. We then use the OnTrigger
// functions to find players that are in the aggro area. The monster will always
// move to the nearest aggro player and then attack it as long as the player is
// in the follow radius. If the player happens to walk out of the follow
// radius then the monster will walk back to the start position quickly.
//
// - **Respawning:** The monsters have a _respawn_ property that can be set to
// true in order to make the monster respawn after it died. We developed the
// respawn system with simplicity in mind, there are no extra spawner objects
// needed. As soon as a monster dies, it will make itself invisible for a while
// and then go back to the starting position to respawn. This feature allows the
// developer to quickly drag monster Prefabs into the scene and place them
// anywhere, without worrying about spawners and spawn areas.
//
// - **Loot:** Dead monsters can also generate loot, based on the _lootItems_
// list. Each monster has a list of items with their dropchance, so that loot
// will always be generated randomly. Monsters can also randomly generate loot
// gold between a minimum and a maximum amount.
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

[RequireComponent(typeof(Animator))]
public partial class Monster : Entity
{
    [Header("Movement")]
    [Range(0, 1)] public float moveProbability = 0.1f; // chance per second
    public float moveDistance = 10;
    // monsters should follow their targets even if they run out of the movement
    // radius. the follow dist should always be bigger than the biggest archer's
    // attack range, so that archers will always pull aggro, even when attacking
    // from far away.
    public float followDistance = 20;

    [Header("Experience Reward")]
    public long rewardExperience = 10;
    public long rewardSkillExperience = 2;

    [Header("Loot")]
    public int lootGoldMin = 0;
    public int lootGoldMax = 10;
    public ItemDropChance[] dropChances;
    // note: Items have a .valid property that can be used to 'delete' an item.
    //       it's better than .RemoveAt() because we won't run into index-out-of
    //       range issues

    [Header("Respawn")]
    public float deathTime = 30f; // enough for animation & looting
    float deathTimeEnd;
    public bool respawn = true;
    public float respawnTime = 10f;
    float respawnTimeEnd;

    // save the start position for random movement distance and respawning
    Vector3 startPosition;

    // the last skill that was casted, to decide which one to cast next
    int lastSkill = -1;

    // networkbehaviour ////////////////////////////////////////////////////////
    protected override void Awake()
    {
        base.Awake();
        startPosition = transform.position;

        // addon system hooks
        Utils.InvokeMany(typeof(Monster), this, "Awake_");
    }

    public override void OnStartServer()
    {
        // call Entity's OnStartServer
        base.OnStartServer();

        // all monsters should spawn with full health and mana
        health = healthMax;
        mana = manaMax;

        // load skills based on skill templates
        foreach (ScriptableSkill skillData in skillTemplates)
            skills.Add(new Skill(skillData));

        // addon system hooks
        Utils.InvokeMany(typeof(Monster), this, "OnStartServer_");
    }

    protected override void Start()
    {
        base.Start();

        // addon system hooks
        Utils.InvokeMany(typeof(Monster), this, "Start_");
    }

    void LateUpdate()
    {
        // only if worth updating right now (e.g. a player is around)
        if (!IsWorthUpdating()) return;

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
            animator.SetBool("DEAD", state == "DEAD");
            foreach (Skill skill in skills)
                animator.SetBool(skill.name, skill.CastTimeRemaining() > 0);
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Monster), this, "LateUpdate_");
    }

    // OnDrawGizmos only happens while the Script is not collapsed
    void OnDrawGizmos()
    {
        // draw the movement area (around 'start' if game running,
        // or around current position if still editing)
        Vector3 startHelp = Application.isPlaying ? startPosition : transform.position;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(startHelp, moveDistance);

        // draw the follow dist
        Gizmos.color = Color.gray;
        Gizmos.DrawWireSphere(startHelp, followDistance);
    }

    // finite state machine events /////////////////////////////////////////////
    bool EventDied()
    {
        return health == 0;
    }

    bool EventDeathTimeElapsed()
    {
        return state == "DEAD" && Time.time >= deathTimeEnd;
    }

    bool EventRespawnTimeElapsed()
    {
        return state == "DEAD" && respawn && Time.time >= respawnTimeEnd;
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
               Vector3.Distance(startPosition, target.collider.ClosestPointOnBounds(transform.position)) > followDistance;
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

    bool EventMoveRandomly()
    {
        return Random.value <= moveProbability * Time.deltaTime;
    }

    // finite state machine - server ///////////////////////////////////////////
    [Server]
    string UpdateServer_IDLE()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died.
            OnDeath();
            currentSkill = -1; // in case we died while trying to cast
            return "DEAD";
        }
        if (EventTargetDied())
        {
            // we had a target before, but it died now. clear it.
            target = null;
            currentSkill = -1;
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            currentSkill = -1;
            agent.stoppingDistance = 0;
            agent.destination = startPosition;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.stoppingDistance = CurrentCastRange() * 0.8f;
            agent.destination = target.collider.ClosestPointOnBounds(transform.position);
            return "MOVING";
        }
        if (EventSkillRequest())
        {
            // we had a target in attack range before and trying to cast a skill
            // on it. check self (alive, mana, weapon etc.) and target
            Skill skill = skills[currentSkill];
            if (CastCheckSelf(skill) && CastCheckTarget(skill))
            {
                // start casting and set the casting end time
                skill.castTimeEnd = Time.time + skill.castTime;
                skills[currentSkill] = skill;
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
        if (EventMoveRandomly())
        {
            // walk to a random position in movement radius (from 'start')
            // note: circle y is 0 because we add it to start.y
            Vector2 circle2D = Random.insideUnitCircle * moveDistance;
            agent.stoppingDistance = 0;
            agent.destination = startPosition + new Vector3(circle2D.x, 0, circle2D.y);
            return "MOVING";
        }
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventRespawnTimeElapsed()) { } // don't care
        if (EventMoveEnd()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care

        return "IDLE"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_MOVING()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died.
            OnDeath();
            currentSkill = -1; // in case we died while trying to cast
            agent.ResetPath();
            return "DEAD";
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
            agent.ResetPath();
            return "IDLE";
        }
        if (EventTargetTooFarToFollow())
        {
            // we had a target before, but it's out of follow range now.
            // clear it and go back to start. don't stay here.
            target = null;
            currentSkill = -1;
            agent.stoppingDistance = 0;
            agent.destination = startPosition;
            return "MOVING";
        }
        if (EventTargetTooFarToAttack())
        {
            // we had a target before, but it's out of attack range now.
            // follow it. (use collider point(s) to also work with big entities)
            agent.stoppingDistance = CurrentCastRange() * 0.8f;
            agent.destination = target.collider.ClosestPointOnBounds(transform.position);
            return "MOVING";
        }
        if (EventAggro())
        {
            // target in attack range. try to cast a first skill on it
            // (we may get a target while randomly wandering around)
            if (skills.Count > 0) currentSkill = NextSkill();
            else Debug.LogError(name + " has no skills to attack with.");
            agent.ResetPath();
            return "IDLE";
        }
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventRespawnTimeElapsed()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care
        if (EventSkillRequest()) { } // don't care, finish movement first
        if (EventMoveRandomly()) { } // don't care

        return "MOVING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_CASTING()
    {
        // keep looking at the target for server & clients (only Y rotation)
        if (target) LookAtY(target.transform.position);

        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died.
            OnDeath();
            currentSkill = -1; // in case we died while trying to cast
            return "DEAD";
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
            CastSkill(skills[currentSkill]);

            // did the target die? then clear it so that the monster doesn't
            // run towards it if the target respawned
            if (target.health == 0) target = null;

            // go back to IDLE
            lastSkill = currentSkill;
            currentSkill = -1;
            return "IDLE";
        }
        if (EventDeathTimeElapsed()) { } // don't care
        if (EventRespawnTimeElapsed()) { } // don't care
        if (EventMoveEnd()) { } // don't care
        if (EventTargetTooFarToAttack()) { } // don't care, we were close enough when starting to cast
        if (EventTargetTooFarToFollow()) { } // don't care, we were close enough when starting to cast
        if (EventAggro()) { } // don't care, always have aggro while casting
        if (EventSkillRequest()) { } // don't care, that's why we are here
        if (EventMoveRandomly()) { } // don't care

        return "CASTING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_DEAD()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventRespawnTimeElapsed())
        {
            // respawn at the start position with full health, visibility, no loot
            gold = 0;
            inventory.Clear();
            Show();
            agent.Warp(startPosition); // recommended over transform.position
            Revive();
            return "IDLE";
        }
        if (EventDeathTimeElapsed())
        {
            // we were lying around dead for long enough now.
            // hide while respawning, or disappear forever
            if (respawn) Hide();
            else NetworkServer.Destroy(gameObject);
            return "DEAD";
        }
        if (EventSkillRequest()) { } // don't care
        if (EventSkillFinished()) { } // don't care
        if (EventMoveEnd()) { } // don't care
        if (EventTargetDisappeared()) { } // don't care
        if (EventTargetDied()) { } // don't care
        if (EventTargetTooFarToFollow()) { } // don't care
        if (EventTargetTooFarToAttack()) { } // don't care
        if (EventAggro()) { } // don't care
        if (EventMoveRandomly()) { } // don't care
        if (EventDied()) { } // don't care, of course we are dead

        return "DEAD"; // nothing interesting happened
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE") return UpdateServer_IDLE();
        if (state == "MOVING") return UpdateServer_MOVING();
        if (state == "CASTING") return UpdateServer_CASTING();
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
        Utils.InvokeMany(typeof(Monster), this, "UpdateClient_");
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by entities that attack us and by AggroArea
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
            //    => we don't even need closestdistance here because they are in
            //       the aggro area anyway. transform.position is perfectly fine
            if (target == null)
            {
                target = entity;
            }
            else if (entity != target) // no need to check dist for same target
            {
                float oldDistance = Vector3.Distance(transform.position, target.transform.position);
                float newDistance = Vector3.Distance(transform.position, entity.transform.position);
                if (newDistance < oldDistance * 0.8) target = entity;
            }

            // addon system hooks
            Utils.InvokeMany(typeof(Monster), this, "OnAggro_", entity);
        }
    }

    // loot ////////////////////////////////////////////////////////////////////
    // other scripts need to know if it still has valid loot (to show UI etc.)
    public bool HasLoot()
    {
        // any gold or valid items?
        return gold > 0 || inventory.Any(slot => slot.amount > 0);
    }

    // death ///////////////////////////////////////////////////////////////////
    [Server]
    protected override void OnDeath()
    {
        // take care of entity stuff
        base.OnDeath();

        // set death and respawn end times. we set both of them now to make sure
        // that everything works fine even if a monster isn't updated for a
        // while. so as soon as it's updated again, the death/respawn will
        // happen immediately if current time > end time.
        deathTimeEnd = Time.time + deathTime;
        respawnTimeEnd = deathTimeEnd + respawnTime; // after death time ended

        // generate gold
        gold = Random.Range(lootGoldMin, lootGoldMax);

        // generate items (note: can't use Linq because of SyncList)
        foreach (ItemDropChance itemChance in dropChances)
            if (Random.value <= itemChance.probability)
                inventory.Add(new ItemSlot(new Item(itemChance.item)));

        // addon system hooks
        Utils.InvokeMany(typeof(Monster), this, "OnDeath_");
    }

    // skills //////////////////////////////////////////////////////////////////
    // monsters always have a weapon
    public override bool HasCastWeapon() { return true; }

    // CanAttack check
    // we use 'is' instead of 'GetType' so that it works for inherited types too
    public override bool CanAttack(Entity entity)
    {
        return health > 0 &&
               entity.health > 0 &&
               entity != this &&
               (entity is Player ||
                entity is Pet);
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

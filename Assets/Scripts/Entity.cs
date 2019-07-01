// The Entity class is rather simple. It contains a few basic entity properties
// like health, mana and level that all inheriting classes like Players and
// Monsters can use.
//
// Entities also have a _target_ Entity that can't be synchronized with a
// SyncVar. Instead we created a EntityTargetSync component that takes care of
// that for us.
//
// Entities use a deterministic finite state machine to handle IDLE/MOVING/DEAD/
// CASTING etc. states and events. Using a deterministic FSM means that we react
// to every single event that can happen in every state (as opposed to just
// taking care of the ones that we care about right now). This means a bit more
// code, but it also means that we avoid all kinds of weird situations like 'the
// monster doesn't react to a dead target when casting' etc.
// The next state is always set with the return value of the UpdateServer
// function. It can never be set outside of it, to make sure that all events are
// truly handled in the state machine and not outside of it. Otherwise we may be
// tempted to set a state in CmdBeingTrading etc., but would likely forget of
// special things to do depending on the current state.
//
// Entities also need a kinematic Rigidbody so that OnTrigger functions can be
// called. Note that there is currently a Unity bug that slows down the agent
// when having lots of FPS(300+) if the Rigidbody's Interpolate option is
// enabled. So for now it's important to disable Interpolation - which is a good
// idea in general to increase performance.
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public enum DamageType { Normal, Block, Crit };

// note: no animator required, towers, dummies etc. may not have one
[RequireComponent(typeof(Rigidbody))] // kinematic, only needed for OnTrigger
[RequireComponent(typeof(NetworkProximityCheckerCustom))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(NetworkNavMeshAgent))]
public abstract partial class Entity : NetworkBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public NetworkProximityChecker proxchecker;
#pragma warning disable CS0108 // O membro oculta o membro herdado; nova palavra-chave ausente
    public NetworkIdentity netIdentity;
#pragma warning restore CS0108 // O membro oculta o membro herdado; nova palavra-chave ausente
    public Animator animator;
    new public Collider collider;

    // finite state machine
    // -> state only writable by entity class to avoid all kinds of confusion
    [Header("State")]
    [SyncVar, SerializeField] string _state = "IDLE";
    public string state { get { return _state; } }

    // 'Entity' can't be SyncVar and NetworkIdentity causes errors when null,
    // so we use [SyncVar] GameObject and wrap it for simplicity
    [Header("Target")]
    [SyncVar] GameObject _target;
    public Entity target
    {
        get { return _target != null ? _target.GetComponent<Entity>() : null; }
        set { _target = value != null ? value.gameObject : null; }
    }

    [Header("Level")]
    [SyncVar] public int level = 1;

    [Header("Health")]
    [SerializeField] protected LevelBasedInt _healthMax = new LevelBasedInt { baseValue = 100 };
    public virtual int healthMax
    {
        get
        {
            // base + buffs
            int buffBonus = buffs.Sum(buff => buff.buffsHealthMax);
            return _healthMax.Get(level) + buffBonus;
        }
    }
    public bool invincible = false; // GMs, Npcs, ...
    [SyncVar] int _health = 1;
    public int health
    {
        get { return Mathf.Min(_health, healthMax); } // min in case hp>hpmax after buff ends etc.
        set { _health = Mathf.Clamp(value, 0, healthMax); }
    }

    public bool healthRecovery = true; // can be disabled in combat etc.
    [SerializeField] protected LevelBasedInt _healthRecoveryRate = new LevelBasedInt { baseValue = 1 };
    public virtual int healthRecoveryRate
    {
        get
        {
            // base + buffs
            float buffPercent = buffs.Sum(buff => buff.buffsHealthPercentPerSecond);
            return _healthRecoveryRate.Get(level) + Convert.ToInt32(buffPercent * healthMax);
        }
    }

    [Header("Mana")]
    [SerializeField] protected LevelBasedInt _manaMax = new LevelBasedInt { baseValue = 100 };
    public virtual int manaMax
    {
        get
        {
            // base + buffs
            int buffBonus = buffs.Sum(buff => buff.buffsManaMax);
            return _manaMax.Get(level) + buffBonus;
        }
    }
    [SyncVar] int _mana = 1;
    public int mana
    {
        get { return Mathf.Min(_mana, manaMax); } // min in case hp>hpmax after buff ends etc.
        set { _mana = Mathf.Clamp(value, 0, manaMax); }
    }

    public bool manaRecovery = true; // can be disabled in combat etc.
    [SerializeField] protected LevelBasedInt _manaRecoveryRate = new LevelBasedInt { baseValue = 1 };
    public int manaRecoveryRate
    {
        get
        {
            // base + buffs
            float buffPercent = buffs.Sum(buff => buff.buffsManaPercentPerSecond);
            return _manaRecoveryRate.Get(level) + Convert.ToInt32(buffPercent * manaMax);
        }
    }

    [Header("Damage")]
    [SerializeField] protected LevelBasedInt _damage = new LevelBasedInt { baseValue = 1 };
    public virtual int damage
    {
        get
        {
            // base + buffs
            int buffBonus = buffs.Sum(buff => buff.buffsDamage);
            return _damage.Get(level) + buffBonus;
        }
    }

    [Header("Defense")]
    [SerializeField] protected LevelBasedInt _defense = new LevelBasedInt { baseValue = 1 };
    public virtual int defense
    {
        get
        {
            // base + buffs
            int buffBonus = buffs.Sum(buff => buff.buffsDefense);
            return _defense.Get(level) + buffBonus;
        }
    }

    [Header("Block")]
    [SerializeField] protected LevelBasedFloat _blockChance;
    public virtual float blockChance
    {
        get
        {
            // base + buffs
            float buffBonus = buffs.Sum(buff => buff.buffsBlockChance);
            return _blockChance.Get(level) + buffBonus;
        }
    }

    [Header("Critical")]
    [SerializeField] protected LevelBasedFloat _criticalChance;
    public virtual float criticalChance
    {
        get
        {
            // base + buffs
            float buffBonus = buffs.Sum(buff => buff.buffsCriticalChance);
            return _criticalChance.Get(level) + buffBonus;
        }
    }

    // speed wrapper
    public float speed { get { return agent.speed; } }

    [Header("Damage Popup")]
    public GameObject damagePopupPrefab;

    // skill system for all entities (players, monsters, npcs, towers, ...)
    // 'skillTemplates' are the available skills (first one is default attack)
    // 'skills' are the loaded skills with cooldowns etc.
    [Header("Skills & Buffs")]
    public ScriptableSkill[] skillTemplates;
    public SyncListSkill skills = new SyncListSkill();
    public SyncListBuff buffs = new SyncListBuff(); // active buffs
    // current skill (synced because we need it as an animation parameter)
    [SyncVar, HideInInspector] public int currentSkill = -1;

    // effect mount is where the arrows/fireballs/etc. are spawned
    // -> can be overwritten, e.g. for mages to set it to the weapon's effect
    //    mount
    // -> assign to right hand if in doubt!
    [SerializeField] Transform _effectMount;
    public virtual Transform effectMount { get { return _effectMount; } }

    // all entities should have an inventory, not just the player.
    // useful for monster loot, chests, etc.
    [Header("Inventory")]
    public SyncListItemSlot inventory = new SyncListItemSlot();

    // all entities should have gold, not just the player
    // useful for monster loot, chests etc.
    // note: int is not enough (can have > 2 mil. easily)
    [Header("Gold")]
    [SyncVar, SerializeField] long _gold = 0;
    public long gold { get { return _gold; } set { _gold = Math.Max(value, 0); } }

    // 3D text mesh for name above the entity's head
    [Header("Text Meshes")]
    public TextMesh nameOverlay;

    // networkbehaviour ////////////////////////////////////////////////////////
    protected virtual void Awake()
    {
        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "Awake_");
    }

    public override void OnStartServer()
    {
        // health recovery every second
        InvokeRepeating("Recover", 1, 1);

        // dead if spawned without health
        if (health == 0) _state = "DEAD";

        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "OnStartServer_");
    }

    protected virtual void Start()
    {
        // disable animator on server. this is a huge performance boost and
        // definitely worth one line of code (1000 monsters: 22 fps => 32 fps)
        // (!isClient because we don't want to do it in host mode either)
        // (OnStartServer doesn't know isClient yet, Start is the only option)
        if (!isClient) animator.enabled = false;
    }

    // monsters, npcs etc. don't have to be updated if no player is around
    // checking observers is enough, because lonely players have at least
    // themselves as observers, so players will always be updated
    // and dead monsters will respawn immediately in the first update call
    // even if we didn't update them in a long time (because of the 'end'
    // times)
    // -> update only if:
    //    - observers are null (they are null in clients)
    //    - if they are not null, then only if at least one (on server)
    //    - if the entity is hidden, otherwise it would never be updated again
    //      because it would never get new observers
    // -> can be overwritten if necessary (e.g. pets might be too far from
    //    observers but should still be updated to run to owner)
    public virtual bool IsWorthUpdating()
    {
        return netIdentity.observers == null ||
               netIdentity.observers.Count > 0 ||
               IsHidden();
    }

    // entity logic will be implemented with a finite state machine
    // -> we should react to every state and to every event for correctness
    // -> we keep it functional for simplicity
    // note: can still use LateUpdate for Updates that should happen in any case
    void Update()
    {
        // only update if it's worth updating (see IsWorthUpdating comments)
        // -> we also clear the target if it's hidden, so that players don't
        //    keep hidden (respawning) monsters as target, hence don't show them
        //    as target again when they are shown again
        if (IsWorthUpdating())
        {
            if (isClient)
            {
                UpdateClient();
                if (nameOverlay != null) nameOverlay.text = name;
            }
            if (isServer)
            {
                CleanupBuffs();
                if (target != null && target.IsHidden()) target = null;
                _state = UpdateServer();
            }

            // addon system hooks
            Utils.InvokeMany(typeof(Entity), this, "Update_");
        }
    }

    // update for server. should return the new state.
    protected abstract string UpdateServer();

    // update for client.
    protected abstract void UpdateClient();

    // visibility //////////////////////////////////////////////////////////////
    // hide a entity
    // note: using SetActive won't work because its not synced and it would
    //       cause inactive objects to not receive any info anymore
    // note: this won't be visible on the server as it always sees everything.
    [Server]
    public void Hide()
    {
        proxchecker.forceHidden = true;
    }

    [Server]
    public void Show()
    {
        proxchecker.forceHidden = false;
    }

    // is the entity currently hidden?
    // note: usually the server is the only one who uses forceHidden, the
    //       client usually doesn't know about it and simply doesn't see the
    //       GameObject.
    public bool IsHidden()
    {
        return proxchecker.forceHidden;
    }

    public float VisRange()
    {
        return proxchecker.visRange;
    }

    // look at a transform while only rotating on the Y axis (to avoid weird
    // tilts)
    public void LookAtY(Vector3 position)
    {
        transform.LookAt(new Vector3(position.x, transform.position.y, position.z));
    }

    // note: client can find out if moving by simply checking the state!
    [Server] // server is the only one who has up-to-date NavMeshAgent
    public bool IsMoving()
    {
        // -> agent.hasPath will be true if stopping distance > 0, so we can't
        //    really rely on that.
        // -> pathPending is true while calculating the path, which is good
        // -> remainingDistance is the distance to the last path point, so it
        //    also works when clicking somewhere onto a obstacle that isn'
        //    directly reachable.
        return agent.pathPending ||
               agent.remainingDistance > agent.stoppingDistance ||
               agent.velocity != Vector3.zero;
    }

    // health & mana ///////////////////////////////////////////////////////////
    public float HealthPercent()
    {
        return (health != 0 && healthMax != 0) ? (float)health / (float)healthMax : 0;
    }

    [Server]
    public void Revive(float healthPercentage = 1)
    {
        health = Mathf.RoundToInt(healthMax * healthPercentage);
    }

    public float ManaPercent()
    {
        return (mana != 0 && manaMax != 0) ? (float)mana / (float)manaMax : 0;
    }

    // combat //////////////////////////////////////////////////////////////////
    // deal damage at another entity
    // (can be overwritten for players etc. that need custom functionality)
    [Server]
    public virtual void DealDamageAt(Entity entity, int amount)
    {
        int damageDealt = 0;
        DamageType damageType = DamageType.Normal;

        // don't deal any damage if entity is invincible
        if (!entity.invincible)
        {
            // block? (we use < not <= so that block rate 0 never blocks)
            if (UnityEngine.Random.value < entity.blockChance)
            {
                damageType = DamageType.Block;
            }
            // deal damage
            else
            {
                // subtract defense (but leave at least 1 damage, otherwise
                // it may be frustrating for weaker players)
                damageDealt = Mathf.Max(amount - entity.defense, 1);

                // critical hit?
                if (UnityEngine.Random.value < criticalChance)
                {
                    damageDealt *= 2;
                    damageType = DamageType.Crit;
                }

                // deal the damage
                entity.health -= damageDealt;
            }
        }

        // let's make sure to pull aggro in any case so that archers
        // are still attacked if they are outside of the aggro range
        entity.OnAggro(this);

        // show effects on clients
        entity.RpcOnDamageReceived(damageDealt, damageType);

        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "DealDamageAt_", entity, amount);
    }

    // no need to instantiate damage popups on the server
    // -> calculating the position on the client saves server computations and
    //    takes less bandwidth (4 instead of 12 byte)
    [Client]
    void ShowDamagePopup(int amount, DamageType damageType)
    {
        // spawn the damage popup (if any) and set the text
        if (damagePopupPrefab != null)
        {
            // showing it above their head looks best, and we don't have to use
            // a custom shader to draw world space UI in front of the entity
            Bounds bounds = collider.bounds;
            Vector3 position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

            GameObject popup = Instantiate(damagePopupPrefab, position, Quaternion.identity);
            if (damageType == DamageType.Normal)
                popup.GetComponentInChildren<TextMesh>().text = amount.ToString();
            else if (damageType == DamageType.Block)
                popup.GetComponentInChildren<TextMesh>().text = "<i>Block!</i>";
            else if (damageType == DamageType.Crit)
                popup.GetComponentInChildren<TextMesh>().text = amount + " Crit!";
        }
    }

    [ClientRpc(channel = Channels.DefaultUnreliable)] // unimportant => unreliable
    void RpcOnDamageReceived(int amount, DamageType damageType)
    {
        // show popup above receiver's head in all observers via ClientRpc
        ShowDamagePopup(amount, damageType);

        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "OnDamageReceived_", amount, damageType);
    }

    // recovery ////////////////////////////////////////////////////////////////
    // recover health and mana once a second
    // note: when stopping the server with the networkmanager gui, it will
    //       generate warnings that Recover was called on client because some
    //       entites will only be disabled but not destroyed. let's not worry
    //       about that for now.
    [Server]
    public void Recover()
    {
        if (enabled && health > 0)
        {
            if (healthRecovery) health += healthRecoveryRate;
            if (manaRecovery) mana += manaRecoveryRate;
        }
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by the AggroArea (if any) on clients and server
    public virtual void OnAggro(Entity entity) { }

    // skill system ////////////////////////////////////////////////////////////
    // helper function to find a skill index
    public int GetSkillIndexByName(string skillName)
    {
        return skills.FindIndex(skill => skill.name == skillName);
    }

    // fist fights are virtually pointless because they overcomplicate the code
    // and they don't add any value to the game. so we need a check to find out
    // if the entity currently has a weapon equipped, otherwise casting a skill
    // shouldn't be possible. this may always return true for monsters, towers
    // etc.
    public abstract bool HasCastWeapon();

    // we need an abstract function to check if an entity can attack another,
    // e.g. if player can attack monster / pet / npc, ...
    // => we don't just compare the type because other things like 'is own pet'
    //    etc. matter too
    public abstract bool CanAttack(Entity entity);

    // the first check validates the caster
    // (the skill won't be ready if we check self while casting it. so the
    //  checkSkillReady variable can be used to ignore that if needed)
    public bool CastCheckSelf(Skill skill, bool checkSkillReady = true)
    {
        // has a weapon (important for projectiles etc.), no cooldown, hp, mp?
        return (!skill.requiresWeapon || HasCastWeapon()) &&
               (!checkSkillReady || skill.IsReady()) &&
               health > 0 &&
               mana >= skill.manaCosts;
    }

    // the second check validates the target and corrects it for the skill if
    // necessary (e.g. when trying to heal an npc, it sets target to self first)
    // (skill shots that don't need a target will just return true if the user
    //  wants to cast them at a valid position)
    public bool CastCheckTarget(Skill skill)
    {
        return skill.CheckTarget(this);
    }

    // the third check validates the distance between the caster and the target
    // (target entity or target position in case of skill shots)
    // note: castchecktarget already corrected the target (if any), so we don't
    //       have to worry about that anymore here
    public bool CastCheckDistance(Skill skill, out Vector3 destination)
    {
        return skill.CheckDistance(this, out destination);
    }

    // casts the skill. casting and waiting has to be done in the state machine
    public void CastSkill(Skill skill)
    {
        // * check if we can currently cast a skill (enough mana etc.)
        // * check if we can cast THAT skill on THAT target
        // note: we don't check the distance again. the skill will be cast even
        //   if the target walked a bit while we casted it (it's simply better
        //   gameplay and less frustrating)
        if (CastCheckSelf(skill, false) && CastCheckTarget(skill))
        {
            // let the skill template handle the action
            skill.Apply(this);

            // decrease mana in any case
            mana -= skill.manaCosts;

            // start the cooldown (and save it in the struct)
            skill.cooldownEnd = Time.time + skill.cooldown;

            // save any skill modifications in any case
            skills[currentSkill] = skill;
        }
        else
        {
            // not all requirements met. no need to cast the same skill again
            currentSkill = -1;
        }
    }

    // helper function to add or refresh a buff
    public void AddOrRefreshBuff(Buff buff)
    {
        // reset if already in buffs list, otherwise add
        int index = buffs.FindIndex(b => b.name == buff.name);
        if (index != -1) buffs[index] = buff;
        else buffs.Add(buff);
    }

    // helper function to remove all buffs that ended
    void CleanupBuffs()
    {
        for (int i = 0; i < buffs.Count; ++i)
        {
            if (buffs[i].BuffTimeRemaining() == 0)
            {
                buffs.RemoveAt(i);
                --i;
            }
        }
    }

    // inventory ///////////////////////////////////////////////////////////////
    // helper function to find an item in the inventory
    public int GetInventoryIndexByName(string itemName)
    {
        return inventory.FindIndex(slot => slot.amount > 0 && slot.item.name == itemName);
    }

    // helper function to count the free slots
    public int InventorySlotsFree()
    {
        return inventory.Count(slot => slot.amount == 0);
    }

    // helper function to calculate the total amount of an item type in inventory
    // note: .Equals because name AND dynamic variables matter (petLevel etc.)
    public int InventoryCount(Item item)
    {
        return (from slot in inventory
                where slot.amount > 0 && slot.item.Equals(item)
                select slot.amount).Sum();
    }

    // helper function to remove 'n' items from the inventory
    public bool InventoryRemove(Item item, int amount)
    {
        for (int i = 0; i < inventory.Count; ++i)
        {
            ItemSlot slot = inventory[i];
            // note: .Equals because name AND dynamic variables matter (petLevel etc.)
            if (slot.amount > 0 && slot.item.Equals(item))
            {
                // take as many as possible
                amount -= slot.DecreaseAmount(amount);
                inventory[i] = slot;

                // are we done?
                if (amount == 0) return true;
            }
        }

        // if we got here, then we didn't remove enough items
        return false;
    }

    // helper function to check if the inventory has space for 'n' items of type
    // -> the easiest solution would be to check for enough free item slots
    // -> it's better to try to add it onto existing stacks of the same type
    //    first though
    // -> it could easily take more than one slot too
    // note: this checks for one item type once. we can't use this function to
    //       check if we can add 10 potions and then 10 potions again (e.g. when
    //       doing player to player trading), because it will be the same result
    public bool InventoryCanAdd(Item item, int amount)
    {
        // go through each slot
        for (int i = 0; i < inventory.Count; ++i)
        {
            // empty? then subtract maxstack
            if (inventory[i].amount == 0)
                amount -= item.maxStack;
            // not empty. same type too? then subtract free amount (max-amount)
            // note: .Equals because name AND dynamic variables matter (petLevel etc.)
            else if (inventory[i].item.Equals(item))
                amount -= (inventory[i].item.maxStack - inventory[i].amount);

            // were we able to fit the whole amount already?
            if (amount <= 0) return true;
        }

        // if we got here than amount was never <= 0
        return false;
    }

    // helper function to put 'n' items of a type into the inventory, while
    // trying to put them onto existing item stacks first
    // -> this is better than always adding items to the first free slot
    // -> function will only add them if there is enough space for all of them
    public bool InventoryAdd(Item item, int amount)
    {
        // we only want to add them if there is enough space for all of them, so
        // let's double check
        if (InventoryCanAdd(item, amount))
        {
            // go through each slot
            for (int i = 0; i < inventory.Count; ++i)
            {
                // empty? then fill slot with as many as possible
                if (inventory[i].amount == 0)
                {
                    int add = Mathf.Min(amount, item.maxStack);
                    inventory[i] = new ItemSlot(item, add);
                    amount -= add;
                }
                // not empty. same type too? then add free amount (max-amount)
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                else if (inventory[i].item.Equals(item))
                {
                    ItemSlot temp = inventory[i];
                    amount -= temp.IncreaseAmount(amount);
                    inventory[i] = temp;
                }

                // were we able to fit the whole amount already?
                if (amount <= 0) return true;
            }
            // we should have been able to add all of them
            if (amount != 0) Debug.LogError("inventory add failed: " + item.name + " " + amount);
        }
        return false;
    }

    // death ///////////////////////////////////////////////////////////////////
    // universal OnDeath function that takes care of all the Entity stuff.
    // should be called by inheriting classes' finite state machine on death.
    [Server]
    protected virtual void OnDeath()
    {
        // stop any movement and buffs, clear target
        agent.ResetPath();
        buffs.Clear();
        target = null;

        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "OnDeath_");
    }
}

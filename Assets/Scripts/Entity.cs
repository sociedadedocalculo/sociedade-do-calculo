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
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public enum DamageType {Normal, Block, Crit};

// note: no animator required, towers, dummies etc. may not have one
[RequireComponent(typeof(Rigidbody))] // kinematic, only needed for OnTrigger
[RequireComponent(typeof(NetworkProximityChecker))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(AudioSource))]
public abstract partial class Entity : NetworkBehaviour
{
    [Header("Components")]
    public NavMeshAgent agent;
    public NetworkProximityChecker proxchecker;
    public NetworkIdentity netIdentity;
    public Animator animator;
    new public Collider collider;
    public AudioSource audioSource;

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
        get { return _target != null  ? _target.GetComponent<Entity>() : null; }
        set { _target = value != null ? value.gameObject : null; }
    }

    [Header("Level")]
    [SyncVar] public int level = 1;

    [Header("Health")]
    [SerializeField] protected LevelBasedInt _healthMax = new LevelBasedInt{baseValue=100};
    public virtual int healthMax
    {
        get
        {
            // base + passives + buffs
            int passiveBonus = (from skill in skills
                                where skill.level > 0 && skill.data is PassiveSkill
                                select ((PassiveSkill)skill.data).bonusHealthMax.Get(skill.level)).Sum();
            int buffBonus = buffs.Sum(buff => buff.bonusHealthMax);
            return _healthMax.Get(level) + passiveBonus + buffBonus;
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
    [SerializeField] protected LevelBasedInt _healthRecoveryRate = new LevelBasedInt{baseValue=1};
    public virtual int healthRecoveryRate
    {
        get
        {
            // base + passives + buffs
            float passivePercent = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).bonusHealthPercentPerSecond.Get(skill.level)).Sum();
            float buffPercent = buffs.Sum(buff => buff.bonusHealthPercentPerSecond);
            return _healthRecoveryRate.Get(level) + Convert.ToInt32(passivePercent * healthMax) + Convert.ToInt32(buffPercent * healthMax);
        }
    }

    [Header("Mana")]
    [SerializeField] protected LevelBasedInt _manaMax = new LevelBasedInt{baseValue=100};
    public virtual int manaMax
    {
        get
        {
            // base + passives + buffs
            int passiveBonus = (from skill in skills
                                  where skill.level > 0 && skill.data is PassiveSkill
                                  select ((PassiveSkill)skill.data).bonusManaMax.Get(skill.level)).Sum();
            int buffBonus = buffs.Sum(buff => buff.bonusManaMax);
            return _manaMax.Get(level) + passiveBonus + buffBonus;
        }
    }
    [SyncVar] int _mana = 1;
    public int mana
    {
        get { return Mathf.Min(_mana, manaMax); } // min in case hp>hpmax after buff ends etc.
        set { _mana = Mathf.Clamp(value, 0, manaMax); }
    }

    public bool manaRecovery = true; // can be disabled in combat etc.
    [SerializeField] protected LevelBasedInt _manaRecoveryRate = new LevelBasedInt{baseValue=1};
    public int manaRecoveryRate
    {
        get
        {
            // base + passives + buffs
            float passivePercent = (from skill in skills
                                    where skill.level > 0 && skill.data is PassiveSkill
                                    select ((PassiveSkill)skill.data).bonusManaPercentPerSecond.Get(skill.level)).Sum();
            float buffPercent = buffs.Sum(buff => buff.bonusManaPercentPerSecond);
            return _manaRecoveryRate.Get(level) + Convert.ToInt32(passivePercent * manaMax) + Convert.ToInt32(buffPercent * manaMax);
        }
    }

    [Header("Damage")]
    [SerializeField] protected LevelBasedInt _damage = new LevelBasedInt{baseValue=1};
    public virtual int damage
    {
        get
        {
            // base + passives + buffs
            int passiveBonus = (from skill in skills
                                where skill.level > 0 && skill.data is PassiveSkill
                                select ((PassiveSkill)skill.data).bonusDamage.Get(skill.level)).Sum();
            int buffBonus = buffs.Sum(buff => buff.bonusDamage);
            return _damage.Get(level) + passiveBonus + buffBonus;
        }
    }

    [Header("Defense")]
    [SerializeField] protected LevelBasedInt _defense = new LevelBasedInt{baseValue=1};
    public virtual int defense
    {
        get
        {
            // base + passives + buffs
            int passiveBonus = (from skill in skills
                                where skill.level > 0 && skill.data is PassiveSkill
                                select ((PassiveSkill)skill.data).bonusDefense.Get(skill.level)).Sum();
            int buffBonus = buffs.Sum(buff => buff.bonusDefense);
            return _defense.Get(level) + passiveBonus + buffBonus;
        }
    }

    [Header("Block")]
    [SerializeField] protected LevelBasedFloat _blockChance;
    public virtual float blockChance
    {
        get
        {
            // base + passives + buffs
            float passiveBonus = (from skill in skills
                                  where skill.level > 0 && skill.data is PassiveSkill
                                  select ((PassiveSkill)skill.data).bonusBlockChance.Get(skill.level)).Sum();
            float buffBonus = buffs.Sum(buff => buff.bonusBlockChance);
            return _blockChance.Get(level) + passiveBonus + buffBonus;
        }
    }

    [Header("Critical")]
    [SerializeField] protected LevelBasedFloat _criticalChance;
    public virtual float criticalChance
    {
        get
        {
            // base + passives + buffs
            float passiveBonus = (from skill in skills
                                  where skill.level > 0 && skill.data is PassiveSkill
                                  select ((PassiveSkill)skill.data).bonusCriticalChance.Get(skill.level)).Sum();
            float buffBonus = buffs.Sum(buff => buff.bonusCriticalChance);
            return _criticalChance.Get(level) + passiveBonus + buffBonus;
        }
    }

    [Header("Speed")]
    [SerializeField] protected LevelBasedFloat _speed = new LevelBasedFloat{baseValue=5};
    public virtual float speed
    {
        get
        {
            // base + passives + buffs
            float passiveBonus = (from skill in skills
                                where skill.level > 0 && skill.data is PassiveSkill
                                select ((PassiveSkill)skill.data).bonusSpeed.Get(skill.level)).Sum();
            float buffBonus = buffs.Sum(buff => buff.bonusSpeed);
            return _speed.Get(level) + passiveBonus + buffBonus;
        }
    }

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

    // equipment needs to be in Entity because arrow shooting skills need to
    // check if the entity has enough arrows
    [Header("Equipment")]
    public SyncListItemSlot equipment = new SyncListItemSlot();

    // all entities should have gold, not just the player
    // useful for monster loot, chests etc.
    // note: int is not enough (can have > 2 mil. easily)
    [Header("Gold")]
    [SyncVar, SerializeField] long _gold = 0;
    public long gold { get { return _gold; } set { _gold = Math.Max(value, 0); } }

    // 3D text mesh for name above the entity's head
    [Header("Text Meshes")]
    public TextMesh nameOverlay;
    public TextMesh stunnedOverlay;

    // every entity can be stunned by setting stunEndTime
    protected double stunTimeEnd;

    // safe zone flag
    // -> needs to be in Entity because both player and pet need it
    [HideInInspector] public bool inSafeZone;

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
            // always apply speed to agent
            agent.speed = speed;

            if (isClient)
            {
                UpdateClient();
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

        // update overlays in any case, except on server-only mode
        // (also update for character selection previews etc. then)
        bool serverOnly = isServer && !isClient;
        if (!serverOnly) UpdateOverlays();
    }

    // update for server. should return the new state.
    protected abstract string UpdateServer();

    // update for client.
    protected abstract void UpdateClient();

    // can be overwritten for more overlays
    protected virtual void UpdateOverlays()
    {
        if (nameOverlay != null)
            nameOverlay.text = name;

        if (stunnedOverlay != null)
            stunnedOverlay.gameObject.SetActive(state == "STUNNED");
    }

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

    public bool IsMoving()
    {
        // -> agent.hasPath will be true if stopping distance > 0, so we can't
        //    really rely on that.
        // -> pathPending is true while calculating the path, which is good
        // -> remainingDistance is the distance to the last path point, so it
        //    also works when clicking somewhere onto a obstacle that isn't
        //    directly reachable.
        // -> velocity is the best way to detect WASD movement
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
    public virtual void DealDamageAt(Entity entity, int amount, float stunChance=0, float stunTime=0)
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

                // stun?
                if (UnityEngine.Random.value < stunChance)
                {
                    entity.stunTimeEnd = NetworkTime.time + stunTime;
                }
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

    [ClientRpc]
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
    public virtual void OnAggro(Entity entity) {}

    // skill system ////////////////////////////////////////////////////////////
    // helper function to find a skill index
    public int GetSkillIndexByName(string skillName)
    {
        return skills.FindIndex(skill => skill.name == skillName);
    }

    // helper function to find a buff index
    public int GetBuffIndexByName(string buffName)
    {
        return buffs.FindIndex(buff => buff.name == buffName);
    }

    // fist fights are virtually pointless because they overcomplicate the code
    // and they don't add any value to the game. so we need a check to find out
    // if the entity currently has a weapon equipped, otherwise casting a skill
    // shouldn't be possible. this may always return true for monsters, towers
    // etc.
    public abstract bool HasCastWeapon();

    // we need a function to check if an entity can attack another.
    // => overwrite to add more cases like 'monsters can only attack players'
    //    or 'player can attack pets but not own pet' etc.
    public virtual bool CanAttack(Entity entity)
    {
        return health > 0 &&
               entity.health > 0 &&
               entity != this &&
               !inSafeZone && !entity.inSafeZone;
    }

    // the first check validates the caster
    // (the skill won't be ready if we check self while casting it. so the
    //  checkSkillReady variable can be used to ignore that if needed)
    public bool CastCheckSelf(Skill skill, bool checkSkillReady = true)
    {
        // has a weapon (important for projectiles etc.), no cooldown, hp, mp?
        return skill.CheckSelf(this, checkSkillReady);
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

    // starts casting
    public void StartCastSkill(Skill skill)
    {
        // start casting and set the casting end time
        skill.castTimeEnd = NetworkTime.time + skill.castTime;

        // save modifications
        skills[currentSkill] = skill;

        // rpc for client sided effects
        RpcSkillCastStarted();
    }

    // finishes casting. casting and waiting has to be done in the state machine
    public void FinishCastSkill(Skill skill)
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

            // rpc for client sided effects
            // -> pass that skill because skillIndex might be reset in the mean
            //    time, we never know
            RpcSkillCastFinished(skill);

            // decrease mana in any case
            mana -= skill.manaCosts;

            // start the cooldown (and save it in the struct)
            skill.cooldownEnd = NetworkTime.time + skill.cooldown;

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

    // skill cast started rpc for client sided effects
    // note: no need to pass skillIndex, currentSkill is synced anyway
    [ClientRpc]
    public void RpcSkillCastStarted()
    {
        // validate: still alive and valid skill?
        if (health > 0 && 0 <= currentSkill && currentSkill < skills.Count)
        {
            skills[currentSkill].data.OnCastStarted(this);
        }
    }

    // skill cast finished rpc for client sided effects
    // note: no need to pass skillIndex, currentSkill is synced anyway
    [ClientRpc]
    public void RpcSkillCastFinished(Skill skill)
    {
        // validate: still alive?
        if (health > 0)
        {
            // call scriptableskill event
            skill.data.OnCastFinished(this);

            // maybe some other component needs to know about it too
            SendMessage("OnSkillCastFinished", skill, SendMessageOptions.DontRequireReceiver);
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
            // add to same item stacks first (if any)
            // (otherwise we add to first empty even if there is an existing
            //  stack afterwards)
            for (int i = 0; i < inventory.Count; ++i)
            {
                // not empty and same type? then add free amount (max-amount)
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                if (inventory[i].amount > 0 && inventory[i].item.Equals(item))
                {
                    ItemSlot temp = inventory[i];
                    amount -= temp.IncreaseAmount(amount);
                    inventory[i] = temp;
                }

                // were we able to fit the whole amount already? then stop loop
                if (amount <= 0) return true;
            }

            // add to empty slots (if any)
            for (int i = 0; i < inventory.Count; ++i)
            {
                // empty? then fill slot with as many as possible
                if (inventory[i].amount == 0)
                {
                    int add = Mathf.Min(amount, item.maxStack);
                    inventory[i] = new ItemSlot(item, add);
                    amount -= add;
                }

                // were we able to fit the whole amount already? then stop loop
                if (amount <= 0) return true;
            }
            // we should have been able to add all of them
            if (amount != 0) Debug.LogError("inventory add failed: " + item.name + " " + amount);
        }
        return false;
    }

    // equipment ///////////////////////////////////////////////////////////////
    public int GetEquipmentIndexByName(string itemName)
    {
        return equipment.FindIndex(slot => slot.amount > 0 && slot.item.name == itemName);
    }

    // death ///////////////////////////////////////////////////////////////////
    // universal OnDeath function that takes care of all the Entity stuff.
    // should be called by inheriting classes' finite state machine on death.
    [Server]
    protected virtual void OnDeath()
    {
        // clear movement/buffs/target/cast
        agent.ResetMovement();
        buffs.Clear();
        target = null;
        currentSkill = -1;

        // addon system hooks
        Utils.InvokeMany(typeof(Entity), this, "OnDeath_");
    }

    // ontrigger ///////////////////////////////////////////////////////////////
    // protected so that inheriting classes can use OnTrigger too, while also
    // calling those here via base.OnTriggerEnter/Exit
    protected void OnTriggerEnter(Collider col)
    {
        // check if trigger first to avoid GetComponent tests for environment
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = true;
    }

    protected void OnTriggerExit(Collider col)
    {
        // check if trigger first to avoid GetComponent tests for environment
        if (col.isTrigger && col.GetComponent<SafeZone>())
            inSafeZone = false;
    }
}

// All player logic was put into this class. We could also split it into several
// smaller components, but this would result in many GetComponent calls and a
// more complex syntax.
//
// The default Player class takes care of the basic player logic like the state
// machine and some properties like damage and defense.
//
// The Player class stores the maximum experience for each level in a simple
// array. So the maximum experience for level 1 can be found in expMax[0] and
// the maximum experience for level 2 can be found in expMax[1] and so on. The
// player's health and mana are also level dependent in most MMORPGs, hence why
// there are hpMax and mpMax arrays too. We can find out a players's max health
// in level 1 by using hpMax[0] and so on.
//
// The class also takes care of selection handling, which detects 3D world
// clicks and then targets/navigates somewhere/interacts with someone.
//
// Animations are not handled by the NetworkAnimator because it's still very
// buggy and because it can't really react to movement stops fast enough, which
// results in moonwalking. Not synchronizing animations over the network will
// also save us bandwidth
using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System;
using System.Linq;
using System.Collections.Generic;

public enum TradeStatus {Free, Locked, Accepted}
public enum CraftingState {None, InProgress, Success, Failed}

[Serializable]
public struct SkillbarEntry
{
    public string reference;
    public KeyCode hotKey;
}

[Serializable]
public struct EquipmentInfo
{
    public string requiredCategory;
    public Transform location;
    public ScriptableItem defaultItem;
    public int defaultItemAmount;
}

[Serializable]
public struct ItemMallCategory
{
    public string category;
    public ScriptableItem[] items;
}

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Chat))]
[RequireComponent(typeof(NetworkName))]
public partial class Player : Entity
{
    [Header("Components")]
    public Chat chat;
    public Camera avatarCamera;
    public NetworkNavMeshAgentRubberbanding networkNavMeshAgent;

    [Header("Text Meshes")]
    public TextMesh guildOverlay;
    public string guildOverlayPrefix = "[";
    public string guildOverlaySuffix = "]";
    public Color nameOverlayDefaultColor = Color.white;
    public Color nameOverlayOffenderColor = Color.magenta;
    public Color nameOverlayMurdererColor = Color.red;
    public Color nameOverlayPartyColor = new Color(0.341f, 0.965f, 0.702f);

    [Header("Icons")]
    public Sprite classIcon; // for character selection
    public Sprite portraitIcon; // for top left portrait

    // some meta info
    [HideInInspector] public string account = "";
    [HideInInspector] public string className = "";

    // localPlayer singleton for easier access from UI scripts etc.
    public static Player localPlayer;

    // health
    public override int healthMax
    {
        get
        {
            // calculate equipment bonus
            int equipmentBonus = (from slot in equipment
                                  where slot.amount > 0
                                  select ((EquipmentItem)slot.item.data).healthBonus).Sum();

            // calculate strength bonus (1 strength means 1% of hpMax bonus)
            int attributeBonus = Convert.ToInt32(_healthMax.Get(level) * (strength * 0.01f));

            // base (health + buff) + equip + attributes
            return base.healthMax + equipmentBonus + attributeBonus;
        }
    }

    // mana
    public override int manaMax
    {
        get
        {
            // calculate equipment bonus
            int equipmentBonus = (from slot in equipment
                                  where slot.amount > 0
                                  select ((EquipmentItem)slot.item.data).manaBonus).Sum();

            // calculate intelligence bonus (1 intelligence means 1% of hpMax bonus)
            int attributeBonus = Convert.ToInt32(_manaMax.Get(level) * (intelligence * 0.01f));

            // base (mana + buff) + equip + attributes
            return base.manaMax + equipmentBonus + attributeBonus;
        }
    }

    // damage
    public override int damage
    {
        get
        {
            // calculate equipment bonus
            int equipmentBonus = (from slot in equipment
                                  where slot.amount > 0
                                  select ((EquipmentItem)slot.item.data).damageBonus).Sum();

            // return base (damage + buff) + equip
            return base.damage + equipmentBonus;
        }
    }

    // defense
    public override int defense
    {
        get
        {
            // calculate equipment bonus
            int equipmentBonus = (from slot in equipment
                                  where slot.amount > 0
                                  select ((EquipmentItem)slot.item.data).defenseBonus).Sum();

            // return base (defense + buff) + equip
            return base.defense + equipmentBonus;
        }
    }

    // block
    public override float blockChance
    {
        get
        {
            // calculate equipment bonus
            float equipmentBonus = (from slot in equipment
                                    where slot.amount > 0
                                    select ((EquipmentItem)slot.item.data).blockChanceBonus).Sum();

            // return base (blockChance + buff) + equip
            return base.blockChance + equipmentBonus;
        }
    }

    // crit
    public override float criticalChance
    {
        get
        {
            // calculate equipment bonus
            float equipmentBonus = (from slot in equipment
                                    where slot.amount > 0
                                    select ((EquipmentItem)slot.item.data).criticalChanceBonus).Sum();

            // return base (criticalChance + buff) + equip
            return base.criticalChance + equipmentBonus;
        }
    }

    [Header("Attributes")]
    [SyncVar] public int strength = 0;
    [SyncVar] public int intelligence = 0;

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
                while (_experience >= experienceMax && level < maxLevel)
                {
                    // subtract current level's required exp, then level up
                    _experience -= experienceMax;
                    ++level;

                    // addon system hooks
                    Utils.InvokeMany(typeof(Player), this, "OnLevelUp_");
                }

                // set to expMax if there is still too much exp remaining
                if (_experience > experienceMax) _experience = experienceMax;
            }
        }
    }
    [SerializeField] protected LevelBasedLong _experienceMax = new LevelBasedLong{baseValue=10, bonusPerLevel=10};
    public long experienceMax { get { return _experienceMax.Get(level); } }

    [Header("Skill Experience")]
    [SyncVar] public long skillExperience = 0;

    [Header("Indicator")]
    public GameObject indicatorPrefab;
    [HideInInspector] public GameObject indicator;

    [Header("Inventory")]
    public int inventorySize = 30;
    public ScriptableItem[] defaultItems;
    public KeyCode[] inventorySplitKeys = {KeyCode.LeftShift, KeyCode.RightShift};

    [Header("Trash")]
    [SyncVar] public ItemSlot trash = new ItemSlot();

    [Header("Equipment Info")]
    public EquipmentInfo[] equipmentInfo = new EquipmentInfo[] {
        new EquipmentInfo{requiredCategory="Weapon", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Head", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Chest", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Legs", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Shield", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Shoulders", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Hands", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Feet", location=null, defaultItem=null}
    };

    [Header("Skillbar")]
    public SkillbarEntry[] skillbar = new SkillbarEntry[] {
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha1},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha2},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha3},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha4},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha5},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha6},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha7},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha8},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha9},
        new SkillbarEntry{reference="", hotKey=KeyCode.Alpha0},
    };

    [Header("Quests")] // contains active and completed quests (=all)
    public int activeQuestLimit = 10;
    public SyncListQuest quests = new SyncListQuest();

    [Header("Interaction")]
    public float interactionRange = 4;
    public KeyCode targetNearestKey = KeyCode.Tab;
    public bool localPlayerClickThrough = true; // click selection goes through localplayer. feels best.

    [Header("PvP")]
    public BuffSkill offenderBuff;
    public BuffSkill murdererBuff;

    [Header("Trading")]
    [SyncVar, HideInInspector] public string tradeRequestFrom = "";
    [SyncVar, HideInInspector] public TradeStatus tradeStatus = TradeStatus.Free;
    [SyncVar, HideInInspector] public long tradeOfferGold = 0;
    public SyncListInt tradeOfferItems = new SyncListInt(); // inventory indices

    [Header("Crafting")]
    public List<int> craftingIndices = Enumerable.Repeat(-1, ScriptableRecipe.recipeSize).ToList();
    [HideInInspector] public CraftingState craftingState = CraftingState.None; // // client sided
    [SyncVar, HideInInspector] public double craftingTimeEnd; // double for long term precision

    [Header("Item Mall")]
    public ItemMallCategory[] itemMallCategories; // the items that can be purchased in the item mall
    [SyncVar] public long coins = 0;
    public float couponWaitSeconds = 3;

    [Header("Guild")]
    [SyncVar, HideInInspector] public string guildName = ""; // syncvar so that all observers see it
    [SyncVar, HideInInspector] public string guildInviteFrom = "";
    [HideInInspector] public Guild guild; // no syncvar, only owner sees it via TargetRPC (saves lots of bandwidth)
    public float guildInviteWaitSeconds = 3;

    [Header("Party")]
    [HideInInspector] public Party party;
    [SyncVar, HideInInspector] public string partyInviteFrom = "";
    public float partyInviteWaitSeconds = 3;

    [Header("Pet")]
    [SyncVar] GameObject _activePet;
    public Pet activePet
    {
        get { return _activePet != null  ? _activePet.GetComponent<Pet>() : null; }
        set { _activePet = value != null ? value.gameObject : null; }
    }
    // pet's destination should always be right next to player, not inside him
    // -> we use a helper property so we don't have to recalculate it each time
    // -> we offset the position by exactly 1 x bounds to the left because dogs
    //    are usually trained to walk on the left of the owner. looks natural.
    public Vector3 petDestination
    {
        get
        {
            Bounds bounds = collider.bounds;
            return transform.position - transform.right * bounds.size.x;
        }
    }

    // when moving into attack range of a target, we always want to move a
    // little bit closer than necessary to tolerate for latency and other
    // situations where the target might have moved away a little bit already.
    [Header("Movement")]
    [Range(0.1f, 1)] public float attackToMoveRangeRatio = 0.8f;

    [Header("Death")]
    public float deathExperienceLossPercent = 0.05f;

    // some commands should have delays to avoid DDOS, too much database usage
    // or brute forcing coupons etc. we use one riskyAction timer for all.
    [SyncVar, HideInInspector] public double nextRiskyActionTime = 0; // double for long term precision

    // the next target to be set if we try to set it while casting
    // 'Entity' can't be SyncVar and NetworkIdentity causes errors when null,
    // so we use [SyncVar] GameObject and wrap it for simplicity
    [SyncVar] GameObject _nextTarget;
    public Entity nextTarget
    {
        get { return _nextTarget != null  ? _nextTarget.GetComponent<Entity>() : null; }
        set { _nextTarget = value != null ? value.gameObject : null; }
    }

    // cache players to save lots of computations
    // (otherwise we'd have to iterate NetworkServer.objects all the time)
    // => on server: all online players
    // => on client: all observed players
    public static Dictionary<string, Player> onlinePlayers = new Dictionary<string, Player>();

    // helper variable to remember which skill to use when we walked close enough
    int useSkillWhenCloser = -1;

    // networkbehaviour ////////////////////////////////////////////////////////
    protected override void Awake()
    {
        // cache base components
        base.Awake();

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "Awake_");
    }

    public override void OnStartLocalPlayer()
    {
        // set singleton
        localPlayer = this;

        // setup camera targets
        Camera.main.GetComponent<CameraMMO>().target = transform;
        GameObject.FindWithTag("MinimapCamera").GetComponent<CopyPosition>().target = transform;
        if (avatarCamera) avatarCamera.enabled = true; // avatar camera for local player

        // load skillbar after player data was loaded
        LoadSkillbar();

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "OnStartLocalPlayer_");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();

        // setup synclist callbacks on client. no need to update and show and
        // animate equipment on server
        equipment.Callback += OnEquipmentChanged;

        // refresh all locations once (on synclist changed won't be called
        // for initial lists)
        // -> needs to happen before ProximityChecker's initial SetVis call,
        //    otherwise we get a hidden character with visible equipment
        //    (hence OnStartClient and not Start)
        for (int i = 0; i < equipment.Count; ++i)
            RefreshLocation(i);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        // initialize trade item indices
        for (int i = 0; i < 6; ++i) tradeOfferItems.Add(-1);

        InvokeRepeating("ProcessCoinOrders", 5, 5);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "OnStartServer_");
    }

    protected override void Start()
    {
        // do nothing if not spawned (=for character selection previews)
        if (!isServer && !isClient) return;

        base.Start();
        onlinePlayers[name] = this;

        // spawn effects for any buffs that might still be active after loading
        // (OnStartServer is too early)
        // note: no need to do that in Entity.Start because we don't load them
        //       with previously casted skills
        if (isServer)
            for (int i = 0; i < buffs.Count; ++i)
                if (buffs[i].BuffTimeRemaining() > 0)
                    buffs[i].data.SpawnEffect(this, this);

        // notify guild members that we are online. this also updates the client's
        // own guild info via targetrpc automatically
        // -> OnStartServer is too early because it's not spawned there yet
        if (isServer)
            SetGuildOnline(true);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "Start_");
    }

    void LateUpdate()
    {
        // pass parameters to animation state machine
        // => passing the states directly is the most reliable way to avoid all
        //    kinds of glitches like movement sliding, attack twitching, etc.
        // => make sure to import all looping animations like idle/run/attack
        //    with 'loop time' enabled, otherwise the client might only play it
        //    once
        // => MOVING state is set to local IsMovement result directly. otherwise
        //    we would see animation latencies for rubberband movement if we
        //    have to wait for MOVING state to be received from the server
        // => skill names are assumed to be boolean parameters in animator
        //    so we don't need to worry about an animation number etc.
        if (isClient) // no need for animations on the server
        {
            // now pass parameters after any possible rebinds
            foreach (Animator anim in GetComponentsInChildren<Animator>())
            {
                anim.SetBool("MOVING", IsMoving());
                anim.SetBool("CASTING", state == "CASTING");
                anim.SetBool("STUNNED", state == "STUNNED");
                anim.SetBool("DEAD", state == "DEAD");
                foreach (Skill skill in skills)
                    if (skill.level > 0 && !(skill.data is PassiveSkill))
                        anim.SetBool(skill.name, skill.CastTimeRemaining() > 0);
            }
        }

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "LateUpdate_");
    }

    void OnDestroy()
    {
        // do nothing if not spawned (=for character selection previews)
        if (!isServer && !isClient) return;

        // Unity bug: isServer is false when called in host mode. only true when
        // called in dedicated mode. so we need a workaround:
        if (NetworkServer.active) // isServer
        {
            // leave party (if any)
            if (InParty())
            {
                // dismiss if master, leave otherwise
                if (party.members[0] == name)
                    PartyDismiss();
                else
                    PartyLeave();
            }

            // notify guild members that we are offline
            SetGuildOnline(false);
        }

        if (isLocalPlayer) // requires at least Unity 5.5.1 bugfix to work
        {
            Destroy(indicator);
            SaveSkillbar();
            localPlayer = null;
        }

        onlinePlayers.Remove(name);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "OnDestroy_");
    }

    // finite state machine events /////////////////////////////////////////////
    bool EventDied()
    {
        return health == 0;
    }

    bool EventTargetDisappeared()
    {
        return target == null;
    }

    bool EventTargetDied()
    {
        return target != null && target.health == 0;
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

    bool EventMoveStart()
    {
        return state != "MOVING" && IsMoving(); // only fire when started moving
    }

    bool EventMoveEnd()
    {
        return state == "MOVING" && !IsMoving(); // only fire when stopped moving
    }

    bool EventTradeStarted()
    {
        // did someone request a trade? and did we request a trade with him too?
        Player player = FindPlayerFromTradeInvitation();
        return player != null && player.tradeRequestFrom == name;
    }

    bool EventTradeDone()
    {
        // trade canceled or finished?
        return state == "TRADING" && tradeRequestFrom == "";
    }

    bool craftingRequested;
    bool EventCraftingStarted()
    {
        bool result = craftingRequested;
        craftingRequested = false;
        return result;
    }

    bool EventCraftingDone()
    {
        return state == "CRAFTING" && NetworkTime.time > craftingTimeEnd;
    }

    bool EventStunned()
    {
        return NetworkTime.time <= stunTimeEnd;
    }

    [Command]
    public void CmdRespawn() { respawnRequested = true; }
    bool respawnRequested;
    bool EventRespawn()
    {
        bool result = respawnRequested;
        respawnRequested = false; // reset
        return result;
    }

    [Command]
    public void CmdCancelAction() { cancelActionRequested = true; }
    bool cancelActionRequested;
    bool EventCancelAction()
    {
        bool result = cancelActionRequested;
        cancelActionRequested = false; // reset
        return result;
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
            return "DEAD";
        }
        if (EventStunned())
        {
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventCancelAction())
        {
            // the only thing that we can cancel is the target
            target = null;
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            // cancel casting (if any), set target, go to trading
            currentSkill = -1; // just in case
            target = FindPlayerFromTradeInvitation();
            return "TRADING";
        }
        if (EventCraftingStarted())
        {
            // cancel casting (if any), go to crafting
            currentSkill = -1; // just in case
            return "CRAFTING";
        }
        if (EventMoveStart())
        {
            // cancel casting (if any)
            currentSkill = -1;
            return "MOVING";
        }
        if (EventSkillRequest())
        {
            // user wants to cast a skill.
            // check self (alive, mana, weapon etc.) and target and distance
            Skill skill = skills[currentSkill];
            nextTarget = target; // return to this one after any corrections by CastCheckTarget
            Vector3 destination;
            if (CastCheckSelf(skill) && CastCheckTarget(skill) && CastCheckDistance(skill, out destination))
            {
                // start casting and cancel movement in any case
                // (player might move into attack range * 0.8 but as soon as we
                //  are close enough to cast, we fully commit to the cast.)
                agent.ResetMovement();
                StartCastSkill(skill);
                return "CASTING";
            }
            else
            {
                // checks failed. stop trying to cast.
                currentSkill = -1;
                return "IDLE";
            }
        }
        if (EventSkillFinished()) {} // don't care
        if (EventMoveEnd()) {} // don't care
        if (EventTradeDone()) {} // don't care
        if (EventCraftingDone()) {} // don't care
        if (EventRespawn()) {} // don't care
        if (EventTargetDied()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care

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
            return "DEAD";
        }
        if (EventStunned())
        {
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventMoveEnd())
        {
            // finished moving. do whatever we did before.
            return "IDLE";
        }
        if (EventCancelAction())
        {
            // cancel casting (if any) and stop moving
            currentSkill = -1;
            agent.ResetMovement();
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            // cancel casting (if any), stop moving, set target, go to trading
            currentSkill = -1;
            agent.ResetMovement();
            target = FindPlayerFromTradeInvitation();
            return "TRADING";
        }
        if (EventCraftingStarted())
        {
            // cancel casting (if any), stop moving, go to crafting
            currentSkill = -1;
            agent.ResetMovement();
            return "CRAFTING";
        }
        if (EventSkillRequest())
        {
            // if and where we keep moving depends on the skill and the target
            // check self (alive, mana, weapon etc.) and target
            Skill skill = skills[currentSkill];
            nextTarget = target; // return to this one after any corrections by CastCheckTarget
            Vector3 destination;
            if (CastCheckSelf(skill) && CastCheckTarget(skill) && CastCheckDistance(skill, out destination))
            {
                // start casting and cancel movement in any case
                agent.ResetMovement();
                StartCastSkill(skill);
                return "CASTING";
            }
            else
            {
                // invalid target. stop trying to cast, but keep moving.
                currentSkill = -1;
                return "MOVING";
            }
        }
        if (EventMoveStart()) {} // don't care
        if (EventSkillFinished()) {} // don't care
        if (EventTradeDone()) {} // don't care
        if (EventCraftingDone()) {} // don't care
        if (EventRespawn()) {} // don't care
        if (EventTargetDied()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care

        return "MOVING"; // nothing interesting happened
    }

    void UseNextTargetIfAny()
    {
        // use next target if the user tried to target another while casting
        // (target is locked while casting so skill isn't applied to an invalid
        //  target accidentally)
        if (nextTarget != null)
        {
            target = nextTarget;
            nextTarget = null;
        }
    }

    [Server]
    string UpdateServer_CASTING()
    {
        // keep looking at the target for server & clients (only Y rotation)
        if (target) LookAtY(target.transform.position);

        // events sorted by priority (e.g. target doesn't matter if we died)
        //
        // IMPORTANT: nextTarget might have been set while casting, so make sure
        // to handle it in any case here. it should definitely be null again
        // after casting was finished.
        // => this way we can reliably display nextTarget on the client if it's
        //    != null, so that UITarget always shows nextTarget>target
        //    (this just feels better)
        if (EventDied())
        {
            // we died.
            OnDeath();
            UseNextTargetIfAny(); // if user selected a new target while casting
            return "DEAD";
        }
        if (EventStunned())
        {
            currentSkill = -1;
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            // we do NOT cancel the cast if the player moved, and here is why:
            // * local player might move into cast range and then try to cast.
            // * server then receives the Cmd, goes to CASTING state, then
            //   receives one of the last movement updates from the local player
            //   which would cause EventMoveStart and cancel the cast.
            // * this is the price for rubberband movement.
            // => if the player wants to cast and got close enough, then we have
            //    to fully commit to it. there is no more way out except via
            //    cancel action. any movement in here is to be rejected.
            //    (many popular MMOs have the same behaviour too)
            agent.ResetMovement();
            return "CASTING";
        }
        if (EventCancelAction())
        {
            // cancel casting
            currentSkill = -1;
            UseNextTargetIfAny(); // if user selected a new target while casting
            return "IDLE";
        }
        if (EventTradeStarted())
        {
            // cancel casting (if any), stop moving, set target, go to trading
            currentSkill = -1;
            agent.ResetMovement();

            // set target to trade target instead of next target (clear that)
            target = FindPlayerFromTradeInvitation();
            nextTarget = null;
            return "TRADING";
        }
        if (EventTargetDisappeared())
        {
            // cancel if the target matters for this skill
            if (skills[currentSkill].cancelCastIfTargetDied)
            {
                currentSkill = -1;
                UseNextTargetIfAny(); // if user selected a new target while casting
                return "IDLE";
            }
        }
        if (EventTargetDied())
        {
            // cancel if the target matters for this skill
            if (skills[currentSkill].cancelCastIfTargetDied)
            {
                currentSkill = -1;
                UseNextTargetIfAny(); // if user selected a new target while casting
                return "IDLE";
            }
        }
        if (EventSkillFinished())
        {
            // apply the skill after casting is finished
            // note: we don't check the distance again. it's more fun if players
            //       still cast the skill if the target ran a few steps away
            Skill skill = skills[currentSkill];

            // apply the skill on the target
            FinishCastSkill(skill);

            // clear current skill for now
            currentSkill = -1;

            // target-based skill and no more valid target? then clear
            // (otherwise IDLE will get an unnecessary skill request and mess
            //  with targeting)
            bool validTarget = target != null && target.health > 0;
            if (currentSkill != -1 && skills[currentSkill].cancelCastIfTargetDied && !validTarget)
                currentSkill = -1;

            // use next target if the user tried to target another while casting
            UseNextTargetIfAny();

            // go back to IDLE
            return "IDLE";
        }
        if (EventMoveEnd()) {} // don't care
        if (EventTradeDone()) {} // don't care
        if (EventCraftingStarted()) {} // don't care
        if (EventCraftingDone()) {} // don't care
        if (EventRespawn()) {} // don't care
        if (EventSkillRequest()) {} // don't care

        return "CASTING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_STUNNED()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died.
            OnDeath();
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
    string UpdateServer_TRADING()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died, stop trading. other guy will receive targetdied event.
            OnDeath();
            TradeCleanup();
            return "DEAD";
        }
        if (EventStunned())
        {
            // stop trading
            currentSkill = -1;
            agent.ResetMovement();
            TradeCleanup();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            // reject movement while trading
            agent.ResetMovement();
            return "TRADING";
        }
        if (EventCancelAction())
        {
            // stop trading
            TradeCleanup();
            return "IDLE";
        }
        if (EventTargetDisappeared())
        {
            // target disconnected, stop trading
            TradeCleanup();
            return "IDLE";
        }
        if (EventTargetDied())
        {
            // target died, stop trading
            TradeCleanup();
            return "IDLE";
        }
        if (EventTradeDone())
        {
            // someone canceled or we finished the trade. stop trading
            TradeCleanup();
            return "IDLE";
        }
        if (EventMoveEnd()) {} // don't care
        if (EventSkillFinished()) {} // don't care
        if (EventCraftingStarted()) {} // don't care
        if (EventCraftingDone()) {} // don't care
        if (EventRespawn()) {} // don't care
        if (EventTradeStarted()) {} // don't care
        if (EventSkillRequest()) {} // don't care

        return "TRADING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_CRAFTING()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventDied())
        {
            // we died, stop crafting
            OnDeath();
            return "DEAD";
        }
        if (EventStunned())
        {
            // stop crafting
            agent.ResetMovement();
            return "STUNNED";
        }
        if (EventMoveStart())
        {
            // reject movement while crafting
            agent.ResetMovement();
            return "CRAFTING";
        }
        if (EventCraftingDone())
        {
            // finish crafting
            Craft();
            return "IDLE";
        }
        if (EventCancelAction()) {} // don't care. user pressed craft, we craft.
        if (EventTargetDisappeared()) {} // don't care
        if (EventTargetDied()) {} // don't care
        if (EventMoveEnd()) {} // don't care
        if (EventSkillFinished()) {} // don't care
        if (EventRespawn()) {} // don't care
        if (EventTradeStarted()) {} // don't care
        if (EventTradeDone()) {} // don't care
        if (EventCraftingStarted()) {} // don't care
        if (EventSkillRequest()) {} // don't care

        return "CRAFTING"; // nothing interesting happened
    }

    [Server]
    string UpdateServer_DEAD()
    {
        // events sorted by priority (e.g. target doesn't matter if we died)
        if (EventRespawn())
        {
            // revive to closest spawn, with 50% health, then go to idle
            Transform start = NetworkManager.singleton.GetNearestStartPosition(transform.position);
            agent.Warp(start.position); // recommended over transform.position
            Revive(0.5f);
            return "IDLE";
        }
        if (EventMoveStart())
        {
            // this should never happen, rubberband should prevent from moving
            // while dead.
            Debug.LogWarning("Player " + name + " moved while dead. This should not happen.");
            return "DEAD";
        }
        if (EventMoveEnd()) {} // don't care
        if (EventSkillFinished()) {} // don't care
        if (EventDied()) {} // don't care
        if (EventCancelAction()) {} // don't care
        if (EventTradeStarted()) {} // don't care
        if (EventTradeDone()) {} // don't care
        if (EventCraftingStarted()) {} // don't care
        if (EventCraftingDone()) {} // don't care
        if (EventTargetDisappeared()) {} // don't care
        if (EventTargetDied()) {} // don't care
        if (EventSkillRequest()) {} // don't care

        return "DEAD"; // nothing interesting happened
    }

    [Server]
    protected override string UpdateServer()
    {
        if (state == "IDLE")     return UpdateServer_IDLE();
        if (state == "MOVING")   return UpdateServer_MOVING();
        if (state == "CASTING")  return UpdateServer_CASTING();
        if (state == "STUNNED")  return UpdateServer_STUNNED();
        if (state == "TRADING")  return UpdateServer_TRADING();
        if (state == "CRAFTING") return UpdateServer_CRAFTING();
        if (state == "DEAD")     return UpdateServer_DEAD();
        Debug.LogError("invalid state:" + state);
        return "IDLE";
    }

    // finite state machine - client ///////////////////////////////////////////
    [Client]
    protected override void UpdateClient()
    {
        if (state == "IDLE" || state == "MOVING")
        {
            if (isLocalPlayer)
            {
                // simply accept input
                SelectionHandling();
                WASDHandling();
                TargetNearest();

                // canel action if escape key was pressed
                if (Input.GetKeyDown(KeyCode.Escape)) CmdCancelAction();

                // trying to cast a skill on a monster that wasn't in range?
                // then check if we walked into attack range by now
                if (useSkillWhenCloser != -1)
                {
                    // can we still attack the target? maybe it was switched.
                    if (CanAttack(target))
                    {
                        // in range already?
                        // -> we don't use CastCheckDistance because we want to
                        // move a bit closer (attackToMoveRangeRatio)
                        float range = skills[useSkillWhenCloser].castRange * attackToMoveRangeRatio;
                        if (Utils.ClosestDistance(collider, target.collider) <= range)
                        {
                            // then stop moving and start attacking
                            CmdUseSkill(useSkillWhenCloser);

                            // reset
                            useSkillWhenCloser = -1;
                        }
                        // otherwise keep walking there. the target might move
                        // around or run away, so we need to keep adjusting the
                        // destination all the time
                        else
                        {
                            //Debug.Log("walking closer to target...");
                            agent.stoppingDistance = range * attackToMoveRangeRatio;
                            agent.destination = target.collider.ClosestPoint(transform.position);
                        }
                    }
                    // otherwise reset
                    else useSkillWhenCloser = -1;
                }
            }
        }
        else if (state == "CASTING")
        {
            // keep looking at the target for server & clients (only Y rotation)
            if (target) LookAtY(target.transform.position);

            if (isLocalPlayer)
            {
                // simply accept input and reset any client sided movement
                SelectionHandling();
                WASDHandling(); // still call this to set pendingVelocity for after cast
                TargetNearest();
                agent.ResetMovement();

                // canel action if escape key was pressed
                if (Input.GetKeyDown(KeyCode.Escape)) CmdCancelAction();
            }
        }
        else if (state == "STUNNED")
        {
            if (isLocalPlayer)
            {
                // simply accept input and reset any client sided movement
                SelectionHandling();
                TargetNearest();
                agent.ResetMovement();

                // canel action if escape key was pressed
                if (Input.GetKeyDown(KeyCode.Escape)) CmdCancelAction();
            }
        }
        else if (state == "TRADING") {}
        else if (state == "CRAFTING") {}
        else if (state == "DEAD") {}
        else Debug.LogError("invalid state:" + state);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "UpdateClient_");
    }

    // overlays ////////////////////////////////////////////////////////////////
    protected override void UpdateOverlays()
    {
        base.UpdateOverlays();

        if (nameOverlay != null)
        {
            // find local player (null while in character selection)
            if (localPlayer != null)
            {
                // note: murderer has higher priority (a player can be a murderer and an
                // offender at the same time)
                if (IsMurderer())
                    nameOverlay.color = nameOverlayMurdererColor;
                else if (IsOffender())
                    nameOverlay.color = nameOverlayOffenderColor;
                // member of the same party
                else if (localPlayer.InParty() && localPlayer.party.GetMemberIndex(name) != -1)
                    nameOverlay.color = nameOverlayPartyColor;
                // otherwise default
                else
                    nameOverlay.color = nameOverlayDefaultColor;
            }
        }

        if (guildOverlay != null)
            guildOverlay.text = guildName != "" ? guildOverlayPrefix + guildName + guildOverlaySuffix : "";
    }

    // skill finished event & pending actions //////////////////////////////////
    // pending actions while casting. to be applied after cast.
    int pendingSkill = -1;
    Vector3 pendingDestination;
    bool pendingDestinationValid;
    Vector3 pendingVelocity;
    bool pendingVelocityValid;

    // client event when skill cast finished on server
    // -> useful for follow up attacks etc.
    //    (doing those on server won't really work because the target might have
    //     moved, in which case we need to follow, which we need to do on the
    //     client)
    [Client]
    void OnSkillCastFinished(Skill skill)
    {
        if (!isLocalPlayer) return;

        // tried to click move somewhere?
        if (pendingDestinationValid)
        {
            agent.stoppingDistance = 0;
            agent.destination = pendingDestination;
        }
        // tried to wasd move somewhere?
        else if (pendingVelocityValid)
        {
            agent.velocity = pendingVelocity;
        }
        // user pressed another skill button?
        else if (pendingSkill != -1)
        {
            TryUseSkill(pendingSkill, true);
        }
        // otherwise do follow up attack if no interruptions happened
        else if (skill.followupDefaultAttack)
        {
            TryUseSkill(0, true);
        }

        // clear pending actions in any case
        pendingSkill = -1;
        pendingDestinationValid = false;
        pendingVelocityValid = false;
    }

    // attributes //////////////////////////////////////////////////////////////
    public int AttributesSpendable()
    {
        // calculate the amount of attribute points that can still be spent
        // -> one point per level
        // -> we don't need to store the points in an extra variable, we can
        //    simply decrease the attribute points spent from the level
        return level - (strength + intelligence);
    }

    [Command]
    public void CmdIncreaseStrength()
    {
        // validate
        if (health > 0 && AttributesSpendable() > 0) ++strength;
    }

    [Command]
    public void CmdIncreaseIntelligence()
    {
        // validate
        if (health > 0 && AttributesSpendable() > 0) ++intelligence;
    }

    // combat //////////////////////////////////////////////////////////////////
    // helper function to calculate the experience rewards for sharing parties
    public static long CalculatePartyExperienceShare(long total, int memberCount, float bonusPercentagePerMember, int memberLevel, int killedLevel)
    {
        // bonus percentage based on how many members there are
        float bonusPercentage = (memberCount-1) * bonusPercentagePerMember;

        // calculate the share via ceil, so that uneven numbers still result in
        // at least 'total' in the end.
        // e.g. 4/2=2 (good); 5/2=2 (1 point got lost)
        long share = (long)Mathf.Ceil(total / (float)memberCount);

        // balance experience reward for the receiver's level. this is important
        // to avoid crazy power leveling where a level 1 hero would get a LOT of
        // level ups if his friend kills a level 100 monster once.
        long balanced = BalanceExpReward(share, memberLevel, killedLevel);
        long bonus = Convert.ToInt64(balanced * bonusPercentage);

        return balanced + bonus;
    }

    [Server]
    public void OnDamageDealtToMonster(Monster monster)
    {
        // did we kill it?
        if (monster.health == 0)
        {
            // share kill rewards with party or only for self
            List<Player> closeMembers = InParty() ? GetPartyMembersInProximity() : new List<Player>();

            // share experience & skill experience
            // note: bonus only applies to exp. share parties, otherwise
            //       there's an unnecessary pressure to always join a
            //       party when leveling alone too.
            // note: if monster.rewardExp is 10 then it's possible that
            //       two members only receive 2 exp each (= 4 total).
            //       this happens because of exp balancing by level and
            //       is as intended.
            if (InParty() && party.shareExperience)
            {
                foreach (Player member in closeMembers)
                {
                    member.experience += CalculatePartyExperienceShare(
                        monster.rewardExperience,
                        closeMembers.Count,
                        Party.BonusExperiencePerMember,
                        member.level,
                        monster.level
                    );
                    member.skillExperience += CalculatePartyExperienceShare(
                        monster.rewardSkillExperience,
                        closeMembers.Count,
                        Party.BonusExperiencePerMember,
                        member.level,
                        monster.level
                    );
                }
            }
            else
            {
                skillExperience += BalanceExpReward(monster.rewardSkillExperience, level, monster.level);
                experience += BalanceExpReward(monster.rewardExperience, level, monster.level);
            }

            // give pet the same exp without dividing it, but balance it
            // => AFTER player exp reward! pet can only ever level up to player
            //    level, so it's best if the player gets exp and level-ups
            //    first, then afterwards we try to level up the pet.
            if (activePet != null)
                activePet.experience += BalanceExpReward(monster.rewardExperience, activePet.level, monster.level);

            // increase quest kill counter for all party members
            if (InParty())
            {
                foreach (Player member in closeMembers)
                    member.QuestsOnKilled(monster);
            }
            else QuestsOnKilled(monster);
        }
    }

    [Server]
    public void OnDamageDealtToPlayer(Player player)
    {
        // was he innocent?
        if (!player.IsOffender() && !player.IsMurderer())
        {
            // did we kill him? then start/reset murder status
            // did we just attack him? then start/reset offender status
            // (unless we are already a murderer)
            if (player.health == 0) StartMurderer();
            else if (!IsMurderer()) StartOffender();
        }
    }

    [Server]
    public void OnDamageDealtToPet(Pet pet)
    {
        // was he innocent?
        if (!pet.owner.IsOffender() && !pet.owner.IsMurderer())
        {
            // did we kill him? then start/reset murder status
            // did we just attack him? then start/reset offender status
            // (unless we are already a murderer)
            if (pet.health == 0) StartMurderer();
            else if (!IsMurderer()) StartOffender();
        }
    }

    // custom DealDamageAt function that also rewards experience if we killed
    // the monster
    [Server]
    public override void DealDamageAt(Entity entity, int amount, float stunChance=0, float stunTime=0)
    {
        // deal damage with the default function
        base.DealDamageAt(entity, amount, stunChance, stunTime);

        // a monster?
        if (entity is Monster)
        {
            OnDamageDealtToMonster((Monster)entity);
        }
        // a player?
        // (see murder code section comments to understand the system)
        else if (entity is Player)
        {
            OnDamageDealtToPlayer((Player)entity);
        }
        // a pet?
        // (see murder code section comments to understand the system)
        else if (entity is Pet)
        {
            OnDamageDealtToPet((Pet)entity);
        }

        // let pet know that we attacked something
        if (activePet != null && activePet.autoAttack)
            activePet.OnAggro(entity);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "DealDamageAt_", entity, amount);
    }

    // experience //////////////////////////////////////////////////////////////
    public float ExperiencePercent()
    {
        return (experience != 0 && experienceMax != 0) ? (float)experience / (float)experienceMax : 0;
    }

    // players gain exp depending on their level. if a player has a lower level
    // than the monster, then he gains more exp (up to 100% more) and if he has
    // a higher level, then he gains less exp (up to 100% less)
    // -> test with monster level 20 and expreward of 100:
    //   BalanceExpReward( 1, 20, 100)); => 200
    //   BalanceExpReward( 9, 20, 100)); => 200
    //   BalanceExpReward(10, 20, 100)); => 200
    //   BalanceExpReward(11, 20, 100)); => 190
    //   BalanceExpReward(12, 20, 100)); => 180
    //   BalanceExpReward(13, 20, 100)); => 170
    //   BalanceExpReward(14, 20, 100)); => 160
    //   BalanceExpReward(15, 20, 100)); => 150
    //   BalanceExpReward(16, 20, 100)); => 140
    //   BalanceExpReward(17, 20, 100)); => 130
    //   BalanceExpReward(18, 20, 100)); => 120
    //   BalanceExpReward(19, 20, 100)); => 110
    //   BalanceExpReward(20, 20, 100)); => 100
    //   BalanceExpReward(21, 20, 100)); =>  90
    //   BalanceExpReward(22, 20, 100)); =>  80
    //   BalanceExpReward(23, 20, 100)); =>  70
    //   BalanceExpReward(24, 20, 100)); =>  60
    //   BalanceExpReward(25, 20, 100)); =>  50
    //   BalanceExpReward(26, 20, 100)); =>  40
    //   BalanceExpReward(27, 20, 100)); =>  30
    //   BalanceExpReward(28, 20, 100)); =>  20
    //   BalanceExpReward(29, 20, 100)); =>  10
    //   BalanceExpReward(30, 20, 100)); =>   0
    //   BalanceExpReward(31, 20, 100)); =>   0
    public static long BalanceExpReward(long reward, int attackerLevel, int victimLevel)
    {
        int levelDiff = Mathf.Clamp(victimLevel - attackerLevel, -10, 10);
        float multiplier = 1 + levelDiff * 0.1f;
        return Convert.ToInt64(reward * multiplier);
    }

    // aggro ///////////////////////////////////////////////////////////////////
    // this function is called by entities that attack us
    [ServerCallback]
    public override void OnAggro(Entity entity)
    {
        // forward to pet if it's supposed to defend us
        if (activePet != null && activePet.defendOwner)
            activePet.OnAggro(entity);
    }

    // death ///////////////////////////////////////////////////////////////////
    [Server]
    protected override void OnDeath()
    {
        // take care of entity stuff
        base.OnDeath();

        // lose experience
        long loss = Convert.ToInt64(experienceMax * deathExperienceLossPercent);
        experience -= loss;

        // send an info chat message
        string message = "You died and lost " + loss + " experience.";
        chat.TargetMsgInfo(connectionToClient, message);

        // addon system hooks
        Utils.InvokeMany(typeof(Player), this, "OnDeath_");
    }

    // loot ////////////////////////////////////////////////////////////////////
    [Command]
    public void CmdTakeLootGold()
    {
        // validate: dead monster and close enough?
        // use collider point(s) to also work with big entities
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            target != null && target is Monster && target.health == 0 &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange)
        {
            // distribute reward through party or to self
            if (InParty() && party.shareGold)
            {
                // find all party members in observer range
                // (we don't distribute it all across the map. standing
                //  next to each other is a better experience. players
                //  can't just stand safely in a city while gaining exp)
                List<Player> closeMembers = GetPartyMembersInProximity();

                // calculate the share via ceil, so that uneven numbers
                // still result in at least total gold in the end.
                // e.g. 4/2=2 (good); 5/2=2 (1 gold got lost)
                long share = (long)Mathf.Ceil((float)target.gold / (float)closeMembers.Count);

                // now distribute
                foreach (Player member in closeMembers)
                    member.gold += share;
            }
            else
            {
                gold += target.gold;
            }

            // reset target gold
            target.gold = 0;
        }
    }

    [Command]
    public void CmdTakeLootItem(int index)
    {
        // validate: dead monster and close enough and valid loot index?
        // use collider point(s) to also work with big entities
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            target != null && target is Monster && target.health == 0 &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            0 <= index && index < target.inventory.Count &&
            target.inventory[index].amount > 0)
        {
            ItemSlot slot = target.inventory[index];

            // try to add it to the inventory, clear monster slot if it worked
            if (InventoryAdd(slot.item, slot.amount))
            {
                slot.amount = 0;
                target.inventory[index] = slot;
            }
        }
    }

    // inventory ///////////////////////////////////////////////////////////////
    [Command]
    public void CmdSwapInventoryTrash(int inventoryIndex)
    {
        // dragging an inventory item to the trash always overwrites the trash
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= inventoryIndex && inventoryIndex < inventory.Count)
        {
            // inventory slot has to be valid and destroyable and not summoned
            ItemSlot slot = inventory[inventoryIndex];
            if (slot.amount > 0 && slot.item.destroyable && !slot.item.petSummoned)
            {
                // overwrite trash
                trash = slot;

                // clear inventory slot
                slot.amount = 0;
                inventory[inventoryIndex] = slot;
            }
        }
    }

    [Command]
    public void CmdSwapTrashInventory(int inventoryIndex)
    {
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= inventoryIndex && inventoryIndex < inventory.Count)
        {
            // inventory slot has to be empty or destroyable
            ItemSlot slot = inventory[inventoryIndex];
            if (slot.amount == 0 || slot.item.destroyable)
            {
                // swap them
                inventory[inventoryIndex] = trash;
                trash = slot;
            }
        }
    }

    [Command]
    public void CmdSwapInventoryInventory(int fromIndex, int toIndex) {
        // note: should never send a command with complex types!
        // validate: make sure that the slots actually exist in the inventory
        // and that they are not equal
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= fromIndex && fromIndex < inventory.Count &&
            0 <= toIndex && toIndex < inventory.Count &&
            fromIndex != toIndex)
        {
            // swap them
            ItemSlot temp = inventory[fromIndex];
            inventory[fromIndex] = inventory[toIndex];
            inventory[toIndex] = temp;
        }
    }

    [Command]
    public void CmdInventorySplit(int fromIndex, int toIndex)
    {
        // note: should never send a command with complex types!
        // validate: make sure that the slots actually exist in the inventory
        // and that they are not equal
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= fromIndex && fromIndex < inventory.Count &&
            0 <= toIndex && toIndex < inventory.Count &&
            fromIndex != toIndex)
        {
            // slotFrom needs at least two to split, slotTo has to be empty
            ItemSlot slotFrom = inventory[fromIndex];
            ItemSlot slotTo = inventory[toIndex];
            if (slotFrom.amount >= 2 && slotTo.amount == 0)
            {
                // split them serversided (has to work for even and odd)
                slotTo = slotFrom; // copy the value

                slotTo.amount = slotFrom.amount / 2;
                slotFrom.amount -= slotTo.amount; // works for odd too

                // put back into the list
                inventory[fromIndex] = slotFrom;
                inventory[toIndex] = slotTo;
            }
        }
    }

    [Command]
    public void CmdInventoryMerge(int fromIndex, int toIndex)
    {
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= fromIndex && fromIndex < inventory.Count &&
            0 <= toIndex && toIndex < inventory.Count &&
            fromIndex != toIndex)
        {
            // both items have to be valid
            ItemSlot slotFrom = inventory[fromIndex];
            ItemSlot slotTo = inventory[toIndex];
            if (slotFrom.amount > 0 && slotTo.amount > 0)
            {
                // make sure that items are the same type
                // note: .Equals because name AND dynamic variables matter (petLevel etc.)
                if (slotFrom.item.Equals(slotTo.item))
                {
                    // merge from -> to
                    // put as many as possible into 'To' slot
                    int put = slotTo.IncreaseAmount(slotFrom.amount);
                    slotFrom.DecreaseAmount(put);

                    // put back into the list
                    inventory[fromIndex] = slotFrom;
                    inventory[toIndex] = slotTo;
                }
            }
        }
    }

    [ClientRpc]
    public void RpcUsedItem(Item item)
    {
        // validate
        if (item.data is UsableItem)
        {
            UsableItem itemData = (UsableItem)item.data;
            itemData.OnUsed(this);
        }
    }

    [Command]
    public void CmdUseInventoryItem(int index)
    {
        // validate
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= index && index < inventory.Count && inventory[index].amount > 0 &&
            inventory[index].item.data is UsableItem)
        {
            // use item
            // note: we don't decrease amount / destroy in all cases because
            // some items may swap to other slots in .Use()
            UsableItem itemData = (UsableItem)inventory[index].item.data;
            if (itemData.CanUse(this, index))
            {
                // .Use might clear the slot, so we backup the Item first for the Rpc
                Item item = inventory[index].item;
                itemData.Use(this, index);
                RpcUsedItem(item);
            }
        }
    }

    // equipment ///////////////////////////////////////////////////////////////
    void OnEquipmentChanged(SyncListItemSlot.Operation op, int index)
    {
        // update the model
        RefreshLocation(index);
    }

    void RebindAnimators()
    {
        foreach (Animator anim in GetComponentsInChildren<Animator>())
            anim.Rebind();
    }

    public void RefreshLocation(int index)
    {
        ItemSlot slot = equipment[index];
        EquipmentInfo info = equipmentInfo[index];

        // valid category and valid location? otherwise don't bother
        if (info.requiredCategory != "" && info.location != null)
        {
            // clear previous one in any case (when overwriting or clearing)
            if (info.location.childCount > 0) Destroy(info.location.GetChild(0).gameObject);

            //  valid item?
            if (slot.amount > 0)
            {
                // has a model? then set it
                EquipmentItem itemData = (EquipmentItem)slot.item.data;
                if (itemData.modelPrefab != null)
                {
                    // load the model
                    GameObject go = Instantiate(itemData.modelPrefab);
                    go.transform.SetParent(info.location, false);

                    // is it a skinned mesh with an animator?
                    Animator anim = go.GetComponent<Animator>();
                    if (anim != null)
                    {
                        // assign main animation controller to it
                        anim.runtimeAnimatorController = animator.runtimeAnimatorController;

                        // restart all animators, so that skinned mesh equipment will be
                        // in sync with the main animation
                        RebindAnimators();
                    }
                }
            }
        }
    }

    [Command]
    public void CmdSwapInventoryEquip(int inventoryIndex, int equipmentIndex)
    {
        // validate: make sure that the slots actually exist in the inventory
        // and in the equipment
        if (health > 0 &&
            0 <= inventoryIndex && inventoryIndex < inventory.Count &&
            0 <= equipmentIndex && equipmentIndex < equipment.Count)
        {
            // item slot has to be empty (unequip) or equipabable
            ItemSlot slot = inventory[inventoryIndex];
            if (slot.amount == 0 ||
                slot.item.data is EquipmentItem &&
                ((EquipmentItem)slot.item.data).CanEquip(this, inventoryIndex, equipmentIndex))
            {
                // swap them
                ItemSlot temp = equipment[equipmentIndex];
                equipment[equipmentIndex] = slot;
                inventory[inventoryIndex] = temp;
            }
        }
    }

    // skills //////////////////////////////////////////////////////////////////
    public override bool HasCastWeapon()
    {
        // equipped any 'Weapon...' item?
        return equipment.FindIndex(slot => slot.amount > 0 &&
            ((EquipmentItem)slot.item.data).category.StartsWith("Weapon")
        ) != -1;
    }

    // CanAttack check
    // we use 'is' instead of 'GetType' so that it works for inherited types too
    public override bool CanAttack(Entity entity)
    {
        return base.CanAttack(entity) &&
               (entity is Monster ||
                entity is Player ||
                (entity is Pet && entity != activePet));
    }

    [Command]
    public void CmdUseSkill(int skillIndex)
    {
        // validate
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= skillIndex && skillIndex < skills.Count)
        {
            // skill learned and can be casted?
            if (skills[skillIndex].level > 0 && skills[skillIndex].IsReady())
            {
                currentSkill = skillIndex;
            }
        }
    }

    // helper function: try to use a skill and walk into range if necessary
    [Client]
    public void TryUseSkill(int skillIndex, bool ignoreState=false)
    {
        // only if not casting already
        // (might need to ignore that when coming from pending skill where
        //  CASTING is still true)
        if (state != "CASTING" || ignoreState)
        {
            Skill skill = skills[skillIndex];
            if (CastCheckSelf(skill) && CastCheckTarget(skill))
            {
                // check distance between self and target
                Vector3 destination;
                if (CastCheckDistance(skill, out destination))
                {
                    // cast
                    CmdUseSkill(skillIndex);
                }
                else
                {
                    // move to the target first
                    // (use collider point(s) to also work with big entities)
                    agent.stoppingDistance = skill.castRange * attackToMoveRangeRatio;
                    agent.destination = destination;

                    // use skill when there
                    useSkillWhenCloser = skillIndex;
                }
            }
        }
        else
        {
            pendingSkill = skillIndex;
        }
    }

    public bool HasLearnedSkill(string skillName)
    {
        return skills.Any(skill => skill.name == skillName && skill.level > 0);
    }

    public bool HasLearnedSkillWithLevel(string skillName, int skillLevel)
    {
        return skills.Any(skill => skill.name == skillName && skill.level >= skillLevel);
    }

    // helper function for command and UI
    // -> this is for learning and upgrading!
    public bool CanUpgradeSkill(Skill skill)
    {
        return skill.level < skill.maxLevel &&
               level >= skill.upgradeRequiredLevel &&
               skillExperience >= skill.upgradeRequiredSkillExperience &&
               (skill.predecessor == null || (HasLearnedSkillWithLevel(skill.predecessor.name, skill.predecessorLevel)));
    }

    // -> this is for learning and upgrading!
    [Command]
    public void CmdUpgradeSkill(int skillIndex)
    {
        // validate
        if ((state == "IDLE" || state == "MOVING" || state == "CASTING") &&
            0 <= skillIndex && skillIndex < skills.Count)
        {
            // can be upgraded?
            Skill skill = skills[skillIndex];
            if (CanUpgradeSkill(skill))
            {
                // decrease skill experience
                skillExperience -= skill.upgradeRequiredSkillExperience;

                // upgrade
                ++skill.level;
                skills[skillIndex] = skill;
            }
        }
    }

    // skillbar ////////////////////////////////////////////////////////////////
    //[Client] <- disabled while UNET OnDestroy isLocalPlayer bug exists
    void SaveSkillbar()
    {
        // save skillbar to player prefs (based on player name, so that
        // each character can have a different skillbar)
        for (int i = 0; i < skillbar.Length; ++i)
            PlayerPrefs.SetString(name + "_skillbar_" + i, skillbar[i].reference);

        // force saving playerprefs, otherwise they aren't saved for some reason
        PlayerPrefs.Save();
    }

    [Client]
    void LoadSkillbar()
    {
        print("loading skillbar for " + name);
        List<Skill> learned = skills.Where(skill => skill.level > 0).ToList();
        for (int i = 0; i < skillbar.Length; ++i)
        {
            // try loading an existing entry
            if (PlayerPrefs.HasKey(name + "_skillbar_" + i))
            {
                string entry = PlayerPrefs.GetString(name + "_skillbar_" + i, "");

                // is this a valid item/equipment/learned skill?
                // (might be an old character's playerprefs)
                // => only allow learned skills (in case it's an old character's
                //    skill that we also have, but haven't learned yet)
                if (HasLearnedSkill(entry) ||
                    GetInventoryIndexByName(entry) != -1 ||
                    GetEquipmentIndexByName(entry) != -1)
                {
                    skillbar[i].reference = entry;
                }
            }
            // otherwise fill with default skills for a better first impression
            else if (i < learned.Count)
            {
                skillbar[i].reference = learned[i].name;
            }
        }
    }

    // quests //////////////////////////////////////////////////////////////////
    public int GetQuestIndexByName(string questName)
    {
        return quests.FindIndex(quest => quest.name == questName);
    }

    // helper function to check if the player has completed a quest before
    public bool HasCompletedQuest(string questName)
    {
        return quests.Any(q => q.name == questName && q.completed);
    }

    // helper function to check if a player has an active (not completed) quest
    public bool HasActiveQuest(string questName)
    {
        return quests.Any(q => q.name == questName && !q.completed);
    }

    [Server]
    public void QuestsOnKilled(Entity victim)
    {
        // call OnKilled in all active (not completed) quests
        for (int i = 0; i < quests.Count; ++i)
            if (!quests[i].completed)
                quests[i].OnKilled(this, i, victim);
    }

    [Server]
    public void QuestsOnLocation(Collider location)
    {
        // call OnLocation in all active (not completed) quests
        for (int i = 0; i < quests.Count; ++i)
            if (!quests[i].completed)
                quests[i].OnLocation(this, i, location);
    }

    // helper function to check if the player can accept a new quest
    // note: no quest.completed check needed because we have a'not accepted yet'
    //       check
    public bool CanAcceptQuest(ScriptableQuest quest)
    {
        // not too many quests yet?
        // has required level?
        // not accepted yet?
        // has finished predecessor quest (if any)?
        return quests.Count(q => !q.completed) < activeQuestLimit &&
               level >= quest.requiredLevel &&          // has required level?
               GetQuestIndexByName(quest.name) == -1 && // not accepted yet?
               (quest.predecessor == null || HasCompletedQuest(quest.predecessor.name));
    }

    [Command]
    public void CmdAcceptQuest(int npcQuestIndex)
    {
        // validate
        // use collider point(s) to also work with big entities
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            0 <= npcQuestIndex && npcQuestIndex < ((Npc)target).quests.Length &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            CanAcceptQuest(((Npc)target).quests[npcQuestIndex]))
        {
            ScriptableQuest npcQuest = ((Npc)target).quests[npcQuestIndex];
            quests.Add(new Quest(npcQuest));
        }
    }

    // helper function to check if the player can complete a quest
    public bool CanCompleteQuest(string questName)
    {
        // has the quest and not completed yet?
        int index = GetQuestIndexByName(questName);
        if (index != -1 && !quests[index].completed)
        {
            // fulfilled?
            Quest quest = quests[index];
            if(quest.IsFulfilled(this))
            {
                // enough space for reward item (if any)?
                return quest.rewardItem == null || InventoryCanAdd(new Item(quest.rewardItem), 1);
            }
        }
        return false;
    }

    [Command]
    public void CmdCompleteQuest(int npcQuestIndex)
    {
        // validate
        // use collider point(s) to also work with big entities
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            0 <= npcQuestIndex && npcQuestIndex < ((Npc)target).quests.Length &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange)
        {
            ScriptableQuest npcQuest = ((Npc)target).quests[npcQuestIndex];
            int index = GetQuestIndexByName(npcQuest.name);
            if (index != -1)
            {
                // can complete it? (also checks inventory space for reward, if any)
                Quest quest = quests[index];
                if (CanCompleteQuest(quest.name))
                {
                    // call quest.OnCompleted to remove quest items from
                    // inventory, etc.
                    quest.OnCompleted(this);

                    // gain rewards
                    gold += quest.rewardGold;
                    experience += quest.rewardExperience;
                    if (quest.rewardItem != null)
                        InventoryAdd(new Item(quest.rewardItem), 1);

                    // complete quest
                    quest.completed = true;
                    quests[index] = quest;
                }
            }
        }
    }

    // npc trading /////////////////////////////////////////////////////////////
    [Command]
    public void CmdNpcBuyItem(int index, int amount)
    {
        // validate: close enough, npc alive and valid index?
        // use collider point(s) to also work with big entities
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            0 <= index && index < ((Npc)target).saleItems.Length)
        {
            // valid amount?
            Item npcItem = new Item(((Npc)target).saleItems[index]);
            if (1 <= amount && amount <= npcItem.maxStack)
            {
                long price = npcItem.buyPrice * amount;

                // enough gold and enough space in inventory?
                if (gold >= price && InventoryCanAdd(npcItem, amount))
                {
                    // pay for it, add to inventory
                    gold -= price;
                    InventoryAdd(npcItem, amount);
                }
            }
        }
    }

    [Command]
    public void CmdNpcSellItem(int index, int amount)
    {
        // validate: close enough, npc alive and valid index and valid item?
        // use collider point(s) to also work with big entities
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            0 <= index && index < inventory.Count)
        {
            // sellable?
            ItemSlot slot = inventory[index];
            if (slot.amount > 0 && slot.item.sellable && !slot.item.petSummoned)
            {
                // valid amount?
                if (1 <= amount && amount <= slot.amount)
                {
                    // sell the amount
                    long price = slot.item.sellPrice * amount;
                    gold += price;
                    slot.DecreaseAmount(amount);
                    inventory[index] = slot;
                }
            }
        }
    }

    // npc teleport ////////////////////////////////////////////////////////////
    [Command]
    public void CmdNpcTeleport()
    {
        // validate
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            ((Npc)target).teleportTo != null)
        {
            // using agent.Warp is recommended over transform.position
            // (the latter can cause weird bugs when using it with an agent)
            agent.Warp(((Npc)target).teleportTo.position);
        }
    }

    // player to player trading ////////////////////////////////////////////////
    // how trading works:
    // 1. A invites his target with CmdTradeRequest()
    //    -> sets B.tradeInvitationFrom = A;
    // 2. B sees a UI window and accepts (= invites A too)
    //    -> sets A.tradeInvitationFrom = B;
    // 3. the TradeStart event is fired, both go to 'TRADING' state
    // 4. they lock the trades
    // 5. they accept, then items and gold are swapped

    public bool CanStartTrade()
    {
        // a player can only trade if he is not trading already and alive
        return health > 0 && state != "TRADING";
    }

    public bool CanStartTradeWith(Entity entity)
    {
        // can we trade? can the target trade? are we close enough?
        return entity != null && entity is Player && entity != this &&
               CanStartTrade() && ((Player)entity).CanStartTrade() &&
               Utils.ClosestDistance(collider, entity.collider) <= interactionRange;
    }

    // request a trade with the target player.
    [Command]
    public void CmdTradeRequestSend()
    {
        // validate
        if (CanStartTradeWith(target))
        {
            // send a trade request to target
            ((Player)target).tradeRequestFrom = name;
            print(name + " invited " + target.name + " to trade");
        }
    }

    // helper function to find the guy who sent us a trade invitation
    [Server]
    Player FindPlayerFromTradeInvitation()
    {
        if (tradeRequestFrom != "" && onlinePlayers.ContainsKey(tradeRequestFrom))
            return onlinePlayers[tradeRequestFrom];
        return null;
    }

    // accept a trade invitation by simply setting 'requestFrom' for the other
    // person to self
    [Command]
    public void CmdTradeRequestAccept()
    {
        Player sender = FindPlayerFromTradeInvitation();
        if (sender != null)
        {
            if (CanStartTradeWith(sender))
            {
                // also send a trade request to the person that invited us
                sender.tradeRequestFrom = name;
                print(name + " accepted " + sender.name + "'s trade request");
            }
        }
    }

    // decline a trade invitation
    [Command]
    public void CmdTradeRequestDecline()
    {
        tradeRequestFrom = "";
    }

    [Server]
    void TradeCleanup()
    {
        // clear all trade related properties
        tradeOfferGold = 0;
        for (int i = 0; i < tradeOfferItems.Count; ++i) tradeOfferItems[i] = -1;
        tradeStatus = TradeStatus.Free;
        tradeRequestFrom = "";
    }

    [Command]
    public void CmdTradeCancel()
    {
        // validate
        if (state == "TRADING")
        {
            // clear trade request for both guys. the FSM event will do the rest
            Player player = FindPlayerFromTradeInvitation();
            if (player != null) player.tradeRequestFrom = "";
            tradeRequestFrom = "";
        }
    }

    [Command]
    public void CmdTradeOfferLock()
    {
        // validate
        if (state == "TRADING")
            tradeStatus = TradeStatus.Locked;
    }

    [Command]
    public void CmdTradeOfferGold(long amount)
    {
        // validate
        if (state == "TRADING" && tradeStatus == TradeStatus.Free &&
            0 <= amount && amount <= gold)
            tradeOfferGold = amount;
    }

    [Command]
    public void CmdTradeOfferItem(int inventoryIndex, int offerIndex)
    {
        // validate
        if (state == "TRADING" && tradeStatus == TradeStatus.Free &&
            0 <= offerIndex && offerIndex < tradeOfferItems.Count &&
            !tradeOfferItems.Contains(inventoryIndex) && // only one reference
            0 <= inventoryIndex && inventoryIndex < inventory.Count)
        {
            ItemSlot slot = inventory[inventoryIndex];
            if (slot.amount > 0 && slot.item.tradable && !slot.item.petSummoned)
                tradeOfferItems[offerIndex] = inventoryIndex;
        }
    }

    [Command]
    public void CmdTradeOfferItemClear(int offerIndex)
    {
        // validate
        if (state == "TRADING" && tradeStatus == TradeStatus.Free &&
            0 <= offerIndex && offerIndex < tradeOfferItems.Count)
            tradeOfferItems[offerIndex] = -1;
    }

    [Server]
    bool IsTradeOfferStillValid()
    {
        // enough gold and all offered items are -1 or valid?
        return gold >= tradeOfferGold &&
               tradeOfferItems.All(index => index == -1 ||
                                            (0 <= index && index < inventory.Count && inventory[index].amount > 0));
    }

    [Server]
    int TradeOfferItemSlotAmount()
    {
        return tradeOfferItems.Count(i => i != -1);
    }

    [Server]
    int InventorySlotsNeededForTrade()
    {
        // if other guy offers 2 items and we offer 1 item then we only need
        // 2-1 = 1 slots. and the other guy would need 1-2 slots and at least 0.
        if (target != null && target is Player)
        {
            Player other = (Player)target;
            int otherAmount = other.TradeOfferItemSlotAmount();
            int myAmount = TradeOfferItemSlotAmount();
            return Mathf.Max(otherAmount - myAmount, 0);
        }
        return 0;
    }

    [Command]
    public void CmdTradeOfferAccept()
    {
        // validate
        // note: distance check already done when starting the trade
        if (state == "TRADING" && tradeStatus == TradeStatus.Locked &&
            target != null && target is Player)
        {
            Player other = (Player)target;

            // other has locked?
            if (other.tradeStatus == TradeStatus.Locked)
            {
                //  simply accept and wait for the other guy to accept too
                tradeStatus = TradeStatus.Accepted;
                print("first accept by " + name);
            }
            // other has accepted already? then both accepted now, start trade.
            else if (other.tradeStatus == TradeStatus.Accepted)
            {
                // accept
                tradeStatus = TradeStatus.Accepted;
                print("second accept by " + name);

                // both offers still valid?
                if (IsTradeOfferStillValid() && other.IsTradeOfferStillValid())
                {
                    // both have enough inventory slots?
                    // note: we don't use InventoryCanAdd here because:
                    // - current solution works if both have full inventories
                    // - InventoryCanAdd only checks one slot. here we have
                    //   multiple slots though (it could happen that we can
                    //   not add slot 2 after we did add slot 1's items etc)
                    if (InventorySlotsFree() >= InventorySlotsNeededForTrade() &&
                        other.InventorySlotsFree() >= other.InventorySlotsNeededForTrade())
                    {
                        // exchange the items by first taking them out
                        // into a temporary list and then putting them
                        // in. this guarantees that exchanging even
                        // works with full inventories

                        // take them out
                        Queue<ItemSlot> tempMy = new Queue<ItemSlot>();
                        foreach (int index in tradeOfferItems)
                        {
                            if (index != -1)
                            {
                                ItemSlot slot = inventory[index];
                                tempMy.Enqueue(slot);
                                slot.amount = 0;
                                inventory[index] = slot;
                            }
                        }

                        Queue<ItemSlot> tempOther = new Queue<ItemSlot>();
                        foreach (int index in other.tradeOfferItems)
                        {
                            if (index != -1)
                            {
                                ItemSlot slot = other.inventory[index];
                                tempOther.Enqueue(slot);
                                slot.amount = 0;
                                other.inventory[index] = slot;
                            }
                        }

                        // put them into the free slots
                        for (int i = 0; i < inventory.Count; ++i)
                            if (inventory[i].amount == 0 && tempOther.Count > 0)
                                inventory[i] = tempOther.Dequeue();

                        for (int i = 0; i < other.inventory.Count; ++i)
                            if (other.inventory[i].amount == 0 && tempMy.Count > 0)
                                other.inventory[i] = tempMy.Dequeue();

                        // did it all work?
                        if (tempMy.Count > 0 || tempOther.Count > 0)
                            Debug.LogWarning("item trade problem");

                        // exchange the gold
                        gold -= tradeOfferGold;
                        other.gold -= other.tradeOfferGold;

                        gold += other.tradeOfferGold;
                        other.gold += tradeOfferGold;
                    }
                }
                else print("trade canceled (invalid offer)");

                // clear trade request for both guys. the FSM event will do the
                // rest
                tradeRequestFrom = "";
                other.tradeRequestFrom = "";
            }
        }
    }

    // crafting ////////////////////////////////////////////////////////////////
    // the crafting system is designed to work with all kinds of commonly known
    // crafting options:
    // - item combinations: wood + stone = axe
    // - weapon upgrading: axe + gem = strong axe
    // - recipe items: axerecipe(item) + wood(item) + stone(item) = axe(item)
    //
    // players can craft at all times, not just at npcs, because that's the most
    // realistic option

    // craft the current combination of items and put result into inventory
    [Command]
    public void CmdCraft(int[] indices)
    {
        // validate: between 1 and 6, all valid, no duplicates?
        // -> can be IDLE or MOVING (in which case we reset the movement)
        if ((state == "IDLE" || state == "MOVING") &&
            indices.Length == ScriptableRecipe.recipeSize)
        {
            // find valid indices that are not '-1' and make sure there are no
            // duplicates
            List<int> validIndices = indices.Where(index => 0 <= index && index < inventory.Count && inventory[index].amount > 0).ToList();
            if (validIndices.Count > 0 && !validIndices.HasDuplicates())
            {
                // build list of item templates from valid indices
                List<ScriptableItem> items = validIndices.Select(index => inventory[index].item.data).ToList();

                // find recipe
                ScriptableRecipe recipe = ScriptableRecipe.dict.Values.ToList().Find(r => r.CanCraftWith(items)); // good enough for now
                if (recipe != null && recipe.result != null)
                {
                    // enough space?
                    Item result = new Item(recipe.result);
                    if (InventoryCanAdd(result, 1))
                    {
                        // store the crafting indices on the server. no need for
                        // a SyncList and unnecessary broadcasting.
                        // we already have a 'craftingIndices' variable anyway.
                        craftingIndices = indices.ToList();

                        // start crafting
                        craftingRequested = true;
                        craftingTimeEnd = NetworkTime.time + recipe.craftingTime;
                    }
                }
            }
        }
    }

    // finish the crafting
    void Craft()
    {
        // should only be called while CRAFTING
        // -> we already validated everything in CmdCraft. let's just craft.
        if (state == "CRAFTING")
        {
            // build list of item templates from indices
            List<int> validIndices = craftingIndices.Where(index => 0 <= index && index < inventory.Count && inventory[index].amount > 0).ToList();
            List<ScriptableItem> items = validIndices.Select(index => inventory[index].item.data).ToList();

            // find recipe
            ScriptableRecipe recipe = ScriptableRecipe.dict.Values.ToList().Find(r => r.CanCraftWith(items)); // good enough for now
            if (recipe != null && recipe.result != null)
            {
                // enough space?
                Item result = new Item(recipe.result);
                if (InventoryCanAdd(result, 1))
                {
                    // remove the ingredients from inventory in any case
                    foreach (int index in validIndices)
                    {
                        // decrease item amount
                        ItemSlot slot = inventory[index];
                        slot.DecreaseAmount(1);
                        inventory[index] = slot;
                    }

                    // roll the dice to decide if we add the result or not
                    // IMPORTANT: we use rand() < probability to decide.
                    // => UnityEngine.Random.value is [0,1] inclusive:
                    //    for 0% probability it's fine because it's never '< 0'
                    //    for 100% probability it's not because it's not always '< 1', it might be == 1
                    //    and if we use '<=' instead then it won't work for 0%
                    // => C#'s Random value is [0,1) exclusive like most random
                    //    functions. this works fine.
                    if (new System.Random().NextDouble() < recipe.probability)
                    {
                        // add result item to inventory
                        InventoryAdd(new Item(recipe.result), 1);
                        TargetCraftingSuccess(connectionToClient);
                    }
                    else
                    {
                        TargetCraftingFailed(connectionToClient);
                    }

                    // clear indices afterwards
                    // note: we set all to -1 instead of calling .Clear because
                    //       that would clear all the slots in host mode.
                    for (int i = 0; i < ScriptableRecipe.recipeSize; ++i)
                        craftingIndices[i] = -1;
                }
            }
        }
    }

    // two rpcs for results to save 1 byte for the actual result
    [TargetRpc] // only send to one client
    public void TargetCraftingSuccess(NetworkConnection target)
    {
        craftingState = CraftingState.Success;
    }

    [TargetRpc] // only send to one client
    public void TargetCraftingFailed(NetworkConnection target)
    {
        craftingState = CraftingState.Failed;
    }

    // pvp murder system ///////////////////////////////////////////////////////
    // attacking someone innocent results in Offender status
    //   (can be attacked without penalty for a short time)
    // killing someone innocent results in Murderer status
    //   (can be attacked without penalty for a long time + negative buffs)
    // attacking/killing a Offender/Murderer has no penalty
    //
    // we use buffs for the offender/status because buffs have all the features
    // that we need here.
    public bool IsOffender()
    {
        return offenderBuff != null && buffs.Any(buff => buff.name == offenderBuff.name);
    }

    public bool IsMurderer()
    {
        return murdererBuff != null && buffs.Any(buff => buff.name == murdererBuff.name);
    }

    public void StartOffender()
    {
        if (offenderBuff != null) AddOrRefreshBuff(new Buff(offenderBuff, 1));
    }

    public void StartMurderer()
    {
        if (murdererBuff != null) AddOrRefreshBuff(new Buff(murdererBuff, 1));
    }

    // item mall ///////////////////////////////////////////////////////////////
    [Command]
    public void CmdEnterCoupon(string coupon)
    {
        // only allow entering one coupon every few seconds to avoid brute force
        if (NetworkTime.time >= nextRiskyActionTime)
        {
            // YOUR COUPON VALIDATION CODE HERE
            // coins += ParseCoupon(coupon);
            Debug.Log("coupon: " + coupon + " => " + name + "@" + NetworkTime.time);
            nextRiskyActionTime = NetworkTime.time + couponWaitSeconds;
        }
    }

    [Command]
    public void CmdUnlockItem(int categoryIndex, int itemIndex)
    {
        // validate: only if alive so people can't buy resurrection potions
        // after dieing in a PvP fight etc.
        if (health > 0 &&
            0 <= categoryIndex && categoryIndex <= itemMallCategories.Length &&
            0 <= itemIndex && itemIndex <= itemMallCategories[categoryIndex].items.Length)
        {
            Item item = new Item(itemMallCategories[categoryIndex].items[itemIndex]);
            if (0 < item.itemMallPrice && item.itemMallPrice <= coins)
            {
                // try to add it to the inventory, subtract costs from coins
                if (InventoryAdd(item, 1))
                {
                    coins -= item.itemMallPrice;
                    Debug.Log(name + " unlocked " + item.name);
                }
            }
        }
    }

    // coins can't be increased by an external application while the player is
    // ingame. we use an additional table to store new orders in and process
    // them every few seconds from here. this way we can even notify the player
    // after his order was processed successfully.
    //
    // note: the alternative is to keep player.coins in the database at all
    // times, but then we need RPCs and the client needs a .coins value anyway.
    [Server]
    void ProcessCoinOrders()
    {
        List<long> orders = Database.GrabCharacterOrders(name);
        foreach (long reward in orders)
        {
            coins += reward;
            Debug.Log("Processed order for: " + name + ";" + reward);
            string message = "Processed order for: " + reward;
            chat.TargetMsgInfo(connectionToClient, message);
        }
    }

    // guild ///////////////////////////////////////////////////////////////////
    public bool InGuild()
    {
        // only if both are true, otherwise we might be in the middle of leaving
        return guildName != "" && guild.GetMemberIndex(name) != -1;
    }

    [Server]
    static void BroadcastGuildChanges(string guildName, Guild guild)
    {
        // save in database
        Database.SaveGuild(guildName, guild.notice, guild.members.ToList());

        // copy to every online member. we don't just reload from db because the
        // online status is only available in the members list
        // (call TargetRpc on that GameObject for that connection)
        foreach (GuildMember member in guild.members)
        {
            if (onlinePlayers.ContainsKey(member.name))
            {
                Player player = onlinePlayers[member.name];
                player.guildName = guildName;
                player.guild = guild;
                player.TargetGuildSync(player.connectionToClient, guild);
            }
        }
    }

    // sending guild notice and members to all observers would be bandwidth
    // overkill, so we use a targetrpc
    [TargetRpc] // only send to one client
    public void TargetGuildSync(NetworkConnection target, Guild guild)
    {
        this.guild = guild;
    }

    // helper function to clear guild variables sync for kick/leave/terminate
    [Server]
    void ClearGuild()
    {
        guildName = "";
        guild = new Guild();
        TargetGuildSync(connectionToClient, guild);
    }

    [Server]
    public void SetGuildOnline(bool online)
    {
        if (InGuild())
        {
            guild.SetOnline(name, online);
            BroadcastGuildChanges(guildName, guild);
        }
    }

    [Command]
    public void CmdGuildInviteTarget()
    {
        // validate
        if (target != null && target is Player &&
            InGuild() && !((Player)target).InGuild() &&
            guild.CanInvite(name, target.name) &&
            NetworkTime.time >= nextRiskyActionTime &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange)
        {
            // send a invite and reset risky time
            ((Player)target).guildInviteFrom = name;
            nextRiskyActionTime = NetworkTime.time + guildInviteWaitSeconds;
            print(name + " invited " + target.name + " to guild");
        }
    }

    [Command]
    public void CmdGuildInviteAccept()
    {
        // valid invitation?
        // note: no distance check because sender might be far away already
        if (!InGuild() && guildInviteFrom != "" &&
            onlinePlayers.ContainsKey(guildInviteFrom))
        {
            // can sender actually invite us?
            Player sender = onlinePlayers[guildInviteFrom];
            if (sender.InGuild() && sender.guild.CanInvite(sender.name, name))
            {
                // add self to sender's guild members list
                sender.guild.AddMember(name, level);

                // broadcast and save changes from sender to everyone
                BroadcastGuildChanges(sender.guildName, sender.guild);
                print(sender.name + " added " + name + " to guild: " + guildName);
            }
        }

        // reset guild invite in any case
        guildInviteFrom = "";
    }

    [Command]
    public void CmdGuildInviteDecline()
    {
        guildInviteFrom = "";
    }

    [Command]
    public void CmdGuildKick(string memberName)
    {
        // validate
        if (InGuild() && guild.CanKick(name, memberName))
        {
            // remove from member list
            guild.RemoveMember(memberName);

            // broadcast and save changes
            BroadcastGuildChanges(guildName, guild);

            // clear variables for the kicked person
            if (onlinePlayers.ContainsKey(memberName))
                onlinePlayers[memberName].ClearGuild();
            print(name + " kicked " + memberName + " from guild: " + guildName);
        }
    }

    [Command]
    public void CmdGuildPromote(string memberName)
    {
        // validate
        if (InGuild() && guild.CanPromote(name, memberName))
        {
            // promote the member
            guild.PromoteMember(memberName);

            // broadcast and save changes
            BroadcastGuildChanges(guildName, guild);
            print(name + " promoted " + memberName + " in guild: " + guildName);
        }
    }

    [Command]
    public void CmdGuildDemote(string memberName)
    {
        // validate
        if (InGuild() && guild.CanDemote(name, memberName))
        {
            // demote the member
            guild.DemoteMember(memberName);

            // broadcast and save changes
            BroadcastGuildChanges(guildName, guild);
            print(name + " demoted " + memberName + " in guild: " + guildName);
        }
    }

    [Command]
    public void CmdSetGuildNotice(string notice)
    {
        // validate
        // (only allow changes every few seconds to avoid bandwidth issues)
        if (InGuild() && guild.CanNotify(name) &&
            notice.Length < Guild.NoticeMaxLength &&
            NetworkTime.time >= nextRiskyActionTime)
        {
            // set notice and reset next time
            guild.notice = notice;
            nextRiskyActionTime = NetworkTime.time + Guild.NoticeWaitSeconds;

            // broadcast and save changes
            BroadcastGuildChanges(guildName, guild);
            print(name + " changed guild notice to: " + guild.notice);
        }
    }

    // helper function to check if we are near a guild manager npc
    public bool IsGuildManagerNear()
    {
        return target != null &&
               target is Npc &&
               ((Npc)target).offersGuildManagement &&
               Utils.ClosestDistance(collider, target.collider) <= interactionRange;
    }

    [Command]
    public void CmdTerminateGuild()
    {
        // validate
        if (InGuild() && IsGuildManagerNear() && guild.CanTerminate(name))
        {
            // remove guild from database
            Database.RemoveGuild(guildName);

            // clear player variables
            ClearGuild();
        }
    }

    [Command]
    public void CmdCreateGuild(string newGuildName)
    {
        // validate
        if (health > 0 && IsGuildManagerNear() && !InGuild() && gold >= Guild.CreationPrice)
        {
            if (Guild.IsValidGuildName(newGuildName) &&
                !Database.GuildExists(newGuildName)) // db check only on server, no Guild.CanCreate function because client has no DB.
            {
                // remove gold
                gold -= Guild.CreationPrice;

                // set guild and add self to members list as highest rank
                guildName = newGuildName;
                guild.notice = ""; // avoid null
                guild.AddMember(name, level, GuildRank.Master);

                // (broadcast and) save changes
                BroadcastGuildChanges(guildName, guild);
                print(name + " created guild: " + guildName);
            }
            else
            {
                string message = "Guild name invalid!"; // exists or invalid regex
                chat.TargetMsgInfo(connectionToClient, message);
            }
        }
    }

    [Command]
    public void CmdLeaveGuild()
    {
        // validate
        if (InGuild() && guild.CanLeave(name))
        {
            // remove self from members list
            guild.RemoveMember(name);

            // broadcast and save changes
            BroadcastGuildChanges(guildName, guild);

            // reset guild info and members list for the person that left
            ClearGuild();
        }
    }

    // party ///////////////////////////////////////////////////////////////////
    public bool InParty()
    {
        return party.members != null && party.members.Length > 0;
    }

    [Server]
    static void BroadcastPartyChanges(Party party)
    {
        // copy to every online member. we don't just reload from db because the
        // online status is only available in the members list
        // (call TargetRpc on that GameObject for that connection)
        foreach (string member in party.members)
        {
            if (onlinePlayers.ContainsKey(member))
            {
                Player player = onlinePlayers[member];
                player.party = party;
                player.TargetPartySync(player.connectionToClient, party);
            }
        }
    }

    // sending party info to all observers would be bandwidth overkill, so we
    // use a targetrpc
    [TargetRpc] // only send to one client
    public void TargetPartySync(NetworkConnection target, Party party)
    {
        this.party = party;
    }

    // helper function to clear party variables sync for kick/leave/dismiss
    [Server]
    void ClearParty()
    {
        party = new Party();
        TargetPartySync(connectionToClient, party);
    }

    // find party members in proximity for item/exp sharing etc.
    public List<Player> GetPartyMembersInProximity()
    {
        if (InParty())
        {
            return netIdentity.observers.Values
                                        .Select(conn => conn.playerController.GetComponent<Player>())
                                        .Where(p => party.GetMemberIndex(p.name) != -1)
                                        .ToList();
        }
        return new List<Player>();
    }

    // party invite by name (not by target) so that chat commands are possible
    // if needed
    [Command]
    public void CmdPartyInvite(string otherName)
    {
        // validate: is there someone with that name, and not self?
        if (otherName != name && onlinePlayers.ContainsKey(otherName) &&
            NetworkTime.time >= nextRiskyActionTime)
        {
            Player other = onlinePlayers[otherName];

            // can only send invite if no party yet or party isn't full and
            // have invite rights and other guy isn't in party yet
            if ((!InParty() || party.CanInvite(name)) && !other.InParty())
            {
                // send a invite and reset risky time
                other.partyInviteFrom = name;
                nextRiskyActionTime = NetworkTime.time + partyInviteWaitSeconds;
                print(name + " invited " + other.name + " to party");
            }
        }
    }

    [Command]
    public void CmdPartyInviteAccept()
    {
        // valid invitation?
        // note: no distance check because sender might be far away already
        if (!InParty() && partyInviteFrom != "" &&
            onlinePlayers.ContainsKey(partyInviteFrom))
        {
            // can sender actually invite us?
            Player sender = onlinePlayers[partyInviteFrom];

            // -> either he is in a party and can still invite someone
            if (sender.InParty() && sender.party.CanInvite(sender.name))
            {
                sender.party.AddMember(name);
                BroadcastPartyChanges(sender.party);
                print(sender.name + " added " + name + " to " + sender.party.members[0] + "'s party");
            // -> or he is not in a party and forms a new one
            }
            else if (!sender.InParty())
            {
                sender.party.AddMember(sender.name); // master
                sender.party.AddMember(name);
                BroadcastPartyChanges(sender.party);
                print(sender.name + " formed a new party with " + name);
            }
        }

        // reset party invite in any case
        partyInviteFrom = "";
    }

    [Command]
    public void CmdPartyInviteDecline()
    {
        partyInviteFrom = "";
    }

    [Command]
    public void CmdPartyKick(int memberIndex)
    {
        // validate: party master and index exists and not master?
        if (InParty() && party.members[0] == name &&
            0 < memberIndex && memberIndex < party.members.Length)
        {
            string member = party.members[memberIndex];

            // kick
            party.RemoveMember(member);

            // still enough people in it for a party?
            if (party.members.Length > 1)
            {
                BroadcastPartyChanges(party);
            }
            else
            {
                // a party requires at least two people, otherwise it's not
                // really a party anymore. if we'd keep it alive with one player
                // then he can't be invited to another party until he dismisses
                // the empty one.
                ClearParty();
            }

            // clear for the kicked person too
            if (onlinePlayers.ContainsKey(member))
                onlinePlayers[member].ClearParty();

            print(name + " kicked " + member + " from party");
        }
    }

    // version without cmd because we need to call it from the server too
    public void PartyLeave()
    {
        // validate: in party and not master?
        if (InParty() && party.members[0] != name)
        {
            // remove self from party
            party.RemoveMember(name);

            // still enough people in it for a party?
            if (party.members.Length > 1)
            {
                BroadcastPartyChanges(party);
            }
            else
            {
                // a party requires at least two people, otherwise it's not
                // really a party anymore. if we'd keep it alive with one player
                // then he can't be invited to another party until he dismisses
                // the empty one.
                if (onlinePlayers.ContainsKey(party.members[0]))
                    onlinePlayers[party.members[0]].ClearParty();
            }

            // clear for self
            ClearParty();
            print(name + " left the party");
        }
    }
    [Command]
    public void CmdPartyLeave() { PartyLeave(); }

    // version without cmd because we need to call it from the server too
    public void PartyDismiss()
    {
        // validate: is master?
        if (InParty() && party.members[0] == name)
        {
            // clear party for everyone
            foreach (string member in party.members)
            {
                if (onlinePlayers.ContainsKey(member))
                    onlinePlayers[member].ClearParty();
            }
            print(name + " dismissed the party");
        }
    }
    [Command]
    public void CmdPartyDismiss() { PartyDismiss(); }

    [Command]
    public void CmdPartySetExperienceShare(bool value)
    {
        // validate: is party master?
        if (InParty() && party.members[0] == name)
        {
            // set new value, sync to everyone else
            party.shareExperience = value;
            BroadcastPartyChanges(party);
        }
    }

    [Command]
    public void CmdPartySetGoldShare(bool value)
    {
        // validate: is party master?
        if (InParty() && party.members[0] == name)
        {
            // set new value, sync to everyone else
            party.shareGold = value;
            BroadcastPartyChanges(party);
        }
    }

    // pet /////////////////////////////////////////////////////////////////////
    [Command]
    public void CmdPetSetAutoAttack(bool value)
    {
        // validate
        if (activePet != null)
            activePet.autoAttack = value;
    }

    [Command]
    public void CmdPetSetDefendOwner(bool value)
    {
        // validate
        if (activePet != null)
            activePet.defendOwner = value;
    }

    // helper function for command and UI
    public bool CanUnsummonPet()
    {
        // only while pet and owner aren't fighting
        return activePet != null &&
               (          state == "IDLE" ||           state == "MOVING") &&
               (activePet.state == "IDLE" || activePet.state == "MOVING");
    }

    [Command]
    public void CmdPetUnsummon()
    {
        // validate
        if (CanUnsummonPet())
        {
            // destroy from world. item.summoned and activePet will be null.
            NetworkServer.Destroy(activePet.gameObject);
        }
    }

    [Command]
    public void CmdNpcRevivePet(int index)
    {
        // validate: close enough, npc alive and valid index and valid item?
        // use collider point(s) to also work with big entities
        if (state == "IDLE" &&
            target != null &&
            target.health > 0 &&
            target is Npc &&
            ((Npc)target).offersPetRevive &&
            Utils.ClosestDistance(collider, target.collider) <= interactionRange &&
            0 <= index && index < inventory.Count)
        {
            ItemSlot slot = inventory[index];
            if (slot.amount > 0 && slot.item.data is PetItem)
            {
                // verify the pet status
                PetItem itemData = (PetItem)slot.item.data;
                if (slot.item.petHealth == 0 && itemData.petPrefab != null)
                {
                    // enough gold?
                    if (gold >= itemData.petPrefab.revivePrice)
                    {
                        // pay for it, revive it
                        gold -= itemData.petPrefab.revivePrice;
                        slot.item.petHealth = itemData.petPrefab.healthMax;
                        inventory[index] = slot;
                    }
                }
            }
        }
    }

    // selection handling //////////////////////////////////////////////////////
    public void SetIndicatorViaParent(Transform parent)
    {
        if (!indicator) indicator = Instantiate(indicatorPrefab);
        indicator.transform.SetParent(parent, true);
        indicator.transform.position = parent.position;
    }

    public void SetIndicatorViaPosition(Vector3 position)
    {
        if (!indicator) indicator = Instantiate(indicatorPrefab);
        indicator.transform.parent = null;
        indicator.transform.position = position;
    }

    [Command]
    public void CmdSetTarget(NetworkIdentity ni)
    {
        // validate
        if (ni != null)
        {
            // can directly change it, or change it after casting?
            if (state == "IDLE" || state == "MOVING" || state == "STUNNED")
                target = ni.GetComponent<Entity>();
            else if (state == "CASTING")
                nextTarget = ni.GetComponent<Entity>();
        }
    }

    [Client]
    void SelectionHandling()
    {
        // click raycasting if not over a UI element & not pinching on mobile
        // note: this only works if the UI's CanvasGroup blocks Raycasts
        if (Input.GetMouseButtonDown(0) && !Utils.IsCursorOverUserInterface() && Input.touchCount <= 1)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            // raycast with local player ignore option
            RaycastHit hit;
            bool cast = localPlayerClickThrough ? Utils.RaycastWithout(ray, out hit, gameObject) : Physics.Raycast(ray, out hit);
            if (cast)
            {
                // clear requested skill in any case because if we clicked
                // somewhere else then we don't care about it anymore
                useSkillWhenCloser = -1;

                // valid target?
                Entity entity = hit.transform.GetComponent<Entity>();
                if (entity)
                {
                    // set indicator
                    SetIndicatorViaParent(hit.transform);

                    // clicked last target again? and is not self or pet?
                    if (entity == target && entity != this && entity != activePet)
                    {
                        // attackable? => attack
                        if (CanAttack(entity))
                        {
                            // do we have at least one skill to use here?
                            if (skills.Count > 0)
                            {
                                // then try to use that one
                                TryUseSkill(0);
                            }
                            // otherwise just walk there
                            // use collider point(s) to also work with big entities
                            else
                            {
                                agent.stoppingDistance = 0;
                                agent.destination = entity.collider.ClosestPoint(transform.position);
                            }
                        }
                        // npc & alive => talk
                        else if (entity is Npc && entity.health > 0)
                        {
                            // close enough to talk?
                            // use collider point(s) to also work with big entities
                            if (Utils.ClosestDistance(collider, entity.collider) <= interactionRange)
                            {
                                UINpcDialogue.singleton.Show();
                            }
                            // otherwise walk there
                            // use collider point(s) to also work with big entities
                            else
                            {
                                agent.stoppingDistance = interactionRange;
                                agent.destination = entity.collider.ClosestPoint(transform.position);
                            }
                        }
                        // monster & dead => loot
                        else if (entity is Monster && entity.health == 0)
                        {
                            // has loot? and close enough?
                            // use collider point(s) to also work with big entities
                            if (((Monster)entity).HasLoot() &&
                                Utils.ClosestDistance(collider, entity.collider) <= interactionRange)
                            {
                                UILoot.singleton.Show();
                            }
                            // otherwise walk there
                            // use collider point(s) to also work with big entities
                            else
                            {
                                agent.stoppingDistance = interactionRange;
                                agent.destination = entity.collider.ClosestPoint(transform.position);
                            }
                        }

                        // addon system hooks
                        Utils.InvokeMany(typeof(Player), this, "OnSelect_", entity);
                    }
                    // clicked a new target
                    else
                    {
                        // target it
                        CmdSetTarget(entity.netIdentity);
                    }
                }
                // otherwise it's a movement target
                else
                {
                    // set indicator and navigate to the nearest walkable
                    // destination. this prevents twitching when destination is
                    // accidentally in a room without a door etc.
                    Vector3 bestDestination = agent.NearestValidDestination(hit.point);
                    SetIndicatorViaPosition(bestDestination);

                    // casting? then set pending destination
                    if (state == "CASTING")
                    {
                        pendingDestination = bestDestination;
                        pendingDestinationValid = true;
                    }
                    else
                    {
                        agent.stoppingDistance = 0;
                        agent.destination = bestDestination;
                    }
                }
            }
        }
    }

    [Client]
    void WASDHandling()
    {
        // don't move if currently typing in an input
        // we check this after checking h and v to save computations
        if (!UIUtils.AnyInputActive())
        {
            // get horizontal and vertical input
            // note: no != 0 check because it's 0 when we stop moving rapidly
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (horizontal != 0 || vertical != 0)
            {
                // create input vector, normalize in case of diagonal movement
                Vector3 input = new Vector3(horizontal, 0, vertical);
                if (input.magnitude > 1) input = input.normalized;

                // get camera rotation without up/down angle, only left/right
                Vector3 angles = Camera.main.transform.rotation.eulerAngles;
                angles.x = 0;
                Quaternion rotation = Quaternion.Euler(angles); // back to quaternion

                // calculate input direction relative to camera rotation
                Vector3 direction = rotation * input;

                // draw direction for debugging
                Debug.DrawLine(transform.position, transform.position + direction, Color.green, 0, false);

                // clear indicator if there is one, and if it's not on a target
                // (simply looks better)
                if (direction != Vector3.zero && indicator != null && indicator.transform.parent == null)
                    Destroy(indicator);

                // cancel path if we are already doing click movement, otherwise
                // we will slide
                agent.ResetMovement();

                // casting? then set pending velocity
                if (state == "CASTING")
                {
                    pendingVelocity = direction * speed;
                    pendingVelocityValid = true;
                }
                else
                {
                    // set velocity
                    agent.velocity = direction * speed;

                    // moving with velocity doesn't look at the direction, do it manually
                    LookAtY(transform.position + direction);
                }

                // clear requested skill in any case because if we clicked
                // somewhere else then we don't care about it anymore
                useSkillWhenCloser = -1;
            }
        }
    }

    // simple tab targeting
    [Client]
    void TargetNearest()
    {
        if (Input.GetKeyDown(targetNearestKey))
        {
            // find all monsters that are alive, sort by distance
            GameObject[] objects = GameObject.FindGameObjectsWithTag("Monster");
            List<Monster> monsters = objects.Select(go => go.GetComponent<Monster>()).Where(m => m.health > 0).ToList();
            List<Monster> sorted = monsters.OrderBy(m => Vector3.Distance(transform.position, m.transform.position)).ToList();

            // target nearest one
            if (sorted.Count > 0)
            {
                SetIndicatorViaParent(sorted[0].transform);
                CmdSetTarget(sorted[0].netIdentity);
            }
        }
    }

    // ontrigger ///////////////////////////////////////////////////////////////
    new void OnTriggerEnter(Collider col)
    {
        // call base function too
        base.OnTriggerEnter(col);

        // quest location?
        if (col.tag == "QuestLocation")
            QuestsOnLocation(col);
    }

    // drag and drop ///////////////////////////////////////////////////////////
    void OnDragAndDrop_InventorySlot_InventorySlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo

        // merge? check Equals because name AND dynamic variables matter (petLevel etc.)
        if (inventory[slotIndices[0]].amount > 0 && inventory[slotIndices[1]].amount > 0 &&
            inventory[slotIndices[0]].item.Equals(inventory[slotIndices[1]].item))
        {
            CmdInventoryMerge(slotIndices[0], slotIndices[1]);
        }
        // split?
        else if (Utils.AnyKeyPressed(inventorySplitKeys))
        {
            CmdInventorySplit(slotIndices[0], slotIndices[1]);
        }
        // swap?
        else
        {
            CmdSwapInventoryInventory(slotIndices[0], slotIndices[1]);
        }
    }

    void OnDragAndDrop_InventorySlot_TrashSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        CmdSwapInventoryTrash(slotIndices[0]);
    }

    void OnDragAndDrop_InventorySlot_EquipmentSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        CmdSwapInventoryEquip(slotIndices[0], slotIndices[1]);
    }

    void OnDragAndDrop_InventorySlot_SkillbarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        skillbar[slotIndices[1]].reference = inventory[slotIndices[0]].item.name; // just save it clientsided
    }

    void OnDragAndDrop_InventorySlot_NpcSellSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        ItemSlot slot = inventory[slotIndices[0]];
        if (slot.item.sellable && !slot.item.petSummoned)
        {
            UINpcTrading.singleton.sellIndex = slotIndices[0];
            UINpcTrading.singleton.sellAmountInput.text = slot.amount.ToString();
        }
    }

    void OnDragAndDrop_InventorySlot_TradingSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        if (inventory[slotIndices[0]].item.tradable)
            CmdTradeOfferItem(slotIndices[0], slotIndices[1]);
    }

    void OnDragAndDrop_InventorySlot_CraftingIngredientSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            if (!craftingIndices.Contains(slotIndices[0]))
            {
                craftingIndices[slotIndices[1]] = slotIndices[0];
                craftingState = CraftingState.None; // reset state
            }
        }
    }

    void OnDragAndDrop_TrashSlot_InventorySlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        CmdSwapTrashInventory(slotIndices[1]);
    }

    void OnDragAndDrop_EquipmentSlot_InventorySlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        CmdSwapInventoryEquip(slotIndices[1], slotIndices[0]); // reversed
    }

    void OnDragAndDrop_EquipmentSlot_SkillbarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        skillbar[slotIndices[1]].reference = equipment[slotIndices[0]].item.name; // just save it clientsided
    }

    void OnDragAndDrop_SkillsSlot_SkillbarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        skillbar[slotIndices[1]].reference = skills[slotIndices[0]].name; // just save it clientsided
    }

    void OnDragAndDrop_SkillbarSlot_SkillbarSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // just swap them clientsided
        string temp = skillbar[slotIndices[0]].reference;
        skillbar[slotIndices[0]].reference = skillbar[slotIndices[1]].reference;
        skillbar[slotIndices[1]].reference = temp;
    }

    void OnDragAndDrop_CraftingIngredientSlot_CraftingIngredientSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            // just swap them clientsided
            int temp = craftingIndices[slotIndices[0]];
            craftingIndices[slotIndices[0]] = craftingIndices[slotIndices[1]];
            craftingIndices[slotIndices[1]] = temp;
            craftingState = CraftingState.None; // reset state
        }
    }

    void OnDragAndDrop_InventorySlot_NpcPetReviveSlot(int[] slotIndices)
    {
        // slotIndices[0] = slotFrom; slotIndices[1] = slotTo
        if (inventory[slotIndices[0]].item.data is PetItem)
            UINpcPetRevive.singleton.itemIndex = slotIndices[0];
    }

    void OnDragAndClear_SkillbarSlot(int slotIndex)
    {
        skillbar[slotIndex].reference = "";
    }

    void OnDragAndClear_TradingSlot(int slotIndex)
    {
        CmdTradeOfferItemClear(slotIndex);
    }

    void OnDragAndClear_NpcSellSlot(int slotIndex)
    {
        UINpcTrading.singleton.sellIndex = -1;
    }

    void OnDragAndClear_CraftingIngredientSlot(int slotIndex)
    {
        // only if not crafting right now
        if (craftingState != CraftingState.InProgress)
        {
            craftingIndices[slotIndex] = -1;
            craftingState = CraftingState.None; // reset state
        }
    }

    void OnDragAndClear_NpcPetReviveSlot(int slotIndex)
    {
        UINpcPetRevive.singleton.itemIndex = -1;
    }

    // validation //////////////////////////////////////////////////////////////
    void OnValidate()
    {
        // make sure that the NetworkNavMeshAgentRubberbanding2D component is
        // ABOVE the player component, so that it gets updated before Player.cs.
        // -> otherwise it overwrites player's WASD velocity for local player
        //    hosts
        // -> there might be away around it, but a warning is good for now
        Component[] components = GetComponents<Component>();
        if (Array.IndexOf(components, GetComponent<NetworkNavMeshAgentRubberbanding>()) >
            Array.IndexOf(components, this))
            Debug.LogWarning(name + "'s NetworkNavMeshAgentRubberbanding component is below the Player component. Please drag it above the Player component in the Inspector, otherwise there might be WASD movement issues due to the Update order.");
    }
}

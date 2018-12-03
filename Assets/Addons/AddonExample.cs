// Understanding the Addon System:
//
// 1. partial classes:
//    The C# compiler will include all 'partial' classes of the same type into
//    one big class. In other words, it's a way to split a big class into many
//    smaller files.
//
//    We use this for our addon system, so that a player addons can be in an
//    extra file, but the compiler does throw them all into one class in the
//    end. This also means that we never have to add addon scripts to all the
//    right prefabs. The C# compiler does that for us automatically.
//
//    Note: use [Header("MyAddon")] in front of your public variables so that
//          you can find them easier in the Inspector.
//
// 2. hooks:
//    The main classes have lots of places that we can hook into if necessary.
//    So instead of having to modify Player.Awake in the main class, we can use
//    the Awake_ hook below.
//
//    All functions starting with Awake_ will be called there. So if one addon
//    uses Awake_Loot and another addon uses Awake_Test, they are both called
//    from Player.Awake automatically.
//
//    If your addon is called 'Test', then your hook should be called Awake_Test
//
// General workflow:
//    Before this system, people simply modified uMMORPG's core files like
//    Player.cs to their needs. With this system, you should start exactly the
//    same way: open Player.cs and figure out where you want to add your modi-
//    fications. But instead of adding them to Player.cs, you add them to your
//    partial class below.
//
//    For example: if you add a 'public int test' variable to the partial Player
//    class below, it will be shown in all your Player prefabs automatically.
//
// Why:
//    There are two main benefits of using this addon system:
//        1. Updates to the core files won't overwrite your modifications.
//        2. Sharing addons is way easier. All it takes is one addon script.
//
//    The Partial + Hooks approach was the only solution for an addon system:
//        - the only way to extend the item/skill/quest structs is with partial,
//          so Unity components are out of the question
//
// Final Note:
//    No addon system allows 100% modifications. There might be cases where you
//    still have to modify the core scripts. If so, it's recommended to write
//    down the necessary modifications for your addons in the comment section.
//
//
//
////////////////////////////////////////////////////////////////////////////////
// Example Addon
//    Author: ...
//
//    Description: ...
//
//    Required Core modifications: ...
//
//    Usage: ...
//
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

// entities ////////////////////////////////////////////////////////////////////
public partial class Player
{
    //[SyncVar] int example;

    public partial class PlayerLevel
    {
    }

    void Awake_Example() {}
    void OnStartLocalPlayer_Example() {}
    void OnStartServer_Example() {}
    void Start_Example() {}
    void UpdateClient_Example() {}
    void LateUpdate_Example() {}
    void OnDestroy_Example() {}
    [Server] void DealDamageAt_Example(Entity entity, int amount) {}
    [Server] void OnDeath_Example() {}
    [Client] void OnSelect_Example(Entity entity) {}
    [Server] void OnLevelUp_Example() {}

    // you can use the drag and drop system too:
    void OnDragAndDrop_InventorySlot_ExampleSlot(int[] slotIndices) {}
    void OnDragAndClear_ExampleSlot(int slotIndex) {}
}

public partial class Monster
{
    void Awake_Example() {}
    void OnStartServer_Example() {}
    void Start_Example() {}
    void UpdateClient_Example() {}
    void LateUpdate_Example() {}
    [Server] void OnAggro_Example(Entity entity) {}
    [Server] void OnDeath_Example() {}
}

public partial class Npc
{
    void OnStartServer_Example() {}
    void UpdateClient_Example() {}
}

public partial class Pet
{
    public partial class PetLevel
    {
    }

    void Awake_Example() {}
    void OnStartServer_Example() {}
    void Start_Example() {}
    void UpdateClient_Example() {}
    void LateUpdate_Example() {}
    void OnDestroy_Example() {}
    [Server] void OnLevelUp_Example() {}
    [Server] void DealDamageAt_Example(Entity entity, int amount) {}
    [Server] void OnAggro_Example(Entity entity) {}
    [Server] void OnDeath_Example() {}
}

public partial class Entity
{
    void Awake_Example() {}
    void OnStartServer_Example() {}
    void Update_Example() {}
    [Server] void DealDamageAt_Example(Entity entity, int amount) {}
    [Client] void OnDamageReceived_Example(int amount, DamageType damageType) {}
    [Server] void OnDeath_Example() {}
}

// items ///////////////////////////////////////////////////////////////////////
public partial class ItemTemplate
{
    //[Header("My Addon")]
    //public int addonVariable = 0;
}

// note: can't add variables yet without modifying original constructor
public partial struct Item
{
    //public int addonVariable {
    //    get { return template.addonVariable; }
    //}

    void ToolTip_Example(StringBuilder tip)
    {
        //tip.Append("");
    }
}

// skills //////////////////////////////////////////////////////////////////////
public partial class SkillTemplate
{
    //[Header("My Addon")]
    //public int addonVariable = 0;

    public partial struct SkillLevel
    {
        // note: adding variables here will give lots of warnings, but it works.
        //public int addonVariable;
    }
}

// note: can't add variables yet without modifying original constructor
public partial struct Skill
{
    //public int addonVariable
    //{
    //    get { return template.addonVariable; }
    //}

    void ToolTip_Example(StringBuilder tip)
    {
        //tip.Append("");
    }
}

// quests //////////////////////////////////////////////////////////////////////
public partial class QuestTemplate
{
    //[Header("My Addon")]
    //public int addonVariable = 0;
}

// note: can't add variables yet without modifying original constructor
public partial struct Quest
{
    //public int addonVariable
    //{
    //    get { return template.addonVariable; }
    //}

    void ToolTip_Example(StringBuilder tip)
    {
        //tip.Append("");
    }
}

// database ////////////////////////////////////////////////////////////////////
public partial class Database
{
    static void Initialize_Example()
    {
        // it's usually best to create an extra table for your addon. example:
        //ExecuteNonQuery(@"CREATE TABLE IF NOT EXISTS example (
        //                  name TEXT NOT NULL PRIMARY KEY)");
    }
    static void CharacterLoad_Example(Player player) {}
    static void CharacterSave_Example(Player player) {}
}

// networkmanager //////////////////////////////////////////////////////////////
public partial class NetworkManagerMMO
{
    void Start_Example() {}
    void OnStartServer_Example() {}
    void OnStopServer_Example() {}
    void OnClientConnect_Example(NetworkConnection conn) {}
    void OnServerLogin_Example(LoginMsg message) {}
    void OnClientCharactersAvailable_Example(CharactersAvailableMsg message) {}
    void OnServerAddPlayer_Example(string account, GameObject player, NetworkConnection conn, CharacterSelectMsg message) {}
    void OnServerCharacterCreate_Example(CharacterCreateMsg message, Player player) {}
    void OnServerCharacterDelete_Example(CharacterDeleteMsg message) {}
    void OnServerDisconnect_Example(NetworkConnection conn) {}
    void OnClientDisconnect_Example(NetworkConnection conn) {}
}

// network messages ////////////////////////////////////////////////////////////
// all network messages can be extended
public partial class LoginMsg
{
}

// here is how to pass more data to the available message to show health, mana
// etc. in the character selection UI if necessary
public partial class CharactersAvailableMsg
{
    public partial struct CharacterPreview
    {
        //public int health;
    }
    void Load_Example(List<Player> players)
    {
        //for (int i = 0; i < players.Count; ++i)
        //    characters[i].health = players[i].health;
    }
}

// chat ////////////////////////////////////////////////////////////////////////
public partial class Chat
{
    void OnStartLocalPlayer_Example() {}
    void OnSubmit_Example(string text) {}
}

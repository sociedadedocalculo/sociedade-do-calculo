// Contains all the network messages that we need.
using System.Collections.Generic;
using System.Linq;
using Mirror;

// client to server ////////////////////////////////////////////////////////////
public partial class LoginMsg : MessageBase
{
    public static short MsgId = 1000;
    public string account;
    public string password;
    public string version;
}

public partial class CharacterSelectMsg : MessageBase
{
    public static short MsgId = 1001;
    public int index;
}

public partial class CharacterDeleteMsg : MessageBase
{
    public static short MsgId = 1002;
    public int index;
}

public partial class CharacterCreateMsg : MessageBase
{
    public static short MsgId = 1003;
    public string name;
    public int classIndex;
}

// server to client ////////////////////////////////////////////////////////////
// we need an error msg packet because we can't use TargetRpc with the Network-
// Manager, since it's not a MonoBehaviour.
public partial class ErrorMsg : MessageBase
{
    public static short MsgId = 2000;
    public string text;
    public bool causesDisconnect;
}

public partial class CharactersAvailableMsg : MessageBase
{
    public static short MsgId = 2001;
    public partial struct CharacterPreview
    {
        public string name;
        public string className; // = the prefab name
        public ItemSlot[] equipment;
    }
    public CharacterPreview[] characters;

    // load method in this class so we can still modify the characters structs
    // in the addon hooks
    public void Load(List<Player> players)
    {
        // we only need name, class, equipment for our UI
        characters = players.Select(
            player => new CharacterPreview{
                name = player.name,
                className = player.className,
                equipment = player.equipment.ToArray()
            }
        ).ToArray();

        // addon system hooks (to initialize extra values like health if necessary)
        Utils.InvokeMany(typeof(CharactersAvailableMsg), this, "Load_", players);
    }
}
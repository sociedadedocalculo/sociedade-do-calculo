// Parties have to be structs in order to work with SyncLists.
using System.Linq;

public struct Party
{
    // Guild.Empty for ease of use
    public static Party Empty = new Party();

    // properties
    public int partyId;
    public string[] members; // first one == master
    public bool shareExperience;
    public bool shareGold;

    // helper properties
    public string master => members != null && members.Length > 0 ? members[0] : "";

    // statics
    public static int Capacity = 8;
    public static float BonusExperiencePerMember = 0.1f;

    // if we create a party then always with two initial members
    public Party(int partyId, string master, string firstMember)
    {
        // create members array
        this.partyId = partyId;
        members = new string[]{master, firstMember};
        shareExperience = false;
        shareGold = false;
    }

    public bool Contains(string memberName)
    {
        return members != null && members.Contains(memberName);
    }

    public bool IsFull()
    {
        return members != null && members.Length == Capacity;
    }
}
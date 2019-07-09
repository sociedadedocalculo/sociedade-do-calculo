// Guilds have to be structs in order to work with SyncLists.
// Note: there are no health>0 checks for guild actions. Dead guild masters
//   should still be able to manage their guild
//   => also keeps code clean, otherwise we'd need Player.onlinePlayers and
//      that's not available on the client (for UI states) etc.
using System;

// guild ranks sorted by value. higher equals more power.
public enum GuildRank : byte
{
    Member = 0,
    Vice = 1,
    Master = 2
}

[Serializable]
public partial struct GuildMember
{
    // basic info
    public string name;
    public int level;
    public bool online;
    public GuildRank rank;

    public GuildMember(string name, int level, bool online, GuildRank rank)
    {
        this.name = name;
        this.level = level;
        this.online = online;
        this.rank = rank;
    }
}

public struct Guild
{
    // Guild.Empty for ease of use
    public static Guild Empty = new Guild();

    // properties
    public string name;
    public string notice;
    public GuildMember[] members;

    public string master => members != null ? Array.Find(members, (m) => m.rank == GuildRank.Master).name : "";

    // if we create a guild then always with a name and a first member
    public Guild(string name, string firstMember, int firstMemberLevel)
    {
        this.name = name;
        notice = "";
        GuildMember member = new GuildMember(firstMember, firstMemberLevel, true, GuildRank.Master);
        members = new GuildMember[]{member};
    }

    // can 'requester' leave the guild?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanLeave(string requesterName)
    {
        return members != null &&
               Array.FindIndex(members, (m) => m.name == requesterName && m.rank != GuildRank.Master) != -1;
    }

    // can 'requester' terminate the guild?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanTerminate(string requesterName)
    {
        // only 1 person left, which is requester, which is the master?
        return members != null &&
               members.Length == 1 &&
               Array.FindIndex(members, (m) => m.name == requesterName && m.rank == GuildRank.Master) != -1;
    }

    // can 'requester' change the notice?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanNotify(string requesterName)
    {
        return members != null &&
               Array.FindIndex(members, (m) => m.name == requesterName && m.rank >= GuildSystem.NotifyMinRank) != -1;
    }

    // can 'requester' kick 'target'?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanKick(string requesterName, string targetName)
    {
        if (members != null)
        {
            int requesterIndex = Array.FindIndex(members, (m) => m.name == requesterName);
            int targetIndex = Array.FindIndex(members, (m) => m.name == targetName);
            if (requesterIndex != -1 && targetIndex != -1)
            {
                GuildMember requester = members[requesterIndex];
                GuildMember target = members[targetIndex];
                return requester.rank >= GuildSystem.KickMinRank &&
                       requesterName != targetName &&
                       target.rank != GuildRank.Master &&
                       target.rank < requester.rank;
            }
        }
        return false;
    }

    // can 'requester' invite 'target'?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanInvite(string requesterName, string targetName)
    {
        return members != null &&
               members.Length < GuildSystem.Capacity &&
               requesterName != targetName &&
               Array.FindIndex(members, (m) => m.name == requesterName && m.rank >= GuildSystem.InviteMinRank) != -1;
    }

    // note: requester can't promote target to same rank, only to lower rank
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanPromote(string requesterName, string targetName)
    {
        if (members != null)
        {
            int requesterIndex = Array.FindIndex(members, (m) => m.name == requesterName);
            int targetIndex = Array.FindIndex(members, (m) => m.name == targetName);
            if (requesterIndex != -1 && targetIndex != -1)
            {
                GuildMember requester = members[requesterIndex];
                GuildMember target = members[targetIndex];
                return requester.rank >= GuildSystem.PromoteMinRank &&
                       requesterName != targetName &&
                       target.rank+1 < requester.rank;
            }
        }
        return false;
    }

    // can 'requester' demote 'target'?
    // => not in GuildSystem because it needs to be available on the client too
    public bool CanDemote(string requesterName, string targetName)
    {
        if (members != null)
        {
            int requesterIndex = Array.FindIndex(members, (m) => m.name == requesterName);
            int targetIndex = Array.FindIndex(members, (m) => m.name == targetName);
            if (requesterIndex != -1 && targetIndex != -1)
            {
                GuildMember requester = members[requesterIndex];
                GuildMember target = members[targetIndex];
                return requester.rank >= GuildSystem.PromoteMinRank &&
                       requesterName != targetName &&
                       target.rank > GuildRank.Member;
            }
        }
        return false;
    }
}

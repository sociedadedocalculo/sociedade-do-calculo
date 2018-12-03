// Guilds have to be structs in order to work with SyncLists.
// Note: there are no health>0 checks for guild actions. Dead guild masters
//   should still be able to manage their guild
//   => also keeps code clean, otherwise we'd need Player.onlinePlayers and
//      that's not available on the client (for UI states) etc.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

// guild ranks sorted by value. higher equals more power.
public enum GuildRank
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
}

public struct Guild
{
    public string notice;
    public GuildMember[] members;

    // statics
    public static int Capacity = 50;
    public static int NoticeMaxLength = 30;
    public static int NoticeWaitSeconds = 5;
    public static int CreationPrice = 100;
    public static int NameMaxLength = 16;

    public static GuildRank InviteMinRank = GuildRank.Vice;
    public static GuildRank KickMinRank = GuildRank.Vice;
    public static GuildRank PromoteMinRank = GuildRank.Master; // includes Demote
    public static GuildRank NotifyMinRank = GuildRank.Vice;

    // helper function to find a member
    public int GetMemberIndex(string memberName)
    {
        return members != null ? Array.FindIndex(members, m => m.name == memberName) : -1;
    }

    // helper function to find guild master name
    public string MasterName()
    {
        return members != null
               ? members.ToList().Find(m => m.rank == GuildRank.Master).name
               : "";
    }

    public static bool IsValidGuildName(string guildName)
    {
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        // and correct length?
        return guildName.Length <= NameMaxLength &&
               Regex.IsMatch(guildName, @"^[a-zA-Z0-9_]+$");
    }

    public void AddMember(string name, int level, GuildRank rank = GuildRank.Member)
    {
        GuildMember member = new GuildMember{name=name, level=level, online=true, rank=rank};
        if (members != null)
        {
            Array.Resize(ref members, members.Length + 1);
            members[members.Length - 1] = member;
        }
        else
        {
            members = new GuildMember[]{member};
        }
    }

    // can 'requester' invite 'target'?
    public bool CanInvite(string requesterName, string targetName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        if (requesterIndex != -1)
        {
            GuildMember requester = members[requesterIndex];

            return requester.rank >= InviteMinRank &&
                   requesterName != targetName &&
                   members.Length < Capacity;
        }
        return false;
    }

    // can 'requester' kick 'target'?
    public bool CanKick(string requesterName, string targetName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex = GetMemberIndex(targetName);
        if (requesterIndex != -1 && targetIndex != -1)
        {
            GuildMember requester = members[requesterIndex];
            GuildMember target = members[targetIndex];

            return requester.rank >= KickMinRank &&
                   requesterName != targetName &&
                   target.rank != GuildRank.Master &&
                   target.rank < requester.rank;
        }
        return false;
    }

    public void RemoveMember(string name)
    {
        List<GuildMember> list = members.ToList();
        list.RemoveAll(m => m.name == name);
        members = list.ToArray();
    }

    // note: requester can't promote target to same rank, only to lower rank
    public bool CanPromote(string requesterName, string targetName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex = GetMemberIndex(targetName);
        if (requesterIndex != -1 && targetIndex != -1)
        {
            GuildMember requester = members[requesterIndex];
            GuildMember target = members[targetIndex];

            return requester.rank >= PromoteMinRank &&
                   requesterName != targetName &&
                   target.rank+1 < requester.rank;
        }
        return false;
    }

    // note: there can only be one master
    public void PromoteMember(string name)
    {
        int index = GetMemberIndex(name);
        if (index != -1 && members[index].rank + 1 < GuildRank.Master)
            ++members[index].rank;
    }

    // can 'requester' demote 'target'?
    public bool CanDemote(string requesterName, string targetName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        int targetIndex = GetMemberIndex(targetName);
        if (requesterIndex != -1 && targetIndex != -1)
        {
            GuildMember requester = members[requesterIndex];
            GuildMember target = members[targetIndex];

            return requester.rank >= PromoteMinRank &&
                   requesterName != targetName &&
                   target.rank > GuildRank.Member;
        }
        return false;
    }

    public void DemoteMember(string name)
    {
        int index = GetMemberIndex(name);
        if (index != -1 && members[index].rank > GuildRank.Member)
            --members[index].rank;
    }

    // can 'requester' change the notice?
    public bool CanNotify(string requesterName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        return requesterIndex != -1 && members[requesterIndex].rank >= NotifyMinRank;
    }

    // can 'requester' terminate the guild?
    public bool CanTerminate(string requesterName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        if (requesterIndex != -1)
        {
            GuildMember requester = members[requesterIndex];
            return members.Length == 1 && requester.rank == GuildRank.Master;
        }
        return false;
    }

    // can 'requester' leave the guild?
    public bool CanLeave(string requesterName)
    {
        int requesterIndex = GetMemberIndex(requesterName);
        return requesterIndex != -1 && members[requesterIndex].rank != GuildRank.Master;
    }

    // helper function to set a member's online status
    public void SetOnline(string name, bool online)
    {
        int index = GetMemberIndex(name);
        if (index != -1)
            members[index].online = online;
    }
}

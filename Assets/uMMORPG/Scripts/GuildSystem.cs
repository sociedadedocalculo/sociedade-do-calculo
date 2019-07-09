// there are different ways to implement a guild system:
//
// - Player.cs can have a '[SyncVar] guild' and broadcast it to all guild members
//   when something changes in the guild. there is no one source of truth, which
//   makes this a bit weird.
//
// - Player.cs can have a Guild class reference that all members share. Mirror
//   can only serialize structs, which makes syncing more difficult then. There
//   is also the question of null vs. not null and we would have to not only
//   kick/leave guilds, but also never forget to set .guild to null. This
//   results in a lot of complicated code.
//
// - GuildSystem could have a list of Guild classes. But then the client would
//   need to have a local syncedGuild class, which makes .guild access on server
//   and client different (and hence very difficult).
//
// - GuildSystem could control the parties. When anything is changed, it
//   automatically sets each member's '[SyncVar] guild' which Mirror syncs
//   automatically. Server and client could access Player.guild to read anything
//   and use GuildSystem to modify parties.
//
//   => This seems to be the best solution for a guild system with Mirror.
//   => GuildSystem is almost independent from Unity. It's just a guild system
//      with names and partyIds.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

public static class GuildSystem
{
    // loaded guilds
    public static Dictionary<string, Guild> guilds = new Dictionary<string, Guild>();

    // configuration
    public static int Capacity = 50;
    public static int NoticeMaxLength = 30;
    public static int NoticeWaitSeconds = 5;
    public static int CreationPrice = 100;
    public static int NameMaxLength = 16;

    public static GuildRank InviteMinRank = GuildRank.Vice;
    public static GuildRank KickMinRank = GuildRank.Vice;
    public static GuildRank PromoteMinRank = GuildRank.Master; // includes Demote
    public static GuildRank NotifyMinRank = GuildRank.Vice;

    // copy guild to someone
    static void BroadcastTo(string member, Guild guild)
    {
        if (Player.onlinePlayers.TryGetValue(member, out Player player))
            player.guild = guild;
    }

    // copy guild to all members & save in dictionary
    static void BroadcastChanges(Guild guild)
    {
        foreach (GuildMember member in guild.members)
            BroadcastTo(member.name, guild);

        guilds[guild.name] = guild;
    }

    public static bool IsValidGuildName(string guildName)
    {
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        // and correct length?
        return guildName.Length <= NameMaxLength &&
               Regex.IsMatch(guildName, @"^[a-zA-Z0-9_]+$");
    }

    // create a guild. health, near npc, etc. needs to be checked in the caller.
    public static bool CreateGuild(string creator, int creatorLevel, string guildName)
    {
        // doesn't exist yet?
        if (IsValidGuildName(guildName) &&
            !Database.singleton.GuildExists(guildName)) // db check only on server, no Guild.CanCreate function because client has no DB.
        {
            // create guild and add creator to members list as highest rank
            Guild guild = new Guild(guildName, creator, creatorLevel);

            // broadcast and save
            BroadcastChanges(guild);
            Debug.Log(creator + " created guild: " + guildName);
            return true;
        }

        // exists or invalid regex
        return false;
    }

    public static void LeaveGuild(string guildName, string member)
    {
        // guild exists and member can leave?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanLeave(member))
        {
            // remove from list
            guild.members = guild.members.Where(m => m.name != member).ToArray();

            // clear for the person that left
            BroadcastTo(member, Guild.Empty);

            // broadcast and save
            BroadcastChanges(guild);
        }
    }

    public static void TerminateGuild(string guildName, string requester)
    {
        // guild exists and member can terminate?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanTerminate(requester))
        {
            // remove guild from database
            Database.singleton.RemoveGuild(guildName);

            // clear for person that terminated
            BroadcastTo(requester, Guild.Empty);
        }
    }

    public static bool SetGuildNotice(string guildName, string requester, string notice)
    {
        // guild exists, member can notify, notice not too long?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanNotify(requester) &&
            notice.Length <= NoticeMaxLength)
        {
            // set notice and reset next time
            guild.notice = notice;

            // broadcast and save
            BroadcastChanges(guild);
            Debug.Log(requester + " changed guild notice to: " + guild.notice);
            return true;
        }
        return false;
    }

    public static void KickFromGuild(string guildName, string requester, string member)
    {
        // guild exists, requester can kick member?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanKick(requester, member))
        {
            // reuse Leave function
            LeaveGuild(guildName, member);
            Debug.Log(requester + " kicked " + member + " from guild: " + guildName);
        }
    }

    public static bool AddToGuild(string guildName, string requester, string member, int memberLevel)
    {
        // guild exists, requester can invite member?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanInvite(requester, member))
        {
            // add to members
            Array.Resize(ref guild.members, guild.members.Length + 1);
            guild.members[guild.members.Length - 1] = new GuildMember(member, memberLevel, true, GuildRank.Member);

            // broadcast and save
            BroadcastChanges(guild);
            Debug.Log(requester + " added " + member + " to guild: " + guildName);
            return true;
        }
        return false;
    }

    public static void SetGuildOnline(string guildName, string member, bool online)
    {
        // guild exists?
        if (guilds.TryGetValue(guildName, out Guild guild))
        {
            // member in guild?
            int index = Array.FindIndex(guild.members, (m) => m.name == member);
            if (index != -1)
            {
                guild.members[index].online = online;

                // broadcast and save
                BroadcastChanges(guild);
            }
        }
    }

    public static void PromoteMember(string guildName, string requester, string member)
    {
        // guild exists, requester can promote member?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanPromote(requester, member))
        {
            // increase rank
            //
            // we need a completely new guild.members copy, because .members
            // is just a pointer to a memory location. so all players of that
            // guild actually use the same memory location for .members.
            // => so if we change newGuild.members[index].rank, then
            //    oldGuild.members[index].rank is the same immediately, because
            //    they point to the same memory.
            // => this is not really a problem, except that the SetSyncVar<Guild>
            //    call won't set the SyncVar dirty because oldGuild and newGuild
            //    are equal (because we modified both oldGuild.members.rank and
            //    newGuild.members.rank at the same time here already)
            int index = Array.FindIndex(guild.members, (m) => m.name == member);
            GuildMember[] newMembers = new GuildMember[guild.members.Length];
            Array.Copy(guild.members, newMembers, guild.members.Length);
            ++newMembers[index].rank;
            guild.members = newMembers;

            // broadcast and save
            BroadcastChanges(guild);
            Debug.Log(requester + " promoted " + member + " in guild: " + guildName);
        }
    }

    public static void DemoteMember(string guildName, string requester, string member)
    {
        // guild exists, requester can promote member?
        if (guilds.TryGetValue(guildName, out Guild guild) &&
            guild.CanDemote(requester, member))
        {
            // decrease rank
            //
            // we need a completely new guild.members copy, because .members
            // is just a pointer to a memory location. so all players of that
            // guild actually use the same memory location for .members.
            // => so if we change newGuild.members[index].rank, then
            //    oldGuild.members[index].rank is the same immediately, because
            //    they point to the same memory.
            // => this is not really a problem, except that the SetSyncVar<Guild>
            //    call won't set the SyncVar dirty because oldGuild and newGuild
            //    are equal (because we modified both oldGuild.members.rank and
            //    newGuild.members.rank at the same time here already)
            int index = Array.FindIndex(guild.members, (m) => m.name == member);
            GuildMember[] newMembers = new GuildMember[guild.members.Length];
            Array.Copy(guild.members, newMembers, guild.members.Length);
            --newMembers[index].rank;
            guild.members = newMembers;


            // broadcast and save
            BroadcastChanges(guild);
            Debug.Log(requester + " demoted " + member + " in guild: " + guildName);
        }
    }
}

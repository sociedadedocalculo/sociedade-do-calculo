// there are different ways to implement a party system:
//
// - Player.cs can have a '[SyncVar] party' and broadcast it to all party members
//   when something changes in the party. there is no one source of truth, which
//   makes this a bit weird. it works, but only until we need a global party
//   list, e.g. for dungeon instances.
//
// - Player.cs can have a Party class reference that all members share. Mirror
//   can only serialize structs, which makes syncing more difficult then. There
//   is also the question of null vs. not null and we would have to not only
//   kick/leave parties, but also never forget to set .party to null. This
//   results in a lot of complicated code.
//
// - PartySystem could have a list of Party classes. But then the client would
//   need to have a local syncedParty class, which makes .party access on server
//   and client different (and hence very difficult).
//
// - PartySystem could control the parties. When anything is changed, it
//   automatically sets each member's '[SyncVar] party' which Mirror syncs
//   automatically. Server and client could access Player.party to read anything
//   and use PartySystem to modify parties.
//
//   => This seems to be the best solution for a party system with Mirror.
//   => PartySystem is almost independent from Unity. It's just a party system
//      with names and partyIds.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PartySystem
{
    static Dictionary<int, Party> parties = new Dictionary<int, Party>();

    // start partyIds at 1. 0 means no party, because default party struct's
    // partyId is 0.
    static int nextPartyId = 1;

    // copy party to someone
    static void BroadcastTo(string member, Party party)
    {
        if (Player.onlinePlayers.TryGetValue(member, out Player player))
            player.party = party;
    }

    // copy party to all members & save in dictionary
    static void BroadcastChanges(Party party)
    {
        foreach (string member in party.members)
            BroadcastTo(member, party);

        parties[party.partyId] = party;
    }

    // check if a partyId exists
    public static bool PartyExists(int partyId)
    {
        return parties.ContainsKey(partyId);
    }

    // creating a party requires at least two members. it's not a party if
    // someone is alone in it.
    public static void FormParty(string creator, string firstMember)
    {
        // create party
        int partyId = nextPartyId++;
        Party party = new Party(partyId, creator, firstMember);

        // broadcast and save in dict
        BroadcastChanges(party);
        Debug.Log(creator + " formed a new party with " + firstMember);
    }

    public static void AddToParty(int partyId, string member)
    {
        // party exists and not full?
        Party party;
        if (parties.TryGetValue(partyId, out party) && !party.IsFull())
        {
            // add to members
            Array.Resize(ref party.members, party.members.Length + 1);
            party.members[party.members.Length - 1] = member;

            // broadcast and save in dict
            BroadcastChanges(party);
            Debug.Log(member + " was added to party " + partyId);
        }
    }

    public static void KickFromParty(int partyId, string requester, string member)
    {
        // party exists?
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            // requester is party master, member is in party, not same?
            if (party.master == requester && party.Contains(member) && requester != member)
            {
                // reuse the leave function
                LeaveParty(partyId, member);
            }
        }
    }

    public static void LeaveParty(int partyId, string member)
    {
        // party exists?
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            // requester is not master but is in party?
            if (party.master != member && party.Contains(member))
            {
                // remove from list
                party.members = party.members.Where(name => name != member).ToArray();

                // still > 1 people?
                if (party.members.Length > 1)
                {
                    // broadcast and save in dict
                    BroadcastChanges(party);
                    BroadcastTo(member, Party.Empty); // clear for kicked person
                }
                // otherwise remove party. no point in having only 1 member.
                else
                {
                    // broadcast and remove from dict
                    BroadcastTo(party.members[0], Party.Empty); // clear for master
                    BroadcastTo(member, Party.Empty); // clear for kicked person
                    parties.Remove(partyId);
                }

                Debug.Log(member + " left the party");
            }
        }
    }

    public static void DismissParty(int partyId, string requester)
    {
        // party exists?
        Party party;
        if (parties.TryGetValue(partyId, out party))
        {
            // is master?
            if (party.master == requester)
            {
                // clear party for everyone
                foreach (string member in party.members)
                    BroadcastTo(member, Party.Empty);

                // remove from dict
                parties.Remove(partyId);
                Debug.Log(requester + " dismissed the party");
            }
        }
    }

    public static void SetPartyExperienceShare(int partyId, string requester, bool value)
    {
        // party exists and master?
        Party party;
        if (parties.TryGetValue(partyId, out party) && party.master == requester)
        {
            // set new value
            party.shareExperience = value;

            // broadcast and save in dict
            BroadcastChanges(party);
        }
    }

    public static void SetPartyGoldShare(int partyId, string requester, bool value)
    {
        // party exists and master?
        Party party;
        if (parties.TryGetValue(partyId, out party) && party.master == requester)
        {
            // set new value
            party.shareGold = value;

            // broadcast and save in dict
            BroadcastChanges(party);
        }
    }
}

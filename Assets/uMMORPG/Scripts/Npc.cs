// The Npc class is rather simple. It contains state Update functions that do
// nothing at the moment, because Npcs are supposed to stand around all day.
//
// Npcs first show the welcome text and then have options for item trading and
// quests.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;
using TMPro;

// talk-to-npc quests work by adding the same quest to two npcs, one with
// accept=true and complete=false, the other with accept=false and complete=true
[Serializable]
public class ScriptableQuestOffer
{
    public ScriptableQuest quest;
    public bool acceptHere = true;
    public bool completeHere = true;
}

[RequireComponent(typeof(NetworkNavMeshAgent))]
public partial class Npc : Entity
{
    [Header("Text Meshes")]
    public TextMeshPro questOverlay;

    [Header("Welcome Text")]
    [TextArea(1, 30)] public string welcome;

    [Header("Items for Sale")]
    public ScriptableItem[] saleItems;

    [Header("Quests")]
    public ScriptableQuestOffer[] quests;

    [Header("Teleportation")]
    public Transform teleportTo;

    [Header("Guild Management")]
    public bool offersGuildManagement = true;

    [Header("Summonables")]
    public bool offersSummonableRevive = true;

    // networkbehaviour ////////////////////////////////////////////////////////
    public override void OnStartServer()
    {
        base.OnStartServer();

        // all npcs should spawn with full health and mana
        health = healthMax;
        mana = manaMax;

        // addon system hooks
        Utils.InvokeMany(GetType(), this, "OnStartServer_");
    }

    // finite state machine states /////////////////////////////////////////////
    [Server] protected override string UpdateServer() { return State; }
    [Client] protected override void UpdateClient()
    {
        // addon system hooks
        Utils.InvokeMany(GetType(), this, "UpdateClient_");
    }

    // overlays ////////////////////////////////////////////////////////////////
    protected override void UpdateOverlays()
    {
        base.UpdateOverlays();

        if (questOverlay != null)
        {
            // find local player (null while in character selection)
            if (Player.localPlayer != null)
            {
                if (quests.Any(entry => entry.completeHere && Player.localPlayer.CanCompleteQuest(entry.quest.name)))
                    questOverlay.text = "!";
                else if (quests.Any(entry => entry.acceptHere && Player.localPlayer.CanAcceptQuest(entry.quest)))
                    questOverlay.text = "?";
                else
                    questOverlay.text = "";
            }
        }
    }

    // skills //////////////////////////////////////////////////////////////////
    public override bool CanAttack(Entity entity) { return false; }

    // quests //////////////////////////////////////////////////////////////////
    // helper function to filter the quests that are shown for a player
    // -> all quests that:
    //    - can be started by the player
    //    - or were already started but aren't completed yet
    public List<ScriptableQuest> QuestsVisibleFor(Player player)
    {
        return quests.Where(entry => (entry.acceptHere && player.CanAcceptQuest(entry.quest)) ||
                                     (entry.completeHere && player.HasActiveQuest(entry.quest.name)))
                     .Select(entry => entry.quest)
                     .ToList();
    }
}

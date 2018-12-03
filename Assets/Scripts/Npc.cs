// The Npc class is rather simple. It contains state Update functions that do
// nothing at the moment, because Npcs are supposed to stand around all day.
//
// Npcs first show the welcome text and then have options for item trading and
// quests.
using UnityEngine;
using Mirror;
using System;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(NetworkNavMeshAgent))]
public partial class Npc : Entity
{
    [Header("Text Meshes")]
    public TextMesh questOverlay;

    [Header("Welcome Text")]
    [TextArea(1, 30)] public string welcome;

    [Header("Items for Sale")]
    public ScriptableItem[] saleItems;

    [Header("Quests")]
    public ScriptableQuest[] quests;

    [Header("Teleportation")]
    public Transform teleportTo;

    [Header("Guild Management")]
    public bool offersGuildManagement = true;

    [Header("Pets")]
    public bool offersPetRevive = true;

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
    [Server] protected override string UpdateServer() { return state; }
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
                if (quests.Any(q => Player.localPlayer.CanCompleteQuest(q.name)))
                    questOverlay.text = "!";
                else if (quests.Any(Player.localPlayer.CanAcceptQuest))
                    questOverlay.text = "?";
                else
                    questOverlay.text = "";
            }
        }
    }

    // skills //////////////////////////////////////////////////////////////////
    public override bool HasCastWeapon() { return true; }
    public override bool CanAttack(Entity entity) { return false; }

    // quests //////////////////////////////////////////////////////////////////
    // helper function to filter the quests that are shown for a player
    // -> all quests that:
    //    - can be started by the player
    //    - or were already started but aren't completed yet
    public List<ScriptableQuest> QuestsVisibleFor(Player player)
    {
        return quests.Where(q => player.CanAcceptQuest(q) ||
                                 player.HasActiveQuest(q.name)).ToList();
    }
}

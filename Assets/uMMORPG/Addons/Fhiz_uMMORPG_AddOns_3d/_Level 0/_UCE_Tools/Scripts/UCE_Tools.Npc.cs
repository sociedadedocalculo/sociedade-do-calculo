// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using Mirror;
using UnityEngine;

// ===================================================================================
// NPC
// ===================================================================================
public partial class Npc
{
    // Required to keep track of how many players are within this Npcs interaction range
    [SyncVar, HideInInspector] public int accessingPlayers = 0;
}
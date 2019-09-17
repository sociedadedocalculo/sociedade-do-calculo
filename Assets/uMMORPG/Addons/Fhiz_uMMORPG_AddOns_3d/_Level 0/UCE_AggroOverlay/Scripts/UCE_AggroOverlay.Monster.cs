// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using UnityEngine;
using Mirror;
using System;
using System.Linq;
using System.Collections;

// =======================================================================================
// MONSTER
// =======================================================================================
public partial class Monster {
	
	public UCE_AggroOverlay aggroOverlay;
	
	// -----------------------------------------------------------------------------------
	// OnClientAggro_UCE_AggroOverlay
	// -----------------------------------------------------------------------------------
	[ClientCallback]
    [DevExtMethods("OnClientAggro")]
    private void OnClientAggro_UCE_AggroOverlay(Entity entity)
    {
    	
    	if (aggroOverlay == null) return;
    	
    	if (
    		target != entity ||
    		!(entity is Player)
    		)
    		aggroOverlay.Hide();
    	else if (target == null ||
    			entity is Player)
    		aggroOverlay.Show();
    		        
    }
	
	// -----------------------------------------------------------------------------------
	
}

// =======================================================================================

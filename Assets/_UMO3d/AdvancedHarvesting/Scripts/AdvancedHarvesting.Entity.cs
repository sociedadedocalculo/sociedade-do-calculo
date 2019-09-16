// =======================================================================================
// NU CORE - ENTITY
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;

// =======================================================================================
// ENTITY
// =======================================================================================
public partial class Entity {

	// =======================================================================================
	// SHOW POPUP
	// =======================================================================================
    [ClientRpc(channel=Channels.DefaultUnreliable)] // unimportant => unreliable
    public void Rpc_UMO3d_AdvancedHarvesting_ShowPopup(GameObject damageReceiver, string text) {
        
        if (damageReceiver != null && text != "") { // still around?
            Entity receiverEntity = damageReceiver.GetComponent<Entity>();
            if (receiverEntity != null && receiverEntity.damagePopupPrefab != null) {
                // showing it above their head looks best, and we don't have to use
                // a custom shader to draw world space UI in front of the entity
                var bounds = receiverEntity.collider.bounds;
                Vector3 position = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);

                var popup = Instantiate(receiverEntity.damagePopupPrefab, position, Quaternion.identity);
                popup.GetComponentInChildren<TextMesh>().text = text;
                
            }
        }
    }
    
	// =======================================================================================
	
}

// =======================================================================================
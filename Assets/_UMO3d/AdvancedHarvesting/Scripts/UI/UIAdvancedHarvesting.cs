// =======================================================================================
// ADVANCED HARVESTING - UI
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

#if _AdvancedHarvesting

namespace UMO3d {

	// ===================================================================================
	// ADVANCED HarvestingS UI
	// ===================================================================================
	public partial class UIAdvancedHarvesting : MonoBehaviour {
	    
	    public GameObject panel;
	    public Transform content;
	    public UIAdvancedHarvestingSlot slotPrefab;
		public KeyCode hotKey = KeyCode.H;

		// -----------------------------------------------------------------------------------
		// Update
		// -----------------------------------------------------------------------------------
		private void Update() {
			var player = Utils.ClientLocalPlayer();
			if (!player) return;
			
			// hotkey (not while typing in chat, etc.)
        	if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
            panel.SetActive(!panel.activeSelf);

			if (panel.activeSelf && player.UMO3d_Professions.Count > 0) {
		
				UIUtils.BalancePrefabs(slotPrefab.gameObject, player.UMO3d_Professions.Count, content);

        		for (int i = 0; i < content.childCount; i++) {
            		content.GetChild(i).GetComponent<UIAdvancedHarvestingSlot>().Show(player.UMO3d_Professions[i]);
        		}
        	
        	}
			
		}
		
		// -----------------------------------------------------------------------------------
		
	}
	
	// -----------------------------------------------------------------------------------
	
}

#endif

// =======================================================================================
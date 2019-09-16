// =======================================================================================
// ADVANCED HARVESTING - UI
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;
using UnityEngine.UI;

#if _AdvancedHarvesting

namespace UMO3d {

	// ===================================================================================
	// HARVEST BAR UI
	// ===================================================================================
	public partial class UIAdvancedHarvestingBar : MonoBehaviour {
		public GameObject panel;
		public Slider slider;
		public Text ProfessionNameText;
		public Text progressText;
		
		[HideInInspector] private float duration;
		[HideInInspector] private float durationRemaining;
		
		// -----------------------------------------------------------------------------------
		// Update
		// -----------------------------------------------------------------------------------
		void Update() {
			var player = Utils.ClientLocalPlayer();
			if (!player) return;

			if (panel.activeSelf) {
			
				if (NetworkTime.time <= durationRemaining) {

					float ratio = (durationRemaining - NetworkTime.time) / duration;
					float remain = durationRemaining - NetworkTime.time;
					slider.value = ratio;
					progressText.text = remain.ToString("F1") + "s";
				
				} else {
					Hide();
				}
				
			} else {
				Hide();
			}
			
		}
		
		// -----------------------------------------------------------------------------------
		// Show
		// -----------------------------------------------------------------------------------
		public void Show(string profName, float harvestDur) {
			var player = Utils.ClientLocalPlayer();
			if (!player) return;
			
			duration = harvestDur;
			durationRemaining = NetworkTime.time + harvestDur;
			
			ProfessionNameText.text = profName;
			
			panel.SetActive(true);
		}
	
		// -----------------------------------------------------------------------------------
		// Hide
		// -----------------------------------------------------------------------------------
		public void Hide() {
			panel.SetActive(false);
		}
	
	
	}

}

// =======================================================================================

#endif
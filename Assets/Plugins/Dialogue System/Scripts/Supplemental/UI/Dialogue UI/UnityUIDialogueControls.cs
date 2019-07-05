#if !(UNITY_4_3 || UNITY_4_5)
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem {
	
	/// <summary>
	/// Contains all dialogue (conversation) controls for a Unity UI Dialogue UI.
	/// </summary>
	[System.Serializable]
	public class UnityUIDialogueControls : AbstractDialogueUIControls {
		
		/// <summary>
		/// The panel containing the dialogue controls. A panel is optional, but you may want one
		/// so you can include a background image, panel-wide effects, etc.
		/// </summary>
		[Tooltip("Panel containing the entire conversation UI")]
		public UnityEngine.UI.Graphic panel;
		
		/// <summary>
		/// The NPC subtitle controls.
		/// </summary>
		public UnityUISubtitleControls npcSubtitle;
		
		/// <summary>
		/// The PC subtitle controls.
		/// </summary>
		public UnityUISubtitleControls pcSubtitle;
		
		/// <summary>
		/// The response menu controls.
		/// </summary>
		public UnityUIResponseMenuControls responseMenu;
		
		[Serializable]
		public class AnimationTransitions {
			public string showTrigger = "Show";
			public string hideTrigger = "Hide";
		}

		[Tooltip("Optional animation transitions; panel should have an Animator")]
		public AnimationTransitions animationTransitions = new AnimationTransitions();
		
		private bool isVisible = false;
		
		private UIShowHideController showHideController = null;
		
		public override AbstractUISubtitleControls NPCSubtitle { 
			get { return npcSubtitle; }
		}
		
		public override AbstractUISubtitleControls PCSubtitle {
			get { return pcSubtitle; }
		}
		
		public override AbstractUIResponseMenuControls ResponseMenu {
			get { return responseMenu; }
		}
		
		public override void SetActive(bool value) {
			try {
				if (value == true) {
					base.SetActive(true);
					ShowPanel();
				} else {
					HidePanel();
				}
			} finally {
				isVisible = value;
			}
		}

		public override void ShowPanel() {
			ShowControls();
			if (!isVisible) {
				isVisible = true;
				CheckShowHideController();
				showHideController.ClearTrigger(animationTransitions.hideTrigger);
				showHideController.Show(animationTransitions.showTrigger, false, null);
			}
		}
		
		private void HidePanel() {
			if (isVisible) {
				CheckShowHideController();
				showHideController.ClearTrigger(animationTransitions.showTrigger);
				showHideController.Hide(animationTransitions.hideTrigger, HideControls);
			} else {
				HideControls();
			}
		}
		
		private void CheckShowHideController() {
			if (showHideController == null) {
				showHideController = new UIShowHideController(null, panel);
			}
		}

		private void ShowControls() {
			if (panel != null) Tools.SetGameObjectActive(panel, true);
		}

		private void HideControls() {
			if (panel != null) Tools.SetGameObjectActive(panel, false);
			#if !UNITY_WEBPLAYER
			base.SetActive(false); // Can't call base virtual methods in coroutines in webplayer.
			#endif
			isVisible = false;
		}

	}
		
}
#endif
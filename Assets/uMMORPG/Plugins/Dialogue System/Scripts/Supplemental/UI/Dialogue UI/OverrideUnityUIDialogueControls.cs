using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PixelCrushers.DialogueSystem {

	/// <summary>
	/// This component allows actors to override Unity UI dialogue controls. It's 
	/// particularly useful to assign world space UIs such as speech bubbles above
	/// actors' heads.
	/// </summary>
	[AddComponentMenu("Dialogue System/UI/Unity UI/Dialogue/Override/Override Unity UI Dialogue Controls")]
	public class OverrideUnityUIDialogueControls : MonoBehaviour {

		[Tooltip("Use these controls when playing subtitles through this actor")]
		public UnityUISubtitleControls subtitle;

		[Tooltip("Use these controls when showing subtitle reminders for actor")]
		public UnityUISubtitleControls subtitleReminder;

		[Tooltip("Use these controls when showing a response menu involving this actor")]
		public UnityUIResponseMenuControls responseMenu;

		public virtual void Start() {
			if (subtitle != null) subtitle.SetActive(false);
			if (subtitleReminder != null) subtitleReminder.SetActive(false);
			if (responseMenu != null) responseMenu.SetActive(false);
		}

	}

}

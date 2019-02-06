using UnityEngine;

namespace PixelCrushers.DialogueSystem {

    /// <summary>
    /// This script replaces the normal continue button functionality with
    /// a two-stage process. If the typewriter effect is still playing, it
    /// simply stops the effect. Otherwise it sends OnContinue to the UI.
    /// </summary>
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3_OR_NEWER
    [HelpURL("http://pixelcrushers.com/dialogue_system/manual/html/unity_u_i_dialogue_u_i.html#unityUIDialogueUIContinueButtonFastForward")]
#endif
    [AddComponentMenu("Dialogue System/UI/Unity UI/Effects/Unity UI Continue Button Fast Forward")]
	public class UnityUIContinueButtonFastForward : MonoBehaviour {

		public UnityUIDialogueUI dialogueUI;
		
		public UnityUITypewriterEffect typewriterEffect;

		public virtual void Awake() {
			if (dialogueUI == null) {
				dialogueUI = Tools.GetComponentAnywhere<UnityUIDialogueUI>(gameObject);
			}
			if (typewriterEffect == null) {
				typewriterEffect = GetComponentInChildren<UnityUITypewriterEffect>();
			}
		}

		public virtual void OnFastForward() {
			if ((typewriterEffect != null) && typewriterEffect.IsPlaying) {
				typewriterEffect.Stop();
			} else {
				if (dialogueUI != null) dialogueUI.OnContinue();
			}
		}
		
	}

}

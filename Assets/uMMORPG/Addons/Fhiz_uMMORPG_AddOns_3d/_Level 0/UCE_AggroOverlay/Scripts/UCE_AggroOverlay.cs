// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using UnityEngine;
using System.Collections;

// =======================================================================================
// UCE AggroOverlay
// =======================================================================================
public class UCE_AggroOverlay : MonoBehaviour {

	public GameObject childObject;
	public float hideAfter = 0.5f;
	
	// -----------------------------------------------------------------------------------
	// Awake
	// -----------------------------------------------------------------------------------
	void Awake() {
		childObject.SetActive(false);
	}
	
	// -----------------------------------------------------------------------------------
	// Show
	// -----------------------------------------------------------------------------------
	public void Show()
	{
		childObject.SetActive(true);
		Invoke("Hide", hideAfter);
	}
	
	// -----------------------------------------------------------------------------------
	// Hide
	// -----------------------------------------------------------------------------------
	public void Hide()
	{
		CancelInvoke();
		childObject.SetActive(false);
	}
	
	// -----------------------------------------------------------------------------------
	
}
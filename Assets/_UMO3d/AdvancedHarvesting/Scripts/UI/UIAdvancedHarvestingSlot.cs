// =======================================================================================
// ADVANCED HARVESTING - SLOT
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using UMO3d;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

#if _AdvancedHarvesting

namespace UMO3d {

	// ===================================================================================
	// ADVANCED HarvestingS SLOT
	// ===================================================================================
	public class UIAdvancedHarvestingSlot : MonoBehaviour {
		
		public Text nameText;
		public Image professionIcon;
		public Slider expSlider;
    	public UIShowToolTip tooltip;
    	
		// -----------------------------------------------------------------------------------
		// Show
		// -----------------------------------------------------------------------------------
		public void Show(AdvancedHarvestingProfession p) {
			
			float value = 0;
			
			string lvl = " [L"+p.level.ToString()+"/"+p.maxlevel.ToString()+"]";
			
			if (p.level < p.maxlevel) {
				value = (p.experience != 0 && p.template.levels[p.level-1] != 0) ? (float)p.experience / (float)p.template.levels[p.level-1] : 0;
			} else {
				value = 1;
			}
			
			nameText.text = p.template.name+lvl;
			professionIcon.sprite = p.template.image;
			expSlider.value = value;
			
			tooltip.enabled = true;
            tooltip.text = ToolTip(p.template);
			
		}
    	
   		// -----------------------------------------------------------------------------------
		// ToolTip
		// -----------------------------------------------------------------------------------
 	
    	public string ToolTip(AdvancedHarvestingProfessionTemplate tpl) {

			var tip = new StringBuilder();
			
			tip.Append(tpl.name+"\n");
			tip.Append(tpl.toolTip+"\n");
			tip.Append("\n");
			tip.Append("Basic Harvest Chance: "	+(tpl.baseHarvestChance*100).ToString()+"%\n");
			
			if (tpl.requiredEquipTool != null) {
			tip.Append("Required Equipment: "	+tpl.requiredEquipTool.name+"\n");
			}
			
			if (tpl.requiredInventoryTool != null) {
			tip.Append("Required Item: "	+tpl.requiredInventoryTool.name+"\n");
			}

			if (tpl.depletableInventoryItem != null && tpl.depleteAmount > 0) {
			tip.Append("Depletable Item: "	+tpl.depletableInventoryItem.name+" x"+tpl.depleteAmount+"\n");
			}

			return tip.ToString();
		}
    	
    	// -----------------------------------------------------------------------------------
    
	}

}

#endif

// =======================================================================================
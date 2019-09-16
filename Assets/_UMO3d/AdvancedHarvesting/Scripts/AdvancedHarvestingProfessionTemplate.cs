// =======================================================================================
// ADVANCED Harvesting - SCRIPTABLE OBJECT
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if _AdvancedHarvesting

// =======================================================================================
// Harvesting
// =======================================================================================
[CreateAssetMenu(fileName = "New AdvancedHarvesting Profession", menuName = "New AdvancedHarvesting Profession", order=999)]
public class AdvancedHarvestingProfessionTemplate : ScriptableObject {
	
	[Header("UMO3d - Advanced Harvesting Profession")]
    public int[] levels;
	public Sprite image;
	public string animatorState;
	
	[Range(0,1)] public float baseHarvestChance = 1.0f;
	
	public ItemTemplate requiredEquipTool;
	public string equipmentCategory;
	public ItemTemplate requiredInventoryTool;
	public ItemTemplate depletableInventoryItem;
	public int depleteAmount;
	
	[TextArea(1, 30)] public string toolTip;
	
	// caching
    static Dictionary<string, AdvancedHarvestingProfessionTemplate> cache = null;
    public static Dictionary<string, AdvancedHarvestingProfessionTemplate> dict
    {
        get
        {
            // load if not loaded yet
            return cache ?? (cache = Resources.LoadAll<AdvancedHarvestingProfessionTemplate>("").ToDictionary(profession => profession.name, profession => profession)
            );
        }
    }
}

#endif

// =======================================================================================

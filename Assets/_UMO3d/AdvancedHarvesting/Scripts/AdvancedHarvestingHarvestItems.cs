// =======================================================================================
// ADVANCED Harvesting - HARVEST CHANCE
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using UnityEngine;

#if _AdvancedHarvesting

namespace UMO3d {

	// =======================================================================================
	// HARVEST CHANCE
	// =======================================================================================
	[System.Serializable]
	public class AdvancedHarvestingHarvestItems {
		public ItemTemplate template;
		[Range(0,1)] public float probability;
		[Range(1,999)] public int minAmount = 1;
		[Range(1,999)] public int maxAmount = 1;
	}

}

#endif

// =======================================================================================

// =======================================================================================
// ADVANCED HARVESTING - ITEM
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using UMO3d;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if _AdvancedHarvesting

// =======================================================================================
// ITEM
// =======================================================================================
public partial struct Item {
    
    public AdvancedHarvestingProfessionTemplate learnProfession 	{ get { return template.learnProfession; } }
	public int gainProfessionExp 									{ get { return template.gainProfessionExp; } }
	
}

#endif

// =======================================================================================
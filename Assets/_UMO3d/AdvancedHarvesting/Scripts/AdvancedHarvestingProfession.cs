// =======================================================================================
// ADVANCED HARVESTING - PROFESSION
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

using System.Linq;
using UnityEngine.Networking;

#if _AdvancedHarvesting

// =======================================================================================
// Harvesting
// =======================================================================================
public struct AdvancedHarvestingProfession {
    
    public string templateName;
    public int experience;

	// -----------------------------------------------------------------------------------
	// AdvancedHarvestingProfession (Constructor)
	// -----------------------------------------------------------------------------------
    public AdvancedHarvestingProfession(string templateName) {
        this.templateName = templateName;
        experience = 0;
    }
    
	// -----------------------------------------------------------------------------------
	// level (Getter)
	// -----------------------------------------------------------------------------------
    public int level {
        get {
        	var exp = this.experience;
            return 1 + template.levels.Count(l => l <= exp);
        }
    }
    
 	// -----------------------------------------------------------------------------------
	// maxlevel (Getter)
	// -----------------------------------------------------------------------------------
    public int maxlevel {
    	get { return template.levels.Count() + 1; }
    }
    
	// -----------------------------------------------------------------------------------
	// AdvancedHarvestingProfessionTemplate (Getter)
	// -----------------------------------------------------------------------------------
    public AdvancedHarvestingProfessionTemplate template {
        get { return AdvancedHarvestingProfessionTemplate.dict[templateName]; }
    }

   // -----------------------------------------------------------------------------------
   
}

public class SyncListAdvancedHarvestingProfession : SyncListStruct<AdvancedHarvestingProfession> { }

#endif

// =======================================================================================

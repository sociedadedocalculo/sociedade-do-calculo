// =======================================================================================
// DEFINES
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

#if UNITY_EDITOR

using UMO3d;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class Define_UMO3d_AdvancedHarvesting
{
	static string definestring;

    const string define = "_AdvancedHarvesting";
	const string versionuMMORPG3d = "v1.100";
	const bool requiredNuCore = false;
    
	const string nuCore = "_NuCore";
	
	static Define_UMO3d_AdvancedHarvesting() {AddLibrayDefineIfNeeded();}

    static void AddLibrayDefineIfNeeded() {
		BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
		definestring = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

		#pragma warning disable
		if (requiredNuCore && !inDefines(nuCore))
			Debug.LogWarning("WARNING! This AddOn requires the most recent <b>NuCore</b> - free download: http://www.indie-mmo.com/butler - If installed already, please restart Unity.");
		
		if (inDefines (define)) {
			Debug.LogWarning("Initalized " + define + " " + Version.UMO3d_AdvancedHarvesting_Str + " for uMMORPG3d " +  versionuMMORPG3d + " (backwards compatibility not guaranteed).");
			return;
		}
		
		PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (definestring + ";" + define));
		Debug.LogWarning("<b>" + define + "</b> added to <i>Scripting Define Symbols</i> for selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ").");
    	#pragma warning restore
    	
    }

	static bool inDefines(string define) {
		string[] defines = definestring.Split (';');
		foreach (string def in defines) {
			if (def == define)
				return true;
		}
		return false;
	}
}

#endif

// =======================================================================================
// VERSION
// by Indie-MMO (http://indie-mmo.com)
// Copyright 2017+
// =======================================================================================

namespace UMO3d {
	public partial class Version {
		public const int UMO3d_AdvancedHarvesting_Int = 100;
		public const string UMO3d_AdvancedHarvesting_Str = "v1.00";
	}
}

// =======================================================================================
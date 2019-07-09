#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MountLib
{
	const string define = "UMMORPG_MOUNTS";

	static MountLib()
	{ AddLibrayDefineIfNeeded(); }

	static void AddLibrayDefineIfNeeded()
	{
		// Get defines.
		BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
		string defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);

		// Append only if not defined already.
		if (defines.Contains(define))
		{
			//Debug.LogWarning("Selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ") already contains <b>" + define + "</b> <i>Scripting Define Symbol</i>.");
			return;
		}

		// Append.
		PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (defines + ";" + define));
		Debug.LogWarning("<b>" + define + "</b> added to <i>Scripting Define Symbols</i> for selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ").");
	}
}

#endif
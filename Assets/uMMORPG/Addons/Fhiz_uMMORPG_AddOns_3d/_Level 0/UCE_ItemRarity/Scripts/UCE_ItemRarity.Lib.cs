// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class ItemRarityLib
{
    private const string define = "_FHIZITEMRARITY";

    static ItemRarityLib()
    {
        AddLibrayDefineIfNeeded();
    }

    private static void AddLibrayDefineIfNeeded()
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string definestring = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        string[] defines = definestring.Split(';');

#if !_FHIZTOOLS
		Debug.LogWarning("<b>uMMMORPG3d</b> only! I cannot give support for uMMORPG2d or uSurvival - Sorry!");

#endif

        if (Contains(defines, define))
            return;

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (definestring + ";" + define));
        Debug.LogWarning("<b>AddOn imported!</b> - to complete installation please refer to the included README and follow instructions.");
        Debug.Log("<b>" + define + "</b> added to <i>Scripting Define Symbols</i> for selected build target (" + EditorUserBuildSettings.activeBuildTarget.ToString() + ").");
    }

    private static bool Contains(string[] defines, string define)
    {
        foreach (string def in defines)
        {
            if (def == define)
                return true;
        }
        return false;
    }
}

#endif
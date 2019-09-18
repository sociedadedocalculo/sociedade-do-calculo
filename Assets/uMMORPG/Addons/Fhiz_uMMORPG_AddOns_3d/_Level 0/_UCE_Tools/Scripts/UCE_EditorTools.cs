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

// =======================================================================================
// UCE EDITOR TOOLS
// =======================================================================================
[InitializeOnLoad]
public static partial class UCE_EditorTools
{
    // -------------------------------------------------------------------------------
    // AddScriptingDefine
    // -------------------------------------------------------------------------------
    public static void AddScriptingDefine(string define)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string definestring = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        string[] defines = definestring.Split(';');

        if (UCE_Tools.ArrayContains(defines, define))
            return;

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (definestring + ";" + define));
    }

    // -------------------------------------------------------------------------------
    // RemoveScriptingDefine
    // -------------------------------------------------------------------------------
    public static void RemoveScriptingDefine(string define)
    {
        BuildTargetGroup buildTargetGroup = EditorUserBuildSettings.selectedBuildTargetGroup;
        string definestring = PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
        string[] defines = definestring.Split(';');

        defines = UCE_Tools.RemoveFromArray(defines, define);

        definestring = string.Join(";", defines);

        PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, (definestring));
    }

    // -----------------------------------------------------------------------------------
}

#endif

// =======================================================================================
// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using UnityEngine;

// =======================================================================================
// TemplateConfiguration
// =======================================================================================
[CreateAssetMenu(menuName = "UCE Other/UCE Configuration", fileName = "New UCE Configuration", order = 999)]
public partial class UCE_TemplateConfiguration : ScriptableObject
{
    /*
		reserved for future functionality
		the settings here have no effect yet, but will have later when starting client/server
		seperation. in addition, global settings can be added here easily (its partial too).
	*/

    public bool isServer = true;
    public bool isClient = true;

    protected const string IS_SERVER = "_SERVER";
    protected const string IS_CLIENT = "_CLIENT";

    // -----------------------------------------------------------------------------------
    // OnValidate
    // -----------------------------------------------------------------------------------
    public void OnValidate()
    {
#if UNITY_EDITOR
        if (isServer && !isClient)
        {
            UCE_EditorTools.RemoveScriptingDefine(IS_CLIENT);
            UCE_EditorTools.AddScriptingDefine(IS_SERVER);
        }
        else if (isClient && !isServer)
        {
            UCE_EditorTools.RemoveScriptingDefine(IS_SERVER);
            UCE_EditorTools.AddScriptingDefine(IS_CLIENT);
        }
        else
        {
            UCE_EditorTools.AddScriptingDefine(IS_CLIENT);
            UCE_EditorTools.AddScriptingDefine(IS_SERVER);
        }
#endif
    }

    // -----------------------------------------------------------------------------------
}
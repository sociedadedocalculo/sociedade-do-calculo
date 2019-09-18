// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// =======================================================================================
// ELEMENT TEMPLATE
// =======================================================================================
[CreateAssetMenu(fileName = "New UCE Element", menuName = "UCE Templates/New UCE Element", order = 999)]
public partial class UCE_ElementTemplate : ScriptableObject
{
    [Header("[-=-=-=- UCE Element -=-=-=-]")]
    [TextArea(1, 30)] public string toolTip;

    public Sprite image;

    // -----------------------------------------------------------------------------------
    // Cache
    // -----------------------------------------------------------------------------------
    private static Dictionary<string, UCE_ElementTemplate> cache = null;

    public static Dictionary<string, UCE_ElementTemplate> dict
    {
        get
        {
            if (cache == null)
                cache = Resources.LoadAll<UCE_ElementTemplate>("").ToDictionary(
                    x => x.name, x => x
                );
            return cache;
        }
    }

    // -----------------------------------------------------------------------------------
}

// =======================================================================================
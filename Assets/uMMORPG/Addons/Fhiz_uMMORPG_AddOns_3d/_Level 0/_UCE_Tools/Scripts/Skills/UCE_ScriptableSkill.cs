// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using UnityEngine;

public abstract partial class ScriptableSkill : ScriptableObject
{
    [Tooltip("This skill cannot be learned via the Skill Window, only via other means")]
    public bool unlearnable;

    [Tooltip("Checked = negative skill, Unchecked = positive skill. Certain skills can debuff disadvantageous skills only")]
    public bool disadvantageous;
}
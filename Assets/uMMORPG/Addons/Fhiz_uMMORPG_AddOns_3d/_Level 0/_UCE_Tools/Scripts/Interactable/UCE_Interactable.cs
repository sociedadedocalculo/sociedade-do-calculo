// =======================================================================================
// Created and maintained by Fhiz
// Usable for both personal and commercial projects, but no sharing or re-sale
// * Discord Support Server.............: https://discord.gg/YkMbDHs
// * Public downloads website...........: https://www.indie-mmo.net
// * Pledge on Patreon for VIP AddOns...: https://www.patreon.com/Fhizban
// =======================================================================================

using Mirror;
using UnityEngine;

// ===================================================================================
// UCE INTERACTABLE CLASS
// ===================================================================================
[RequireComponent(typeof(NetworkIdentity))]
public abstract partial class UCE_Interactable : NetworkBehaviour
{
    [Header("[-=-=-=- UCE INTERACTABLE -=-=-=-]")]
    public string interactionText = "Interact with this Object";
    public Sprite interactionIcon;
    public bool automaticActivation;

    public UCE_InteractionRequirements interactionRequirements;

    protected UCE_UI_InteractableAccessRequirement instance;

    [SyncVar, HideInInspector] public bool unlocked = false;
		
    // -----------------------------------------------------------------------------------
    // Start
    // -----------------------------------------------------------------------------------
    public virtual void Start()
    {
        if (interactionIcon != null)
            this.GetComponentInChildren<SpriteRenderer>().sprite = interactionIcon;
    }

    // -----------------------------------------------------------------------------------
    // OnInteractClient
    // -----------------------------------------------------------------------------------
    //[ClientCallback]
    public virtual void OnInteractClient(Player player) { }

    // -----------------------------------------------------------------------------------
    // OnInteractServer
    // -----------------------------------------------------------------------------------
    //[ServerCallback]
    public virtual void OnInteractServer(Player player) { }

    // -----------------------------------------------------------------------------------
    // IsUnlocked
    // -----------------------------------------------------------------------------------
    public virtual bool IsUnlocked() { return false; }

    // -----------------------------------------------------------------------------------
    // ConfirmAccess
    // @Client
    // -----------------------------------------------------------------------------------
    public virtual void ConfirmAccess()
    {
        Player player = Player.localPlayer;
        if (!player) return;

        if (interactionRequirements.checkRequirements(player) || IsUnlocked())
        {
            OnInteractClient(player);
            player.Cmd_UCE_OnInteractServer(this.gameObject);
        }
    }

    // -----------------------------------------------------------------------------------
    // ShowAccessRequirementsUI
    // @Client
    // -----------------------------------------------------------------------------------
    protected virtual void ShowAccessRequirementsUI()
    {
        if (instance == null)
            instance = FindObjectOfType<UCE_UI_InteractableAccessRequirement>();

        instance.Show(this);
    }

    // -----------------------------------------------------------------------------------
    // HideAccessRequirementsUI
    // @Client
    // -----------------------------------------------------------------------------------
    protected void HideAccessRequirementsUI()
    {
        if (instance == null)
            instance = FindObjectOfType<UCE_UI_InteractableAccessRequirement>();

        instance.Hide();
    }

    // -----------------------------------------------------------------------------------
    // IsWorthUpdating
    // -----------------------------------------------------------------------------------
    public virtual bool IsWorthUpdating()
    {
        return netIdentity.observers == null ||
               netIdentity.observers.Count > 0;
    }

    // -----------------------------------------------------------------------------------
}
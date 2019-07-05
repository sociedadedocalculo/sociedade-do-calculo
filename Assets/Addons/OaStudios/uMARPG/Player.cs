using System.Collections;
using System.Collections.Generic;
using System.Text;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

public partial class Player {
    [SyncVar, HideInInspector]
    public bool gender;

    public SyncListUmaData umaSyncData = new SyncListUmaData();
    public SyncListDna dna = new SyncListDna();
    private DynamicCharacterAvatar umaAvatar;

    private bool mounted = false;
    private float lastAppliedOffset;
    private float lastAppliedSpeedBonus;
    private Transform root;
    private Transform mountPoint;
    private Transform playerMountPoint;

    [SerializeField, Header("uMARPG")]
    public RuntimeAnimatorController runtimeAnimationController;
    [SerializeField]
    public UMAWardrobeRecipe[] maleHairs;
    [SerializeField]
    public UMAWardrobeRecipe[] maleBeards;
    [SerializeField]
    public UMAWardrobeRecipe[] femaleHairs;
    [SerializeField]
    public SharedColorTable hairColors;
    [SerializeField]
    public SharedColorTable skinColors;
    [SerializeField, Header("uMARPG Mounts")]
    private GameObject currentMount;
    [SerializeField]
    private RuntimeAnimatorController mountAnimationController;
    [SerializeField]
    private RuntimeAnimatorController mountedAnimationController;

    // Auto generate effect mount, so its not wierd.
    public override Transform effectMount {
        get {
            if (umaAvatar != null)
                return umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.RightHand);
            else
                return base.effectMount;
        }
    }

    public void Start_UMARPG() {
        dna.Callback += OnDnaChanged;
        umaSyncData.Callback += OnUmaSyncDataChanged;

        // Instantiate All Things Here.
        byte[] recipe = new byte[dna.Count];
        for (int i = 0; i < recipe.Length; i++)
            recipe[i] = dna[i].value;

        string recipeString = Encoding.UTF8.GetString(recipe);

        CreateCharacter(recipeString);

        // setup synclist callbacks on client. no need to update and show and
        // animate equipment on server
        equipment.Callback -= OnEquipmentChanged;
        equipment.Callback += OnUmaEquipmentChanged;

        playerMountPoint = transform.Find("MountPoint");
    }

    public void LateUpdate_UMARPG() {

    }

    void OnUmaSyncDataChanged(SyncListUmaData.Operation op, int index, UmaData data) {
        // Color Changed, Do the thing...
        switch (index) {
            case 0: // Hair Index Changed
                SetSlotForHairAndBeard("Hair", umaSyncData[index].value);
                break;
            case 1: // Beard Index Changed
                SetSlotForHairAndBeard("Beard", umaSyncData[index].value);
                break;
            case 2: // Skin Color Changed
                SetColors("Skin", umaSyncData[index].value);
                break;
            case 3: // Hair Color Changed
                SetColors("Hair", umaSyncData[index].value);
                break;
        }
    }

    void SetInitialColors() {
        SetSlotForHairAndBeard("Hair", umaSyncData[0].value);
        if (gender) // Male
            SetSlotForHairAndBeard("Beard", umaSyncData[1].value);
        SetColors("Skin", umaSyncData[2].value);
        SetColors("Hair", umaSyncData[3].value);
    }

    void SetSlotForHairAndBeard(string slotName, int slotIndex) {
        if (slotName == "Hair") {
            if (!gender) // Female
            {
                umaAvatar.ClearSlot(femaleHairs[slotIndex].wardrobeSlot);
                umaAvatar.SetSlot(femaleHairs[slotIndex]);
                umaAvatar.BuildCharacter();
            } else // Male
            {
                umaAvatar.ClearSlot(maleHairs[slotIndex].wardrobeSlot);
                umaAvatar.SetSlot(maleHairs[slotIndex]);
                umaAvatar.BuildCharacter();
            }
        } else if (slotName == "Beard") {
            if (!gender) // Female
            {
                umaAvatar.ClearSlot("Beard");
                umaAvatar.BuildCharacter();
            } else // Male
              {
                umaAvatar.ClearSlot(maleBeards[slotIndex].wardrobeSlot);
                umaAvatar.SetSlot(maleBeards[slotIndex]);
                umaAvatar.BuildCharacter();
            }
        }
    }

    void SetColors(string name, int colorIndex) {
        if (name == "Hair" || name == "Beard") {
            umaAvatar.SetColor("Hair", hairColors.colors[colorIndex]);
        } else if (name == "Skin") {
            umaAvatar.SetColor("Skin", skinColors.colors[colorIndex]);
        }
    }

    void OnUmaEquipmentChanged(SyncListItemSlot.Operation op, int index, ItemSlot slot) {
        // update the UMA Avatar
        // RefreshUMALocations(equipmentInfo[index].requiredCategory, equipment[index]);
        RefreshUMALocation(index);
    }

    void OnStartLocalPlayer_UMARPG() {
        // Nothing to do here.
    }

    void CreateCharacter(string savedPlayer) {
        // Setup the UMA basics
        umaAvatar = gameObject.GetComponent<DynamicCharacterAvatar>();

        // Load our UMA based on the string we sent
        umaAvatar.LoadFromRecipeString(savedPlayer, DynamicCharacterAvatar.GetLoadOptionsFlags(true, true, false, true, false));

        // IMPORTANT to set this up before loading!
        if (mounted) {
            umaAvatar.animationController = mountedAnimationController;
            GetComponent<Animator>().runtimeAnimatorController = mountedAnimationController;
        } else {
            umaAvatar.animationController = runtimeAnimationController;
            GetComponent<Animator>().runtimeAnimatorController = runtimeAnimationController;
        }

        umaAvatar.CharacterCreated.SetListener(CharacterCreationComplete);
        umaAvatar.BuildCharacter();
    }

    private void CharacterCreationComplete(UMAData data) {
        //Set up Capsule Collider so uMMORPG doesn't brake.
        CapsuleCollider cc = gameObject.AddComponent<CapsuleCollider>();
        cc.radius = umaAvatar.umaData.characterRadius;
        cc.height = umaAvatar.umaData.characterHeight;
        cc.center = new Vector3(0, cc.height / 2.0f, 0);

        // Get the player and set the collider.
        gameObject.GetComponent<Player>().collider = cc;

        // Generate Weapon Parent Locations.
        // Default uMMORPG Slots Are 0 and 4
        equipmentInfo[0].location = umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.RightHand); // Sword - Right Hand
        equipmentInfo[4].location = umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.LeftHand); // Shield - Left Hand

        // refresh all locations once (on synclist changed won't be called for initial lists)
        for (int i = 0; i < equipment.Count; ++i)
            RefreshUMALocation(i);

        // refresh all colors once. (on synclist changed won't be called for initial lists)
        SetInitialColors();
    }

    void OnDnaChanged(SyncListDna.Operation op, int index, Dna d) {
        // Character dna changed, behave accordingly.
        byte[] recipe = new byte[dna.Count];
        for (int i = 0; i < recipe.Length; i++)
            recipe[i] = dna[i].value;

        string recipeString = Encoding.UTF8.GetString(recipe);

        CreateCharacter(recipeString);
    }

    public void RefreshUMALocation(int index) {
        ItemSlot slot = equipment[index];
        EquipmentInfo info = equipmentInfo[index];

        // v1.2 Mounts.
        // If category is mount, different logic applies.
        if (info.requiredCategory == "Mount") {
            if (slot.amount > 0) {
                // Equipped a mount.
                EquipmentItem template = (EquipmentItem)slot.item.data;

                if (template.modelPrefab != null) {
                    var go = (GameObject)Instantiate(template.modelPrefab);

                    NavMeshAgent nma = GetComponent<NavMeshAgent>();
                    if (nma != null) {
                        lastAppliedOffset = slot.item.AgentOffset;
                        lastAppliedSpeedBonus = slot.item.SpeedBonus;

                        nma.baseOffset += slot.item.AgentOffset;
                        nma.speed += slot.item.SpeedBonus;
                    }

                    go.transform.rotation = Quaternion.Euler(slot.item.ApplyRotation);
                    go.transform.localScale = slot.item.ApplyScale;
                    go.transform.SetParent(playerMountPoint, false);
                    go.transform.localPosition = slot.item.ApplyOffset;

                    // Move Root to MountPoint from go.
                    // Find Root first.
                    root = transform.Find("Root");
                    mountPoint = go.transform.FindRecursively("MountPoint");

                    // Set players root to mount object
                    // Apply localrotation if needed.
                    /*root.SetParent(mountPoint, true);
                    mountPoint.transform.localRotation = Quaternion.Euler(
                            new Vector3(item.ApplyLocalRotationToPlayer.x, 
                                        item.ApplyLocalRotationToPlayer.y,
                                        item.ApplyLocalRotationToPlayer.z));*/

                    // Set Animator and rebind
                    go.GetComponent<Animator>().runtimeAnimatorController = mountAnimationController;
                    go.GetComponent<Animator>().Rebind();

                    currentMount = go;
                    mounted = true;
                } else {
                    //Offset the NavMeshAgent.
                    NavMeshAgent nma = GetComponent<NavMeshAgent>();
                    if (nma != null) {
                        nma.baseOffset -= lastAppliedOffset;
                        nma.speed -= lastAppliedSpeedBonus;
                    }

                    mounted = false;
                }

                if (mounted) {
                    umaAvatar.animationController = mountedAnimationController;
                    GetComponent<Animator>().runtimeAnimatorController = mountedAnimationController;
                } else {
                    umaAvatar.animationController = runtimeAnimationController;
                    GetComponent<Animator>().runtimeAnimatorController = runtimeAnimationController;
                }
            } else {
                // UnEquipped a mount.
                // Clear the last mount first if exists. In Any Case.
                if (currentMount != null) {
                    root.SetParent(transform, true);
                    Destroy(currentMount.gameObject);
                    currentMount = null;

                    NavMeshAgent nma = GetComponent<NavMeshAgent>();
                    if (nma != null) {
                        nma.baseOffset -= lastAppliedOffset;
                        nma.speed -= lastAppliedSpeedBonus;
                    }

                    mounted = false;

                    umaAvatar.animationController = runtimeAnimationController;
                    GetComponent<Animator>().runtimeAnimatorController = runtimeAnimationController;
                }
            }

            return;
        }

        // Equipped an armor item.
        // Old uMMORPG implementation, still works for Weapons and Shield
        if (slot.amount > 0 && (slot.item.Slot == ScriptableItem.UmaSlot.LeftHand || slot.item.Slot == ScriptableItem.UmaSlot.RightHand)) {
            if (slot.item.IsMount)
                return;

            EquipmentItem template = (EquipmentItem)slot.item.data;
            Transform parent = null;

            if (slot.item.Slot == ScriptableItem.UmaSlot.LeftHand) {

                parent = umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.LeftHand);

                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (parent.childCount > 5)
                    Destroy(parent.GetChild(5).gameObject);

            } else {
                parent = umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.RightHand);

                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (parent.childCount > 5)
                    Destroy(parent.GetChild(5).gameObject);
            }

            // load the resource
            var go = Instantiate(template.modelPrefab);
            go.transform.rotation = Quaternion.Euler(slot.item.ApplyRotation);
            go.transform.localScale = slot.item.ApplyScale;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = slot.item.ApplyOffset;
        } else if (slot.amount > 0 && slot.item.Slot != ScriptableItem.UmaSlot.None && slot.item.MaleRecipe != null && slot.item.FemaleRecipe != null) {
            if (slot.item.Slot != ScriptableItem.UmaSlot.LeftHand || slot.item.Slot != ScriptableItem.UmaSlot.RightHand) {
                // Its not a weapon, or shield, or off-hand weapon etc.
                umaAvatar.ClearSlot(gender ? slot.item.MaleRecipe.wardrobeSlot : slot.item.FemaleRecipe.wardrobeSlot);
                umaAvatar.SetSlot(gender ? slot.item.MaleRecipe : slot.item.FemaleRecipe);
                umaAvatar.BuildCharacter();
            } else {
                Debug.Log("ITEM WITHOUT SLOT: " + slot.item.name);
                // Its a weapon, or shield or off-hand weapon etc.
                // So we just need to attach that into a bone and be done with that.
                // Already done above, wth was i thinking?
            }
        } else // UnEquipped an armor or equipped item does not have any UMA Recipes.
        {
            // We don't care any item that does not have UMA Recipe.
            // We just clear that slot
            // Convert uMMORPG Slots into UMA Slots.
            string wardrobeSlot = GetUMASlot(info.requiredCategory);

            // Right Hand = 0
            // Left Hand = 4

            if (wardrobeSlot == "LeftHand" || wardrobeSlot == "RightHand") {
                // If right hand remove right hand item
                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (equipmentInfo[0].location != null && wardrobeSlot == "RightHand")
                    if (equipmentInfo[0].location.childCount > 5)
                        Destroy(equipmentInfo[0].location.GetChild(5).gameObject);

                // If left hand, remove left hand item
                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (equipmentInfo[4].location != null && wardrobeSlot == "LeftHand")
                    if (equipmentInfo[4].location.childCount > 5)
                        Destroy(equipmentInfo[4].location.GetChild(5).gameObject);

            } else {
                umaAvatar.ClearSlot(wardrobeSlot);
                if (wardrobeSlot == "Legs") {
                    if (gender)
                        umaAvatar.SetSlot("Underwear", "MaleUnderwear");
                    else
                        umaAvatar.SetSlot("Underwear", "FemaleUndies");
                }
                umaAvatar.BuildCharacter();
            }
        }
    }

    string GetUMASlot(string uMMORPGSlot) {
        switch (uMMORPGSlot) {
            case "Head":
                return "Helmet";
            case "Chest":
                return "Chest";
            case "Legs":
                return "Legs";
            case "Shoulders":
                return "Shoulders";
            case "Hands":
                return "Hands";
            case "Feet":
                return "Feet";
            case "WeaponSword":
                return "RightHand";
            case "WeaponBow":
                return "RightHand";
            case "Shield":
                return "LeftHand";

            default:
                return "";
        }
    }

}

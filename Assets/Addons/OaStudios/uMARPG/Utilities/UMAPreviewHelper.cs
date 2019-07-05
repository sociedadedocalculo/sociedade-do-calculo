using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public class UMAPreviewHelper : MonoBehaviour {

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
    [SerializeField]
    public RuntimeAnimatorController runtimeAnimationController;

    public EquipmentInfo[] equipmentInfo = new EquipmentInfo[] {
        new EquipmentInfo{requiredCategory="Weapon", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Head", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Chest", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Legs", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Shield", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Shoulders", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Hands", location=null, defaultItem=null},
        new EquipmentInfo{requiredCategory="Feet", location=null, defaultItem=null}
    };

    public void SetInitialColors(byte hairIndex, byte beardIndex, byte skinColor, byte hairColor, bool gender, DynamicCharacterAvatar umaAvatar) {
        SetSlotForHairAndBeard("Hair", hairIndex, gender, umaAvatar);
        if (gender) // Male
            SetSlotForHairAndBeard("Beard", beardIndex, gender, umaAvatar);
        SetColors("Skin", skinColor, umaAvatar);
        SetColors("Hair", hairColor, umaAvatar);
    }

    public void SetSlotForHairAndBeard(string slotName, int slotIndex, bool gender, DynamicCharacterAvatar umaAvatar) {
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

    public void SetColors(string name, int colorIndex, DynamicCharacterAvatar umaAvatar) {
        if (name == "Hair" || name == "Beard") {
            umaAvatar.SetColor("Hair", hairColors.colors[colorIndex]);
        } else if (name == "Skin") {
            umaAvatar.SetColor("Skin", skinColors.colors[colorIndex]);
        }
    }

    public void CreateCharacter(string savedPlayer, DynamicCharacterAvatar avatar, CharactersAvailableMsg.CharacterPreview character, Player player) {
        avatar.RecipeUpdated.SetListener((UMAData data) => { });
        avatar.CharacterCreated.SetListener((UMAData data) => {
            //Set up Capsule Collider so uMMORPG doesn't brake.
            CapsuleCollider cc = avatar.gameObject.AddComponent<CapsuleCollider>();
            cc.radius = avatar.umaData.characterRadius;
            cc.height = avatar.umaData.characterHeight;
            cc.center = new Vector3(0, cc.height / 2.0f, 0);

            // Get the player and set the collider.
            avatar.gameObject.GetComponent<Player>().collider = cc;

            for (int i = 0; i < character.equipment.Length; ++i) {
                ItemSlot slot = character.equipment[i];
                player.equipment.Add(slot);
                if (slot.amount > 0) {
                    // OnEquipmentChanged won't be called unless spawned, we
                    // need to refresh manually
                    RefreshUMALocation(i, slot, avatar, character.gender);
                    Debug.Log("Refreshing For: " + character.name);
                }
            }

        });

        // Load our UMA based on the string we sent
        avatar.LoadFromRecipeString(savedPlayer, DynamicCharacterAvatar.GetLoadOptionsFlags(true, true, false, true, false));

        // IMPORTANT to set this up before loading!
        avatar.animationController = runtimeAnimationController;
        avatar.gameObject.GetComponent<Animator>().runtimeAnimatorController = runtimeAnimationController;

        avatar.BuildCharacter();
    }

    public void RefreshUMALocation(int index, ItemSlot slot, DynamicCharacterAvatar umaAvatar, bool gender) {
        EquipmentInfo info = equipmentInfo[index];

        // v1.2 Mounts.
        // If category is mount, different logic applies.

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
                if (parent != null)
                    if (parent.childCount > 5)
                        Destroy(parent.GetChild(5).gameObject);

            } else {
                parent = umaAvatar.umaData.animator.GetBoneTransform(HumanBodyBones.RightHand);

                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (parent != null)
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

            if (string.IsNullOrEmpty(wardrobeSlot)) {
                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (equipmentInfo[0].location != null)
                    if (equipmentInfo[0].location.childCount > 5)
                        Destroy(equipmentInfo[0].location.GetChild(5).gameObject);

                // remove previous item if exists
                // UMA Has 5 Child bones, remove 6th.
                if (equipmentInfo[4].location != null)
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

            default:
                return "";
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public partial class ScriptableItem
{

    public enum UmaSlot : byte
    {
        None = 0,
        Helmet = 1,
        Shoulders = 2,
        Chest = 3,
        Hands = 4,
        Legs = 5,
        Feet = 6,
        LeftHand = 7,
        RightHand = 8,
        Hair = 9,
        Beard = 10,
        Mount = 11
    }

    [Header("uMARPG")]
    [SerializeField]
    public UmaSlot slot;

    [SerializeField]
    public Vector3 applyRotation;

    [SerializeField]
    public Vector3 applyScale;

    [SerializeField]
    public Vector3 applyOffset;

    [SerializeField]
    public UMAWardrobeRecipe maleRecipe;

    [SerializeField]
    public UMAWardrobeRecipe femaleRecipe;

    [SerializeField]
    public bool isMount;

    [SerializeField]
    public float agentOffset;

    [SerializeField]
    public float speedBonus;

    [SerializeField]
    public Vector3 applyLocalRotationToPlayer;
}

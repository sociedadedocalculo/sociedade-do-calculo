using System.Collections;
using System.Collections.Generic;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;

public partial struct Item
{
    public Vector3 ApplyLocalRotationToPlayer { get { return data.applyLocalRotationToPlayer; } }
    public bool IsMount { get { return data.isMount; } }
    public float AgentOffset { get { return data.agentOffset; } }
    public float SpeedBonus { get { return data.speedBonus; } }
    public ScriptableItem.UmaSlot Slot { get { return data.slot; } }
    public UMAWardrobeRecipe MaleRecipe { get { return data.maleRecipe; } }
    public UMAWardrobeRecipe FemaleRecipe { get { return data.femaleRecipe; } }
    public Vector3 ApplyRotation { get { return data.applyRotation; } }
    public Vector3 ApplyScale { get { return data.applyScale; } }
    public Vector3 ApplyOffset { get { return data.applyOffset; } }
}

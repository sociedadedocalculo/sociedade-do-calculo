using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public partial class Entity {

    void OnDamageReceived_SoundManager(int amount, DamageType type)
    {
        if(this is Player)
            ((Player)this).OnDamaged(amount, type);
    }	
}

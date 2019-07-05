using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public struct Dna {
    public byte value;
}

public class SyncListDna : SyncListSTRUCT<Dna> { }

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[System.Serializable]
public struct UmaData {
    public byte value;
}

public class SyncListUmaData : SyncListSTRUCT<UmaData> { }
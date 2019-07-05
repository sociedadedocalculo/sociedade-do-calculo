using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CopyTransform : MonoBehaviour {

    [SerializeField]
    public Transform source;

    [SerializeField]
    public Transform target;

    void LateUpdate()
    {
        target.position = source.position;
    }
}

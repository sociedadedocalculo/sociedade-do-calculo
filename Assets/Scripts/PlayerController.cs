using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    public float moveSpeed = 20.0f;
    private Vector3 pos; 

    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        pos = transform.position;
        
        pos.x += moveSpeed * Input.GetAxis("Horizontal") * Time.deltaTime;
        pos.y += moveSpeed * Input.GetAxis("Vertical") * Time.deltaTime;
        transform.position = pos;
    }

 

}
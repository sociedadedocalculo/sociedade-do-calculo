using UnityEngine;
using System.Collections;
 
public class moveAxis : MonoBehaviour {
 
    float velocidade;
    float rotacao;
 
    void Start () {
        velocidade = 20.0F;
        rotacao = 60.0F;
    }
     
    // Update is called once per frame
    void Update () {
        float translate = (Input.GetAxis ("Vertical") * velocidade) * Time.deltaTime;
        float rotate = (Input.GetAxis ("Horizontal") * rotacao) * Time.deltaTime;
 
        transform.Translate (0, 0, translate);
        transform.Rotate (0, rotate, 0);
    }
}
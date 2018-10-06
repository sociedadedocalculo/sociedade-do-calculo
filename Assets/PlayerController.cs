using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    float velocidadeFrente;
    float velocidadeTras;
    float velocidadeRotacao;

	// Inicialização
	void Start () {
        velocidadeFrente = 10;
        velocidadeTras = 5;
        velocidadeRotacao = 60;
		
	}


   // Update is called once per frame
    void Update ()
    {
       if(Input.GetKey ("w")){
            transform.Translate(0, 0, (velocidadeFrente * Time.deltaTime));
        }
 
        if(Input.GetKey ("s")){
            transform.Translate(0, 0, (-velocidadeTras * Time.deltaTime));
        }
 
        if(Input.GetKey ("a")){
            transform.Rotate(0,(-velocidadeRotacao * Time.deltaTime), 0);
        }
         
        if(Input.GetKey ("d")){
            transform.Rotate(0,(velocidadeRotacao * Time.deltaTime), 0);
        }
}
}
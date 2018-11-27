using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum tipoPersonagem{
	Hobbit,
	Anao,
	MagoNego,
	Humano,
	Elfo
}

public class PlayerBehaviour : MonoBehaviour {

	public tipoPersonagem tipo;

	// Use this for initialization
	protected void Start () {
		base.Start();
		currentLevel = PlayerStatsController.GetCurrentLevel();
		tipo = PlayerStatsController.getTipoPersonagem();

	switch (tipo){
		case tipoPersonagem.Anao:{
			basicStats
		}
	}
	{
		
		default:
	}
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

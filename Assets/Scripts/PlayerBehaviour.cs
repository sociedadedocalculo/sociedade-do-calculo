using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TypeCharacter{
	Hobbit = 0,
	Anao = 1,
	MagoNego = 2,
	Humano = 3,
	Elfo = 4
}

public class PlayerBehaviour : CharacterBase {
	
	private TypeCharacter tipo;
	

	// Use this for initialization
	protected void Start(){
		base.Start();
		PlayerStatsController.SetTypeCharacter(TypeCharacter.Hobbit);
		currentLevel = PlayerStatsController.GetCurrentLevel();
		tipo = PlayerStatsController.GetTypeCharacter();

	// switch (tipo){
	// 	case tipoPersonagem.Anao:{
	// 		basicStats
	// 	}
	// }

		basicStats = PlayerStatsController.intance.GetBasicStats(tipo);
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

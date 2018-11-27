using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.SerializableAttribute]
	public class BasicStats{
		public float startLife;
		public float startMana;

		public int forca;
		public int magia;
		public int agilidade;
		public int baseDefesa;
		public int baseAtaque;
	}


public abstract class CharacterBase : MonoBehaviour {

	//Atributos básicos
	public int currentLevel;
	public BasicStats basicStats;

	

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}

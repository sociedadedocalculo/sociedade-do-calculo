using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BasicInfoChar{
	public BasicStats baseInfo;
	public tipoPersonagem tipoPersonagem;
}
public class PlayerStatsController : MonoBehaviour {

	public static PlayerStatsController intance;

	public int xpMultiply = 1;
	public float xpFirstLevel = 100;
	public float difficultFactor = 1.5f;

	public List<BasicStats> baseInfoChars;
	private float xpNextLevel;
	// Use this for initialization
	void Start () {
		 intance = this;
		 DontDestroyOnLoad(gameObject);
		 Application.LoadLevel("ilha1");
	}
	
	// Update is called once per frame
	void Update () {

		if(Input.GetKeyDown(KeyCode.A))
		AddXp(100);
		if(Input.GetKeyDown(KeyCode.R))
		PlayerPrefs.DeleteAll();
		
	}

	public static void AddXp(float xpAdd){
		float newXp = (GetCurrentXp()+ xpAdd) * PlayerStatsController.intance.xpMultiply;
		
		if(newXp > GetNextXp()){
			AddLevel();
			newXp = 0;
		}
	
		PlayerPrefs.SetFloat("currentXp", newXp);
	}
	public static float GetCurrentXp(){
		return PlayerPrefs.GetFloat("currentXp");
	}

	public static int GetCurrentLevel(){
		return PlayerPrefs.GetInt("currentLevel");
	}
	
	public static void AddLevel(){
		int newLevel = GetCurrentLevel()+1;
		PlayerPrefs.SetInt("currentLevel", newLevel);
	}
	
	public static float GetNextXp(){
		return PlayerStatsController.intance.xpFirstLevel*(GetCurrentLevel()+1) * PlayerStatsController.intance.difficultFactor;
	}

	public static void SetTipoPersonagem(tipoPersonagem novoTipo){
		PlayerPrefs.SetInt("Tipo personagem...", (int)novoTipo);
	}

	public BasicStats GetBasicStats(tipoPersonagem tipo){
		foreach(baseInfoChar info in baseInfoChars){
			if(info.tipoPersonagem == type)
			return info.baseInfo;

		}
		return baseInfoChars[0].baseInfo;

	}

	void OnGUI()
	{
		GUI.Label(new Rect(0,0,200,50), "Current Xp"+GetCurrentXp());
		GUI.Label(new Rect(0,15,200,50), "Current Level"+GetCurrentLevel());
		GUI.Label(new Rect(0,30,200,50), "Current Next Up"+GetNextXp());
	}
}

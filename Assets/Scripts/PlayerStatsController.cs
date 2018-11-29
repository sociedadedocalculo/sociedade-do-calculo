using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BasicInfoChar{
	public BasicStats baseInfo;
	public TypeCharacter tipoPersonagem;
}
public class PlayerStatsController : MonoBehaviour {

	public static PlayerStatsController intance;

	public int xpMultiply = 1;
	public float xpFirstLevel = 100;
	public float difficultFactor = 1.5f;

	public List<BasicInfoChar> baseInfoChars;
	private float xpNextLevel;

    public PlayerStatsController()
    {
    }

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
		return PlayerStatsController.intance.xpFirstLevel*(GetCurrentLevel()+1)*PlayerStatsController.intance.difficultFactor;
	}

	public static TypeCharacter GetTypeCharacter(){
		
		int tipoId = PlayerPrefs.GetInt("TypeCharacter");
		
		if(tipoId == 0)
			return TypeCharacter.Hobbit;
		else if(tipoId == 1)
			return TypeCharacter.Anao;
		else if(tipoId == 2)
			return TypeCharacter.MagoNego;
				else if(tipoId == 3)
			return TypeCharacter.Humano;
		
			else if(tipoId == 4)
			return TypeCharacter.Elfo;
		
		return TypeCharacter.Hobbit;
	}

	public static void SetTypeCharacter(TypeCharacter novoTipo){
		PlayerPrefs.SetInt("Tipo personagem...", (int)novoTipo);
	}
	

	public BasicStats GetBasicStats(TypeCharacter tipo){
		foreach(baseInfo info in baseInfoChars){
			if(info.tipoPersonagem == tipo)
			return info.baseInfo;

		}
		return baseInfoChars[0].baseInfo;

	}

	void OnGUI(){
		GUI.Label(new Rect(0, 0, 200, 50), "Current Xp = "+GetCurrentXp());
		GUI.Label(new Rect(0, 15, 200, 50), "Current Level = "+GetCurrentLevel());
		GUI.Label(new Rect(0, 30, 200, 50), "Current Next Xp = "+GetNextXp());
	}
}

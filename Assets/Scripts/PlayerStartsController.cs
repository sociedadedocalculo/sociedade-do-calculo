using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStartsController : MonoBehaviour {

	public static PlayerStartsController intance;

	public int xpMultiply = 1;
	public float xpFirstLevel = 100;
	public float difficultFactor = 1.5f;

	private float xpNextLevel;
	// Use this for initialization
	void Start () {
		 intance = this;
		 DontDestroyOnLoad(gameObject);
		 Application.LoadLevel("ilha1");
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public static void AddXp(float xpAdd){
		float newXp = (GetCurrent()+ xpAdd) * PlayerStartsController.intance.xpMultiply;
		float diffXp = GetCurrent() - xpNextLevel;
		if(diffXp >= xpNextLevel){
			AddLevel();
			newXp = diffXp;
		}
		if(diffXp > 0)
		AddXp(diffXp);

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
	
	public static void GetNextXp(){
		return PlayerStartsController.intance.xpFirstLevel * (GetCurrentLevel()+1) * PlayerStartsController.intance.difficultFactor;
	}

	void OnGUI()
	{
		GUI.Label(new Rect(0,0,200,50), "Current Xp"+GetCurrentXp());
	}
}

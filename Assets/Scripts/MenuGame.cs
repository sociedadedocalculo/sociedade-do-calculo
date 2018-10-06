using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuGame : MonoBehaviour
{

	private void Start()
	{
		Cursor.visible = true;
		Cursor.lockState = CursorLockMode.None;
	}

	public void Iniciar() {
		SceneManager.LoadScene("CenaTeste");
		Cursor.visible = false;
		Cursor.lockState = CursorLockMode.Locked;
	}

	public void Sair()
	{
		Application.Quit();
	}
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{

    float velocidadeFrente;
    float velocidadeTras;
    float velocidadeRotacao;

    public GameObject[] _jogadores = new GameObject[4];
    public int[] _dupla = new int[2];
    public static string _playerName = "";

    private int _playerAtual;
    private int _indiceAtual = 0;

    // Inicialização
    void Start()
    {

    }


    // Update is called once per frame
    void Update()
    {
        /*  if (_playerName != "")
         {
             int _parceiroPos = TrocarParceiro(_playerName);

             if (_parceiroPos >= 0)
             {
                 if (_indiceAtual == 0)
                 {
                     _dupla[1] = _parceiroPos;
                     AlterarPlayer(_dupla[0]);
                 }
                 else
                 {
                     _dupla[0] = _parceiroPos;
                     AlterarPlayer(_dupla[1]);
                 }
             }
             _playerName = "";
         }


         if (Input.GetKeyDown(KeyCode.Tab))
         {
             if (_playerAtual == _dupla[0])
             {
                 _indiceAtual = 1;
                 _playerAtual = _dupla[1];
                 AlterarPlayer(_dupla[0]);
             }
             else
             {
                 _indiceAtual = 0;
                 _playerAtual = _dupla[0];
                 AlterarPlayer(_dupla[1]);
             }
         } */
    }
    private void AlterarPlayer(int _playerAntigo)
    {
        _jogadores[_playerAntigo].GetComponent<Player>().enabled = false;
        _jogadores[_playerAntigo].transform.GetChild(0).gameObject.SetActive(false);
        _jogadores[_playerAtual].GetComponent<Player>().enabled = true;
        _jogadores[_playerAtual].transform.GetChild(0).gameObject.SetActive(true);
    }

    // {
    //     int _pos = -1;

    //     for(int i = 0; i < _jogadores.Length; i++)
    //     {
    //         if (_jogadores[i].gameObject.name == name)
    //         {
    //             _pos = i;
    //         }
    //     }

    //     return _pos;
    // }


}
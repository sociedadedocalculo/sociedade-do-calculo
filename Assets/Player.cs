using System.Collections;
using System.Collections.Generic;
using UnityEngine;
 
public class Player : MonoBehaviour
{
 
    private Animator _anima;
 
    public GameObject _pontToRaycast = null;
    public float _velocidadeRotacao = 10;
    public static bool _controlar = true;
    private bool _proximoPlayer = false;
    private string _playerName = "";
    void Start()
    {
        _anima = this.GetComponent<Animator>();
    }
 
    void FixedUpdate()
    {
        _proximoPlayer = false;
        if (_pontToRaycast != null)
        {
            RaycastHit hit;
 
            if (Physics.Raycast(_pontToRaycast.transform.position, _pontToRaycast.transform.forward, out hit, 3))
            {
                Vector3 forward = transform.TransformDirection(Vector3.forward) * 10;
                Debug.DrawRay(_pontToRaycast.transform.position, forward, Color.red);
 
                if (hit.collider.GetComponent<Player>())
                {
                    _playerName = hit.collider.gameObject.name;
                    _proximoPlayer = true;
                }
            }
        }
    }
 
    void Update()
    {
        if (_controlar)
        {
            _anima.SetFloat("Andar", Input.GetAxis("Vertical"));
            this.transform.Rotate(0, ((_velocidadeRotacao * Input.GetAxis("Horizontal")) * Time.deltaTime), 0);
 
            if (_proximoPlayer == true && Input.GetKeyDown(KeyCode.Space))
            {
                PlayerController._playerName = _playerName;
            }
        }
    }
}
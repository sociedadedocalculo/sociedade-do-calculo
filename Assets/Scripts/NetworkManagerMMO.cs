// We use a custom NetworkManager that also takes care of login, character
// selection, character creation and more.
//
// We don't use the playerPrefab, instead all available player classes should be
// dragged into the spawnable objects property.
//
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using UnityEngine;
using Mirror;
#if UNITY_EDITOR
using UnityEditor;
#endif

// we need a clearly defined state to know if we are offline/in world/in lobby
// otherwise UICharacterSelection etc. never know 100% if they should be visible
// or not.
public enum NetworkState {Offline, Handshake, Lobby, World}

public partial class NetworkManagerMMO : NetworkManager
{
    // current network manager state on client
    public NetworkState state = NetworkState.Offline;

    // <conn, account> dict for the lobby
    // (people that are still creating or selecting characters)
    Dictionary<NetworkConnection, string> lobby = new Dictionary<NetworkConnection, string>();

    // UI components to avoid FindObjectOfType
    [Header("UI")]
    public UIPopup uiPopup;

    // login info for the local player
    // we don't just name it 'account' to avoid collisions in handshake
    [Header("Login")]
    public string loginAccount = "";
    public string loginPassword = "";

    // we may want to add another game server if the first one gets too crowded.
    // the server list allows people to choose a server.
    //
    // note: we use one port for all servers, so that a headless server knows
    // which port to bind to. otherwise it would have to know which one to
    // choose from the list, which is far too complicated. one port for all
    // servers will do just fine for an Indie MMORPG.
    [Serializable]
    public class ServerInfo
    {
        public string name;
        public string ip;
    }
    public List<ServerInfo> serverList = new List<ServerInfo>() {
        new ServerInfo{name="Local", ip="localhost"}
    };

    [Header("Selecao de Personagem")]
    public int selection = -1;
    public Transform[] selectionLocations;
    public Transform selectionCameraLocation;

    [Header("Database")]
    public int characterLimit = 4;
    public int characterNameMaxLength = 16;
    public int accountMaxLength = 16;
    public float saveInterval = 60f; // in seconds

    // store characters available message on client so that UI can access it
    [HideInInspector] public CharactersAvailableMsg charactersAvailableMsg;

    // name checks /////////////////////////////////////////////////////////////
    public bool IsAllowedAccountName(string account)
    {
        // not too long?
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        return account.Length <= accountMaxLength &&
               Regex.IsMatch(account, @"^[a-zA-Z0-9_]+$");
    }

    public bool IsAllowedCharacterName(string characterName)
    {
        // not too long?
        // only contains letters, number and underscore and not empty (+)?
        // (important for database safety etc.)
        return characterName.Length <= characterNameMaxLength &&
               Regex.IsMatch(characterName, @"^[a-zA-Z0-9_]+$");
    }

    // events //////////////////////////////////////////////////////////////////
    void Start()
    {
        // headless mode? then automatically start a dedicated server
        // (because we can't click the button in headless mode)
        // -> only if not started yet so that addons can also start it if needed
        //    (e.g. network zones)
        if (Utils.IsHeadless() && !NetworkServer.active)
        {
            print("headless mode detected, starting dedicated server");
            StartServer();
        }

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "Start_");
    }

    void Update()
    {
        // any valid local player? then set state to world
        if (ClientScene.localPlayer != null)
            state = NetworkState.World;
    }

    // client popup messages ///////////////////////////////////////////////////
    void ClientSendPopup(NetworkConnection conn, string error, bool disconnect)
    {
        ErrorMsg message = new ErrorMsg{text=error, causesDisconnect=disconnect};
        conn.Send(ErrorMsg.MsgId, message);
    }

    void OnClientReceivePopup(NetworkMessage netMsg)
    {
        ErrorMsg message = netMsg.ReadMessage<ErrorMsg>();
        print("OnClientReceivePopup: " + message.text);

        // show a popup
        uiPopup.Show(message.text);

        // disconnect if it was an important network error
        // (this is needed because the login failure message doesn't disconnect
        //  the client immediately (only after timeout))
        if (message.causesDisconnect)
        {
            netMsg.conn.Disconnect();

            // also stop the host if running as host
            // (host shouldn't start server but disconnect client for invalid
            //  login, which would be pointless)
            if (NetworkServer.active) StopHost();
        }
    }

    // start & stop ////////////////////////////////////////////////////////////
    public override void OnStartServer()
    {
        // handshake packet handlers (in OnStartServer so that reconnecting works)
        NetworkServer.RegisterHandler(LoginMsg.MsgId, OnServerLogin);
        NetworkServer.RegisterHandler(CharacterCreateMsg.MsgId, OnServerCharacterCreate);
        NetworkServer.RegisterHandler(CharacterDeleteMsg.MsgId, OnServerCharacterDelete);

#if !UNITY_EDITOR
        // server only? not host mode?
        if (!NetworkClient.active)
        {
            // set a fixed tick rate instead of updating as often as possible
            // -> updating more than 50x/s is just a waste of CPU power that can
            //    be used by other threads like network transport instead
            // -> note: doesn't work in the editor
            Application.targetFrameRate = Mathf.RoundToInt(1f / Time.fixedDeltaTime);
            print("server tick rate set to: " + Application.targetFrameRate + " (1 / Edit->Project Settings->Time->Fixed Time Step)");
        }
#endif

        // invoke saving
        InvokeRepeating("SavePlayers", saveInterval, saveInterval);

        // call base function to guarantee proper functionality
        base.OnStartServer();

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnStartServer_");
    }

    public override void OnStopServer()
    {
        print("OnStopServer");
        CancelInvoke("SavePlayers");

        // call base function to guarantee proper functionality
        base.OnStopServer();

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnStopServer_");
    }

    // handshake: login ////////////////////////////////////////////////////////
    public bool IsConnecting()
    {
        return NetworkClient.active && !ClientScene.ready;
    }

    public override void OnClientConnect(NetworkConnection conn)
    {
        print("OnClientConnect");

        // setup handlers
        client.RegisterHandler(CharactersAvailableMsg.MsgId, OnClientCharactersAvailable);
        client.RegisterHandler(ErrorMsg.MsgId, OnClientReceivePopup);

        // send login packet with hashed password, so that the original one
        // never leaves the player's computer.
        //
        // it's recommended to use a different salt for each hash. ideally we
        // would store each user's salt in the database. to not overcomplicate
        // things, we will use the account name as salt (at least 16 bytes)
        //
        // Application.version can be modified under:
        // Edit -> Project Settings -> Player -> Bundle Version
        string hash = Utils.PBKDF2Hash(loginPassword, "at_least_16_byte" + loginAccount);
        LoginMsg message = new LoginMsg{account=loginAccount, password=hash, version=Application.version};
        conn.Send(LoginMsg.MsgId, message);
        print("mensagem de login foi enviada...");

        // set state
        state = NetworkState.Handshake;

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnClientConnect_", conn);

        // call base function to make sure that client becomes "ready"
        //base.OnClientConnect(conn);
        ClientScene.Ready(conn); // from bitbucket OnClientConnect source
    }

    // the default OnClientSceneChanged sets the client as ready automatically,
    // which makes no sense for MMORPG situations. this was more for situations
    // where the server tells all clients to load a new scene.
    // -> setting client as ready will cause 'already set as ready' errors if
    //    we call StartClient before loading a new scene (e.g. for zones)
    // -> it's best to just overwrite this with an empty function
    public override void OnClientSceneChanged(NetworkConnection conn) {}

    bool AccountLoggedIn(string account)
    {
        // in lobby or in world?
        return lobby.ContainsValue(account) ||
               Player.onlinePlayers.Values.Any(p => p.account == account);
    }

    // helper function to make a CharactersAvailableMsg from all characters in
    // an account
    CharactersAvailableMsg MakeCharactersAvailableMessage(string account)
    {
        // load from database
        List<Player> characters = Database.CharactersForAccount(account)
                                    .Select(character => Database.CharacterLoad(character, GetPlayerClasses()))
                                    .Select(go => go.GetComponent<Player>())
                                    .ToList();

        // construct the message
        CharactersAvailableMsg message = new CharactersAvailableMsg();
        message.Load(characters);

        // destroy the temporary players again and return the result
        characters.ForEach(player => Destroy(player.gameObject));
        return message;
    }

    void OnServerLogin(NetworkMessage netMsg)
    {
        print("OnServerLogin " + netMsg.conn);
        LoginMsg message = netMsg.ReadMessage<LoginMsg>();

        // correct version?
        if (message.version == Application.version)
        {
            // allowed account name?
            if (IsAllowedAccountName(message.account))
            {
                // validate account info
                if (Database.IsValidAccount(message.account, message.password))
                {
                    // not in lobby and not in world yet?
                    if (!AccountLoggedIn(message.account))
                    {
                        print("Login ok: " + message.account);

                        // add to logged in accounts
                        lobby[netMsg.conn] = message.account;

                        // send necessary data to client
                        CharactersAvailableMsg reply = MakeCharactersAvailableMessage(message.account);
                        netMsg.conn.Send(CharactersAvailableMsg.MsgId, reply);

                        // addon system hooks
                        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerLogin_", message);
                    }
                    else
                    {
                        print("conta ja logada: " + message.account);
                        ClientSendPopup(netMsg.conn, "ja logada", true);

                        // note: we should disconnect the client here, but we can't as
                        // long as unity has no "SendAllAndThenDisconnect" function,
                        // because then the error message would never be sent.
                        //netMsg.conn.Disconnect();
                    }
                }
                else
                {
                    print("Senha ou conta invalida: " + message.account);
                    ClientSendPopup(netMsg.conn, "conta invalida", true);
                }
            }
            else
            {
                print("nome da conta nao permitido: " + message.account);
                ClientSendPopup(netMsg.conn, "nome da conta nao permitido", true);
            }
        }
        else
        {
            print("incompatibilidade de versao: " + message.account + " esperado:" + Application.version + " recebido: " + message.version);
            ClientSendPopup(netMsg.conn, "versao desatualizada", true);
        }
    }

    // handshake: character selection //////////////////////////////////////////
    void LoadPreview(GameObject prefab, Transform location, int selectionIndex, CharactersAvailableMsg.CharacterPreview character)
    {
        // instantiate the prefab
        GameObject preview = Instantiate(prefab.gameObject, location.position, location.rotation);
        preview.transform.parent = location;
        Player player = preview.GetComponent<Player>();

        // assign basic preview values like name and equipment
        player.name = character.name;
        for (int i = 0; i < character.equipment.Length; ++i)
        {
            ItemSlot slot = character.equipment[i];
            player.equipment.Add(slot);
            if (slot.amount > 0)
            {
                // OnEquipmentChanged won't be called unless spawned, we
                // need to refresh manually
                player.RefreshLocation(i);
            }
        }

        // add selection script
        preview.AddComponent<SelectableCharacter>();
        preview.GetComponent<SelectableCharacter>().index = selectionIndex;
    }

    public void ClearPreviews()
    {
        selection = -1;
        foreach (Transform location in selectionLocations)
        {
            if (location.childCount > 0)
            {
                Destroy(location.GetChild(0).gameObject);
            }
        }
    }

    void OnClientCharactersAvailable(NetworkMessage netMsg)
    {
        charactersAvailableMsg = netMsg.ReadMessage<CharactersAvailableMsg>();
        print("personagens disponiveis:" + charactersAvailableMsg.characters.Length);

        // set state
        state = NetworkState.Lobby;

        // clear previous previews in any case
        ClearPreviews();

        // load previews for 3D character selection
        for (int i = 0; i < charactersAvailableMsg.characters.Length; ++i)
        {
            CharactersAvailableMsg.CharacterPreview character = charactersAvailableMsg.characters[i];

            // find the prefab for that class
            Player prefab = GetPlayerClasses().Find(p => p.name == character.className);
            if (prefab != null)
                LoadPreview(prefab.gameObject, selectionLocations[i], i, character);
            else
                Debug.LogWarning("Personagem selecionado: no prefab found for class " + character.className);
        }

        // setup camera
        Camera.main.transform.position = selectionCameraLocation.position;
        Camera.main.transform.rotation = selectionCameraLocation.rotation;

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnClientCharactersAvailable_", charactersAvailableMsg);
    }

    // called after the client calls ClientScene.AddPlayer with a msg parameter
    public override void OnServerAddPlayer(NetworkConnection conn, NetworkReader extraMsg)
    {
        print("OnServerAddPlayer extra");
        if (extraMsg != null)
        {
            // only while in lobby (aka after handshake and not ingame)
            if (lobby.ContainsKey(conn))
            {
                // read the index and find the n-th character
                // (only if we know that he is not ingame, otherwise lobby has
                //  no netMsg.conn key)
                CharacterSelectMsg message = extraMsg.ReadMessage<CharacterSelectMsg>();
                string account = lobby[conn];
                List<string> characters = Database.CharactersForAccount(account);

                // validate index
                if (0 <= message.index && message.index < characters.Count)
                {
                    print(account + " jogador selecinado " + characters[message.index]);

                    // load character data
                    GameObject go = Database.CharacterLoad(characters[message.index], GetPlayerClasses());

                    // add to client
                    NetworkServer.AddPlayerForConnection(conn, go);

                    // addon system hooks
                    Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerAddPlayer_", account, go, conn, message);

                    // remove from lobby
                    lobby.Remove(conn);
                }
                else
                {
                    print("indicide de personagem invalido: " + account + " " + message.index);
                    ClientSendPopup(conn, "indicide de personagem invalido", false);
                }
            }
            else
            {
                print("AddPlayer: not in lobby" + conn);
                ClientSendPopup(conn, "AddPlayer: not in lobby", true);
            }
        }
        else
        {
            print("missing extraMessageReader");
            ClientSendPopup(conn, "missing parameter", true);
        }
    }

    // handshake: character creation ///////////////////////////////////////////
    // find all available player classes
    public List<Player> GetPlayerClasses()
    {
        return (from go in spawnPrefabs
                where go.GetComponent<Player>() != null
                select go.GetComponent<Player>()).ToList();
    }

    // find a NetworkStartPosition for this class, or a normal one otherwise
    // (ignore the ones with playerPrefab == null)
    public Transform GetStartPositionFor(string className)
    {
        Transform spawn = startPositions.Find(
            t => t.GetComponent<NetworkStartPositionForClass>() != null &&
                 t.GetComponent<NetworkStartPositionForClass>().playerPrefab != null &&
                 t.GetComponent<NetworkStartPositionForClass>().playerPrefab.name == className
        );
        return spawn ?? GetStartPosition();
    }

    void OnServerCharacterCreate(NetworkMessage netMsg)
    {
        print("OnServerCharacterCreate " + netMsg.conn);
        CharacterCreateMsg message = netMsg.ReadMessage<CharacterCreateMsg>();

        // only while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(netMsg.conn))
        {
            // allowed character name?
            if (IsAllowedCharacterName(message.name))
            {
                // not existant yet?
                string account = lobby[netMsg.conn];
                if (!Database.CharacterExists(message.name))
                {
                    // not too may characters created yet?
                    if (Database.CharactersForAccount(account).Count < characterLimit)
                    {
                        // valid class index?
                        List<Player> classes = GetPlayerClasses();
                        if (0 <= message.classIndex && message.classIndex < classes.Count)
                        {
                            // create new character based on the prefab.
                            // -> we also assign default items and equipment for new characters
                            // -> skills are handled in Database.CharacterLoad every time. if we
                            //    add new ones to a prefab, all existing players should get them
                            // (instantiate temporary player)
                            print("criando personagem: " + message.name + " " + message.classIndex);
                            Player prefab = GameObject.Instantiate(classes[message.classIndex]).GetComponent<Player>();
                            prefab.name = message.name;
                            prefab.account = account;
                            prefab.className = classes[message.classIndex].name;
                            prefab.transform.position = GetStartPositionFor(prefab.className).position;
                            for (int i = 0; i < prefab.inventorySize; ++i)
                            {
                                // add empty slot or default item if any
                                prefab.inventory.Add(i < prefab.defaultItems.Length ? new ItemSlot(new Item(prefab.defaultItems[i])) : new ItemSlot());
                            }
                            for (int i = 0; i < prefab.equipmentInfo.Length; ++i)
                            {
                                // add empty slot or default item if any
                                EquipmentInfo info = prefab.equipmentInfo[i];
                                prefab.equipment.Add(info.defaultItem != null ? new ItemSlot(new Item(info.defaultItem), info.defaultItemAmount) : new ItemSlot());
                            }
                            prefab.health = prefab.healthMax; // after equipment in case of boni
                            prefab.mana = prefab.manaMax; // after equipment in case of boni

                            // addon system hooks
                            Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerCharacterCreate_", message, prefab);

                            // save the player
                            Database.CharacterSave(prefab, false);
                            GameObject.Destroy(prefab.gameObject);

                            // send available characters list again, causing
                            // the client to switch to the character
                            // selection scene again
                            CharactersAvailableMsg reply = MakeCharactersAvailableMessage(account);
                            netMsg.conn.Send(CharactersAvailableMsg.MsgId, reply);
                        }
                        else
                        {
                            print("classe de personagem invalida: " + message.classIndex);
                            ClientSendPopup(netMsg.conn, "classe de personagem invalida", false);
                        }
                    }
                    else
                    {
                        print("limite de personagem atingido: " + message.name);
                        ClientSendPopup(netMsg.conn, "limite de personagem atingido", false);
                    }
                }
                else
                {
                    print("Nome do personagem ja existe: " + message.name);
                    ClientSendPopup(netMsg.conn, "nome ja existe", false);
                }
            }
            else
            {
                print("nome do personagem nao permitido: " + message.name);
                ClientSendPopup(netMsg.conn, "nome do personagem nao permitido", false);
            }
        }
        else
        {
            print("Personagem Criado: not in lobby");
            ClientSendPopup(netMsg.conn, "Personagem Criado: not in lobby", true);
        }
    }

    void OnServerCharacterDelete(NetworkMessage netMsg)
    {
        print("OnServerCharacterDelete " + netMsg.conn);
        CharacterDeleteMsg message = netMsg.ReadMessage<CharacterDeleteMsg>();

        // only while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(netMsg.conn))
        {
            string account = lobby[netMsg.conn];
            List<string> characters = Database.CharactersForAccount(account);

            // validate index
            if (0 <= message.index && message.index < characters.Count)
            {
                // delete the character
                print("Deleta personagem : " + characters[message.index]);
                Database.CharacterDelete(characters[message.index]);

                // addon system hooks
                Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerCharacterDelete_", message);

                // send the new character list to client
                characters = Database.CharactersForAccount(account);
                CharactersAvailableMsg reply = MakeCharactersAvailableMessage(account);
                netMsg.conn.Send(CharactersAvailableMsg.MsgId, reply);
            }
            else
            {
                print("invalido indice de personagem: " + account + " " + message.index);
                ClientSendPopup(netMsg.conn, "invalido indice de personagem", false);
            }
        }
        else
        {
            print("Personagem Deletado: not in lobby: " + netMsg.conn);
            ClientSendPopup(netMsg.conn, "Personagem Deletado: not in lobby", true);
        }
    }

    // player saving ///////////////////////////////////////////////////////////
    // we have to save all players at once to make sure that item trading is
    // perfectly save. if we would invoke a save function every few minutes on
    // each player seperately then it could happen that two players trade items
    // and only one of them is saved before a server crash - hence causing item
    // duplicates.
    void SavePlayers()
    {
        List<Player> players = Player.onlinePlayers.Values.ToList();
        Database.CharacterSaveMany(players);
        if (players.Count > 0) Debug.Log("salvado " + players.Count + " jogador(s)");
    }

    // stop/disconnect /////////////////////////////////////////////////////////
    // called on the server when a client disconnects
    public override void OnServerDisconnect(NetworkConnection conn)
    {
        print("OnServerDisconnect " + conn);

        // save player (if any)
        if (conn.playerController != null)
        {
            Database.CharacterSave(conn.playerController.GetComponent<Player>(), false);
            print("salvado:" + conn.playerController.name);
        }
        else print("Sem jogador para salvar: " + conn);

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerDisconnect_", conn);

        // remove logged in account after everything else was done
        lobby.Remove(conn); // just returns false if not found

        // do base function logic (removes the player for the connection)
        base.OnServerDisconnect(conn);
    }

    // called on the client if he disconnects
    public override void OnClientDisconnect(NetworkConnection conn)
    {
        print("OnClientDisconnect");

        // show a popup so that users know what happened
        uiPopup.Show("Desconectado!");

        // call base function to guarantee proper functionality
        base.OnClientDisconnect(conn);

        // call StopClient to clean everything up properly (otherwise
        // NetworkClient.active remains false after next login)
        StopClient();

        // set state
        state = NetworkState.Offline;

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnClientDisconnect_", conn);
    }

    // universal quit function for editor & build
    public static void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // called when quitting the application by closing the window / pressing
    // stop in the editor
    // -> we want to send the quit packet to the server instead of waiting for a
    //    timeout
    // -> this also avoids the OnDisconnectError UNET bug (#838689) more often
    new void OnApplicationQuit()
    {
        NewMethod();
        if (IsClientConnected())
        {
            StopClient();
            print("OnApplicationQuit: stopped client");
        }
    }

    private void NewMethod()
    {
        base.OnApplicationQuit();
    }

    new void OnValidate()
    {
        base.OnValidate();

        // ip has to be changed in the server list. make it obvious to users.
        if (!Application.isPlaying && networkAddress != "")
            networkAddress = "Use a lista de servidores abaixo!!";

        // need enough character selection locations for character limit
        if (selectionLocations.Length != characterLimit)
        {
            // create new array with proper size
            Transform[] newArray = new Transform[characterLimit];

            // copy old values
            for (int i = 0; i < Mathf.Min(characterLimit, selectionLocations.Length); ++i)
                newArray[i] = selectionLocations[i];

            // use new array
            selectionLocations = newArray;
        }
    }
}

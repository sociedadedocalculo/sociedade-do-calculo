using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UMA.CharacterSystem;
using UnityEngine;

public partial class NetworkManagerMMO
{
    // Use this for initialization

    [Header("UMA for uMMORPG")]
    public UMAPreviewHelper helper;

    void Start_UMARPG()
    {
        // First DeRegister Original Character Creation Handler.
        NetworkServer.handlers.Remove(CharacterCreateMsg.MsgId);
        // Register Our Own Handler Which Uses UMA
        NetworkServer.RegisterHandler(UMARPGCharacterCreateMsg.MsgId, OnServerUMACharacterCreate);
    }

    void OnClientConnect_UMARPG(NetworkConnection connection) {
        client.UnregisterHandler(CharactersAvailableMsg.MsgId);
        client.RegisterHandler(CharactersAvailableMsg.MsgId, OnClientUMACharactersAvailable);
    }

    void OnClientUMACharactersAvailable(NetworkMessage netMsg) {
        charactersAvailableMsg = netMsg.ReadMessage<CharactersAvailableMsg>();
        print("characters available:" + charactersAvailableMsg.characters.Length);

        // set state
        state = NetworkState.Lobby;

        // clear previous previews in any case
        ClearPreviews();

        // load previews for 3D character selection
        for (int i = 0; i < charactersAvailableMsg.characters.Length; ++i) {
            CharactersAvailableMsg.CharacterPreview character = charactersAvailableMsg.characters[i];

            // find the prefab for that class
            Player prefab = GetPlayerClasses().Find(p => p.name == character.className);
            if (prefab != null)
                LoadUMAPreview(prefab.gameObject, selectionLocations[i], i, character);
            else
                Debug.LogWarning("Character Selection: no prefab found for class " + character.className);
        }

        // setup camera
        Camera.main.transform.position = selectionCameraLocation.position;
        Camera.main.transform.rotation = selectionCameraLocation.rotation;

        // addon system hooks
        Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnClientUMACharactersAvailable_", charactersAvailableMsg);
    }

    void LoadUMAPreview(GameObject prefab, Transform location, int selectionIndex, CharactersAvailableMsg.CharacterPreview character) {
        // instantiate the prefab
        GameObject preview = Instantiate(prefab.gameObject, location.position, location.rotation);
        preview.transform.parent = location;
        Player player = preview.GetComponent<Player>();

        // assign basic preview values like name and equipment
        player.name = character.name;

        // Add Dynamic Avatar so we can modify values.
        DynamicCharacterAvatar avatar = preview.GetComponent<DynamicCharacterAvatar>();

        // Instantiate All Things Here.
        byte[] recipe = new byte[character.dnaValues.Length];
        for (int i = 0; i < recipe.Length; i++)
            recipe[i] = character.dnaValues[i];

        string recipeString = Encoding.UTF8.GetString(recipe);
        helper.CreateCharacter(recipeString, avatar, character, player);

        helper.SetSlotForHairAndBeard("Hair", character.hairIndex, character.gender, avatar);
        helper.SetSlotForHairAndBeard("Beard", character.beardIndex, character.gender, avatar);
        helper.SetInitialColors(character.hairIndex, character.beardIndex, character.skinColorIndex, character.hairColorIndex,
            character.gender, avatar);

        for (int i = 0; i < character.equipment.Length; ++i) {
            ItemSlot slot = character.equipment[i];
            player.equipment.Add(slot);
            if (slot.amount > 0) {
                // OnEquipmentChanged won't be called unless spawned, we
                // need to refresh manually
                helper.RefreshUMALocation(i, slot, avatar, character.gender);
                Debug.Log("Refreshing For: " + character.name);
            }
        }

        // add selection script
        preview.AddComponent<SelectableCharacter>();
        preview.GetComponent<SelectableCharacter>().index = selectionIndex;
    }

    void OnServerUMACharacterCreate(NetworkMessage netMsg)
    {
        print("OnServerCharacterCreate " + netMsg.conn);
        var message = netMsg.ReadMessage<UMARPGCharacterCreateMsg>();

        // can only delete while in lobby (aka after handshake and not ingame)
        if (lobby.ContainsKey(netMsg.conn))
        {
            // not too long?
            if (message.name.Length <= characterNameMaxLength)
            {
                // only contains letters, number and underscore and not empty (+)?
                // (important for database safety etc.)
                if (Regex.IsMatch(message.name, @"^[a-zA-Z0-9_]+$"))
                {
                    // not existant yet?
                    string account = lobby[netMsg.conn];
                    if (!Database.CharacterExists(message.name))
                    {
                        // not too may characters created yet?
                        if (Database.CharactersForAccount(account).Count < characterLimit)
                        {
                            // valid class index?
                            var classes = GetPlayerClasses();
                            if (0 <= message.classIndex && message.classIndex < classes.Count)
                            {
                                {
                                    // create new character based on the prefab
                                    // (instantiate temporary player)

                                    string className = "";
                                    switch (message.classIndex)
                                    {
                                        case 0: // Warrior
                                                // Add Default Items For Warrior
                                                // No need to do something warrior is default prefab.
                                            className = "Warrior";
                                            break;
                                        case 1: // Archer 
                                            className = "Archer";
                                            // Set Skill Templates For Archer
                                            break;
                                    }

                                    print("creating character: " + message.name + " " + message.classIndex);

                                    GameObject prefabObj = spawnPrefabs.Where(i => i.name == className).FirstOrDefault();
                                    if (prefabObj == null)
                                    {
                                        if (prefabObj.GetComponent<DynamicCharacterAvatar>() == null)
                                        {
                                            print("CharacterCreate: Please Add UMA Specific Prefabs To spawnPrefabs.");
                                            ClientSendPopup(netMsg.conn, "CharacterCreate: Please Add UMA Specific Prefabs To spawnPrefabs.", true);
                                            return;
                                        }

                                        print("CharacterCreate: Couldn't Find Appropiate Prefab in spawnPrefabs.");
                                        ClientSendPopup(netMsg.conn, "CharacterCreate: Couldn't Find Appropiate Prefab in spawnPrefabs.", true);
                                        return;
                                    }

                                    var prefab = GameObject.Instantiate(prefabObj).GetComponent<Player>();

                                    prefab.name = message.name;
                                    prefab.className = classes[message.classIndex].name;
                                    prefab.account = account;
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
                                        prefab.equipment.Add(info.defaultItem != null ? new ItemSlot(new Item(info.defaultItem)) : new ItemSlot());
                                    }

                                    prefab.health = prefab.healthMax;
                                    prefab.mana = prefab.manaMax;
                                    prefab.gender = message.gender;

                                    prefab.umaSyncData.Add(new UmaData() { value = message.hairIndex }); // Hair Index
                                    prefab.umaSyncData.Add(new UmaData() { value = message.beardIndex }); // Beard Index
                                    prefab.umaSyncData.Add(new UmaData() { value = message.skinColorIndex }); // Skin Color
                                    prefab.umaSyncData.Add(new UmaData() { value = message.hairColorIndex }); // Hair Color

                                    byte[] dnaValues = new byte[message.dnaValues.Length];
                                    for(int i = 0; i < dnaValues.Length; i++) {
                                        prefab.dna.Add(new Dna() { value = message.dnaValues[i] });
                                        dnaValues[i] = message.dnaValues[i];
                                    }

                                    // addon system hooks
                                    Utils.InvokeMany(typeof(NetworkManagerMMO), this, "OnServerUMACharacterCreate_", message, prefab);

                                    // save the player
                                    // we'll get back to this later.
                                    Database.CharacterSave(prefab, false);
                                    Destroy(prefab.gameObject);

                                    // send available characters list again, causing
                                    // the client to switch to the character
                                    // selection scene again
                                    CharactersAvailableMsg reply = MakeCharactersAvailableMessage(account);
                                    netMsg.conn.Send(CharactersAvailableMsg.MsgId, reply);
                                }
                            }
                            else
                            {
                                print("character invalid class: " + message.classIndex);
                                ClientSendPopup(netMsg.conn, "character invalid class", false);
                            }
                        }
                        else
                        {
                            print("character limit reached: " + message.name);
                            ClientSendPopup(netMsg.conn, "character limit reached", false);
                        }
                    }
                    else
                    {
                        print("character name already exists: " + message.name);
                        ClientSendPopup(netMsg.conn, "name already exists", false);
                    }
                }
                else
                {
                    print("character name invalid: " + message.name);
                    ClientSendPopup(netMsg.conn, "invalid name", false);
                }
            }
            else
            {
                print("character name too long: " + message.name);
                ClientSendPopup(netMsg.conn, "name too long", false);
            }
        }
        else
        {
            print("CharacterCreate: not in lobby");
            ClientSendPopup(netMsg.conn, "CharacterCreate: not in lobby", true);
        }
    }
}

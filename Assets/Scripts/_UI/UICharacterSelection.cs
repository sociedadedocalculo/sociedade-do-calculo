// Simple character selection list. The charcter prefabs are known, so we could
// easily show 3D models, stats, etc. too.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public partial class UICharacterSelection : MonoBehaviour
{
    public UICharacterCreation uiCharacterCreation;
    public NetworkManagerMMO manager; // singleton is null until update
    public GameObject panel;
    public UICharacterSelectionSlot slotPrefab;
    public Transform content;
    public Button createButton;
    public Button quitButton;

    void Update()
    {
        // show if connected, not in world, not in charcreation
        if (manager.IsClientConnected() && !Utils.ClientLocalPlayer() && !uiCharacterCreation.IsVisible())
        {
            panel.SetActive(true);

            // characters available message received already?
            if (manager.charactersAvailableMsg != null)
            {
                // instantiate/destroy enough slots
                CharactersAvailableMsg.CharacterPreview[] characters = manager.charactersAvailableMsg.characters;
                UIUtils.BalancePrefabs(slotPrefab.gameObject, characters.Length, content);

                // refresh all
                List<Player> prefabs = manager.GetPlayerClasses();
                for (int i = 0; i < characters.Length; ++i)
                {
                    Player prefab = prefabs.Find(p => p.name == characters[i].className);
                    UICharacterSelectionSlot slot = content.GetChild(i).GetComponent<UICharacterSelectionSlot>();

                    // name and icon
                    slot.nameText.text = characters[i].name;
                    slot.image.sprite = prefab.GetComponent<Player>().classIcon;

                    // select button: calls AddPLayer which calls OnServerAddPlayer
                    int icopy = i; // needed for lambdas, otherwise i is Count
                    slot.selectButton.onClick.SetListener(() => {
                        CharacterSelectMsg message = new CharacterSelectMsg { index = icopy };
                        ClientScene.AddPlayer(manager.client.connection, 0, message);

                        // make sure we can't select twice and call AddPlayer twice
                        panel.SetActive(false);
                    });

                    // delete button: sends delete message
                    slot.deleteButton.onClick.SetListener(() => {
                        CharacterDeleteMsg message = new CharacterDeleteMsg { index = icopy };
                        manager.client.Send(CharacterDeleteMsg.MsgId, message);
                    });
                }

                createButton.interactable = characters.Length < manager.characterLimit;
                createButton.onClick.SetListener(() => {
                    panel.SetActive(false);
                    uiCharacterCreation.Show();
                });

                quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });
            }
        }
        else panel.SetActive(false);
    }
}

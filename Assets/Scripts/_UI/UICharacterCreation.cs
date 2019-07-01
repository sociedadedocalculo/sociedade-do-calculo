using UnityEngine;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public partial class UICharacterCreation : MonoBehaviour
{
    public NetworkManagerMMO manager; // singleton is null until update
    public GameObject panel;
    public GameObject poppanel;
    public InputField nameInput;
    public Dropdown classDropdown;
    public Button createButton;
    public Button cancelButton;

    void Update()
    {
        // only update while visible (after character selection made it visible)
        if (panel.activeSelf)
        {
            // still connected, not in world?
            if (manager.IsClientConnected() && !Utils.ClientLocalPlayer())
            {
                Show();

                // copy player classes to class selection
                classDropdown.options = manager.GetPlayerClasses().Select(
                    p => new Dropdown.OptionData(p.name)
                ).ToList();

                // create
                createButton.interactable = manager.IsAllowedCharacterName(nameInput.text);
                createButton.onClick.SetListener(() => {
                    CharacterCreateMsg message = new CharacterCreateMsg
                    {
                        name = nameInput.text,
                        classIndex = classDropdown.value
                    };
                    manager.client.Send(CharacterCreateMsg.MsgId, message);
                    Hide();
                });

                // cancel
                cancelButton.onClick.SetListener(() => {
                    nameInput.text = "";
                    Hide();
                });
            }
            else Hide();
        }
        else Hide();
    }

    public void Hide()
    {
        poppanel.SetActive(false);
        panel.SetActive(false);


    }
    public void Show()
    {
        panel.SetActive(true);

    }
    public bool IsVisible() { return panel.activeSelf; }
}

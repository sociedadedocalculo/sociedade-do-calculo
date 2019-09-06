using Mirror;
using UnityEngine;
using UnityEngine.UI;

public partial class UIShortcuts : MonoBehaviour
{
    public GameObject panel;

    public Button inventoryButton;
    public GameObject inventoryPanel;

    public Button equipmentButton;
    public GameObject equipmentPanel;

    public Button skillsButton;
    public GameObject skillsPanel;

    public Button characterInfoButton;
    public GameObject characterInfoPanel;

    public Button questsButton;
    public GameObject questsPanel;

    public Button craftingButton;
    public GameObject craftingPanel;

    public Button guildButton;
    public GameObject guildPanel;

    public Button partyButton;
    public GameObject partyPanel;

    public Button itemMallButton;
    public GameObject itemMallPanel;

    public Button quitButton;

    void Update()
    {
        Player player = Player.localPlayer;
        if (player != null)
        {
            panel.SetActive(true);

            inventoryButton.onClick.SetListener(() => {
                inventoryPanel.SetActive(!inventoryPanel.activeSelf);
            });

            equipmentButton.onClick.SetListener(() => {
                equipmentPanel.SetActive(!equipmentPanel.activeSelf);
            });

            skillsButton.onClick.SetListener(() => {
                skillsPanel.SetActive(!skillsPanel.activeSelf);
            });

            characterInfoButton.onClick.SetListener(() => {
                characterInfoPanel.SetActive(!characterInfoPanel.activeSelf);
            });

            questsButton.onClick.SetListener(() => {
                questsPanel.SetActive(!questsPanel.activeSelf);
            });

            craftingButton.onClick.SetListener(() => {
                craftingPanel.SetActive(!craftingPanel.activeSelf);
            });

            partyButton.onClick.SetListener(() => {
                partyPanel.SetActive(!partyPanel.activeSelf);
            });

            guildButton.onClick.SetListener(() => {
                guildPanel.SetActive(!guildPanel.activeSelf);
            });

            itemMallButton.onClick.SetListener(() => {
                itemMallPanel.SetActive(!itemMallPanel.activeSelf);
            });

            // show "(5)Quit" if we can't log out during combat
            // -> CeilToInt so that 0.1 shows as '1' and not as '0'
            string quitPrefix = "";
            if (player.remainingLogoutTime > 0)
                quitPrefix = "(" + Mathf.CeilToInt((float)player.remainingLogoutTime) + ") ";
            quitButton.GetComponent<UIShowToolTip>().text = quitPrefix + "Quit";
            quitButton.interactable = player.remainingLogoutTime == 0;
            quitButton.onClick.SetListener(() => {
                NetworkManagerMMO.Quit();
            });
        }
        else panel.SetActive(false);
    }
}

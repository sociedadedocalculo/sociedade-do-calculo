// Note: this script has to be on an always-active UI parent, so that we can
// always find it from other code. (GameObject.Find doesn't find inactive ones)
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public partial class UILogin : MonoBehaviour
{
    public UIPopup uiPopup;
    public UIPopupMessage uiPopupMessage;
    public NetworkManagerMMO manager; 
    public GameObject panel;
    public Text statusText;
    public InputField accountInput;
    public InputField passwordInput;
    public Dropdown serverDropdown;
    public Button loginButton;
    public Button registerButton;
    [TextArea(1, 30)] public string registerMessage = "Primeira vez? Tente criar o seu cadastro para logar-se...";
    public Button hostButton;
    public Button dedicatedButton;
    public Button cancelButton;
    public Button quitButton;



    void Start()
    {
        // load last server by name in case order changes some day.
        if (PlayerPrefs.HasKey("LastServer"))
        {
            string last = PlayerPrefs.GetString("LastServer", "");
            serverDropdown.value = manager.serverList.FindIndex(s => s.name == last);
        }
    }

    void OnDestroy()
    {
        // save last server by name in case order changes some day
        PlayerPrefs.SetString("LastServer", serverDropdown.captionText.text);
    }

    void Update()
    {
        // only show while offline or trying to connect
        if (!manager.IsClientConnected())
        {
            panel.SetActive(true);

            // status
            statusText.text = manager.IsConnecting() ? "Conectando..." : "";

            // buttons. interactable while network is not active
            // (using IsConnecting is slightly delayed and would allow multiple clicks)
            registerButton.onClick.SetListener(() => { uiPopup.Show(registerMessage); });
            loginButton.interactable = !manager.isNetworkActive && manager.IsAllowedAccountName(accountInput.text);
            loginButton.onClick.SetListener(() => { manager.StartClient(); });
            hostButton.interactable = !manager.isNetworkActive && manager.IsAllowedAccountName(accountInput.text);
            hostButton.onClick.SetListener(() => { manager.StartHost(); });
            cancelButton.gameObject.SetActive(manager.IsConnecting());
            cancelButton.onClick.SetListener(() => { manager.StopClient(); });
            dedicatedButton.interactable = !manager.isNetworkActive;
            dedicatedButton.onClick.SetListener(() => { manager.StartServer(); });
            quitButton.onClick.SetListener(() => { NetworkManagerMMO.Quit(); });

            // inputs
            manager.loginAccount = accountInput.text;
            manager.loginPassword = passwordInput.text;

            // copy servers to dropdown; copy selected one to networkmanager ip/port.
            serverDropdown.interactable = !manager.isNetworkActive;
            serverDropdown.options = manager.serverList.Select(
                sv => new Dropdown.OptionData(sv.name)
            ).ToList();
            manager.networkAddress = manager.serverList[serverDropdown.value].ip;
        }
        else panel.SetActive(false);

    }

}

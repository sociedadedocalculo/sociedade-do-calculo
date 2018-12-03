using UnityEngine;
using UnityEngine.UI;

public partial class UIPortrait : MonoBehaviour
{
    public GameObject panel;
    public Image image;

    void Update()
    {
        Player player = Player.localPlayer;
        panel.SetActive(player != null); // hide while not in the game world
        if (!player) return;

        image.sprite = player.portraitIcon;
    }
}

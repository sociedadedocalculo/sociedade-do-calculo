// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using UnityEngine;
using UnityEngine.UI;

public class UIGuildMemberSlot : MonoBehaviour
{
    public Image onlineStatusImage;
    public Text nameText;
    public Text levelText;
    public Text rankText;
    public Button promoteButton;
    public Button demoteButton;
    public Button kickButton;
}

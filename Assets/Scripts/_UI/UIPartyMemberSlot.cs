// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using UnityEngine;
using UnityEngine.UI;

public class UIPartyMemberSlot : MonoBehaviour
{
    public Image icon;
    public Text nameText;
    public Text masterIndicatorText;
    public Text levelText;
    public Text guildText;
    public Button actionButton;
    public Slider healthSlider;
    public Slider manaSlider;
}

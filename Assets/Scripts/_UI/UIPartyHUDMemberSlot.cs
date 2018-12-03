// Attach to the prefab for easier component access by the UI Scripts.
// Otherwise we would need slot.GetChild(0).GetComponentInChildren<Text> etc.
using UnityEngine;
using UnityEngine.UI;

public class UIPartyHUDMemberSlot : MonoBehaviour
{
    public Button backgroundButton;
    public Image icon;
    public Text nameText;
    public Text masterIndicatorText;
    public Slider healthSlider;
    public Image healthDistanceImage;
    public Slider manaSlider;
    public Image manaDistanceImage;
}

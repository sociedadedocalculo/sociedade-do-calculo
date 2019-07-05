using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UMA;
using UMA.CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterCreationUMA : MonoBehaviour
{
    // uMMORPG Stuff.
    public NetworkManagerMMO manager; // singleton is null until update
    public GameObject panel;
    public InputField nameInput;
    public Dropdown classDropdown;
    public Button createButton;
    public Button cancelButton;

    // UMA Stuff
    public Dropdown genderDropdown;
    public Dropdown dnaDropdown;
    public Dropdown hairDropdown;
    public Dropdown beardDropdown;
    public Dropdown hairColorDropdown;
    public Dropdown skinColorDropdown;
    public Slider dnaSlider;
    public GameObject beardPanel;

    [SerializeField, Header("uMARPG")]
    public UMAWardrobeRecipe[] maleHairs;
    [SerializeField]
    public UMAWardrobeRecipe[] maleBeards;
    [SerializeField]
    public UMAWardrobeRecipe[] femaleHairs;
    [SerializeField]
    public SharedColorTable hairColors;
    [SerializeField]
    public SharedColorTable skinColors;

    // Private fields.
    Camera mainCamera;
    Camera characterCreationCamera;

    // Get the gameObject so we can do something.
    GameObject umaObject;
    // Avatar 
    DynamicCharacterAvatar uma;
    // Current Dna Settings.
    Dictionary<string, DnaSetter> umaDna;

    void Start()
    {
        // Get Main Camera
        mainCamera = Camera.main;
        characterCreationCamera = GameObject.Find("CharacterCreationCamera").GetComponent<Camera>();
        umaObject = GameObject.Find("UMARPG Player");
        uma = umaObject.GetComponent<DynamicCharacterAvatar>();
        uma.ChangeRace("HumanMaleDCS");

        PopulateMaleDropdown();
        PopulateHairColorDropDown();
        PopulateSkinColorDropDown();

        // Switch Genders For UMA.
        genderDropdown.onValueChanged.SetListener((evnt) =>
        {
            switch (genderDropdown.value)
            {
                case 0: // Male
                    uma.ChangeRace("HumanMaleDCS", DynamicCharacterAvatar.ChangeRaceOptions.keepDNA);
                    PopulateMaleDropdown();
                    break;
                case 1: // Female
                    uma.ChangeRace("HumanFemaleDCS", DynamicCharacterAvatar.ChangeRaceOptions.keepDNA);
                    PopulateFemaleDropdown();
                    break;
            }
        });

        // Hair Color Changed
        hairColorDropdown.onValueChanged.SetListener((evnt) =>
        {
            uma.SetColor("Hair", hairColors.colors[hairColorDropdown.value]);
        });

        // Skin Color Changed
        skinColorDropdown.onValueChanged.SetListener((evnt) =>
        {
            uma.SetColor("Skin", skinColors.colors[skinColorDropdown.value]);
        });

        // Hair changed
        hairDropdown.onValueChanged.SetListener((evnt) =>
        {
            UMAWardrobeRecipe recipe = null;
            // Determine if male or female hair.
            switch (genderDropdown.value)
            {
                case 0: // Male
                    recipe = maleHairs[hairDropdown.value];
                    break;
                case 1: // Female
                    recipe = femaleHairs[hairDropdown.value];
                    break;
            }

            uma.ClearSlot(recipe.wardrobeSlot);
            uma.SetSlot(recipe);
            uma.BuildCharacter();
        });

        // Beard Changed
        beardDropdown.onValueChanged.SetListener((evnt) =>
        {
            // No need to check if male or female,
            // Females don't have beards. hah!
            UMAWardrobeRecipe recipe = maleBeards[beardDropdown.value];
            uma.ClearSlot(recipe.wardrobeSlot);
            uma.SetSlot(recipe);
            uma.BuildCharacter();
        });

        // Set current dna value to build character.
        dnaSlider.onValueChanged.SetListener((evnt) =>
        {
            umaDna[dnaDropdown.captionText.text].Set(dnaSlider.value);
            uma.BuildCharacter();
            uma.ForceUpdate(true, false, false);
        });

        // Set the dna value to original so it wont get lost
        dnaDropdown.onValueChanged.SetListener((evnt) =>
        {
            dnaSlider.value = umaDna[dnaDropdown.captionText.text].Value;
        });

        // Hook DNA Update 
        uma.CharacterDnaUpdated.SetListener((data) =>
        {
            umaDna = uma.GetDNA();

            // update the list if it have no elements
            // or different elements than before.
            if (dnaDropdown.options.Count == 0 || dnaDropdown.options.Count != umaDna.Count)
            {
                List<string> options = new List<string>();
                foreach (var val in umaDna)
                    options.Add(val.Key);

                dnaDropdown.AddOptions(options);
            }
            else // set sliders float value.
            {
                dnaSlider.value = umaDna[dnaDropdown.captionText.text].Value;
            }
        });
    }

    // Populate the hair color drop down
    void PopulateHairColorDropDown()
    {
        List<string> options = new List<string>();
        foreach (OverlayColorData data in hairColors.colors)
            options.Add(data.name);

        hairColorDropdown.AddOptions(options);
    }

    // Populate the skin color drop down
    void PopulateSkinColorDropDown()
    {
        List<string> options = new List<string>();
        foreach (OverlayColorData data in skinColors.colors)
            options.Add(data.name);

        skinColorDropdown.AddOptions(options);
    }

    // Populate male dropdowns for hair and beards
    void PopulateMaleDropdown()
    {
        beardPanel.SetActive(true);
        hairDropdown.ClearOptions();
        beardDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (UMAWardrobeRecipe recipe in maleHairs)
            options.Add(recipe.name);

        hairDropdown.AddOptions(options);

        options = new List<string>();

        foreach (UMAWardrobeRecipe recipe in maleBeards)
            options.Add(recipe.name);

        beardDropdown.AddOptions(options);
    }

    // Populate female dropdowns for hair
    void PopulateFemaleDropdown()
    {
        beardPanel.SetActive(false);
        hairDropdown.ClearOptions();
        beardDropdown.ClearOptions();

        List<string> options = new List<string>();

        foreach (UMAWardrobeRecipe recipe in femaleHairs)
            options.Add(recipe.name);

        hairDropdown.AddOptions(options);
    }

    void Update()
    {
        // only update while visible (after character selection made it visible)
        if (panel.activeSelf)
        {
            // still connected, not in world?
            if (manager.IsClientConnected() && !Player.localPlayer)
            {
                Show();

                // copy player classes to class selection
                classDropdown.options = manager.GetPlayerClasses().Select(
                    p => new Dropdown.OptionData(p.name)
                ).ToList();

                // Check if main camera is active and enabled
                // Disable the main camera, get the character creation camera and enable it.
                // Give it a tag for Main so we don't brake things? // necessary?
                if (mainCamera != null && mainCamera.isActiveAndEnabled)
                {
                    mainCamera.enabled = false;
                    characterCreationCamera.enabled = true;
                    characterCreationCamera.tag = "MainCamera";
                }

                createButton.onClick.RemoveAllListeners();
                createButton.interactable = manager.IsAllowedCharacterName(nameInput.text);
                createButton.onClick.SetListener(() =>
                {
                    UMATextRecipe asset = ScriptableObject.CreateInstance<UMATextRecipe>();
                    asset.Save(uma.umaData.umaRecipe, uma.context);
                    string umaString = asset.recipeString;

                    var message = new UMARPGCharacterCreateMsg
                    {
                        name = nameInput.text,
                        classIndex = classDropdown.value,
                        dnaValues = System.Text.Encoding.UTF8.GetBytes(umaString),
                        gender = genderDropdown.value == 0, // 0 = Male, 1 = Female
                        beardIndex = (byte)genderDropdown.value, // Do not need the beardIndex for female
                        hairIndex = (byte)hairDropdown.value,
                        hairColorIndex = (byte)hairColorDropdown.value,
                        skinColorIndex = (byte)skinColorDropdown.value
                    };

                    manager.client.Send(UMARPGCharacterCreateMsg.MsgId, message);
                    mainCamera.enabled = true;
                    mainCamera.tag = "MainCamera";

                    characterCreationCamera.enabled = false;
                    characterCreationCamera.tag = "Untagged";
                    Hide();
                });

                // cancel
                cancelButton.onClick.SetListener(() =>
                {
                    nameInput.text = "";
                    Hide();
                });
            }
            else Hide();
        }
        else Hide();
    }

    public void Hide() { panel.SetActive(false); }
    public void Show() { panel.SetActive(true); }
    public bool IsVisible() { return panel.activeSelf; }
}

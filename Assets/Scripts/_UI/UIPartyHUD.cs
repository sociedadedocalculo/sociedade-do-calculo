using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public partial class UIPartyHUD : MonoBehaviour
{
    public GameObject panel;
    public UIPartyHUDMemberSlot slotPrefab;
    public Transform memberContent;
    //[Range(0,1)] public float visiblityAlphaRange = 0.5f;
    public AnimationCurve alphaCurve;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // only show and update while there are party members
        if (player.InParty())
        {
            panel.SetActive(true);
            Party party = player.party;

            // get party members without self. no need to show self in HUD too.
            List<string> members = player.InParty() ? party.members.Where(m => m != player.name).ToList() : new List<string>();

            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, members.Count, memberContent);

            // refresh all members
            for (int i = 0; i < members.Count; ++i)
            {
                UIPartyHUDMemberSlot slot = memberContent.GetChild(i).GetComponent<UIPartyHUDMemberSlot>();
                string memberName = members[i];
                float distance = Mathf.Infinity;
                float visRange = player.proxchecker.visRange;

                slot.nameText.text = memberName;
                slot.masterIndicatorText.gameObject.SetActive(party.GetMemberIndex(memberName) == 0);

                // pull health, mana, etc. from observers so that party struct
                // doesn't have to send all that data around. people will only
                // see health of party members that are near them, which is the
                // only time that it's important anyway.
                if (Player.onlinePlayers.ContainsKey(memberName))
                {
                    Player member = Player.onlinePlayers[memberName];
                    slot.icon.sprite = member.classIcon;
                    slot.healthSlider.value = member.HealthPercent();
                    slot.manaSlider.value = member.ManaPercent();
                    slot.backgroundButton.onClick.SetListener(() => {
                        player.CmdSetTarget(member.netIdentity);
                    });

                    // distance color based on visRange ratio
                    distance = Vector3.Distance(player.transform.position, member.transform.position);
                    visRange = member.proxchecker.visRange; // visRange is always based on the other guy
                }

                // distance overlay alpha based on visRange ratio
                // (because values are only up to date for members in observer
                //  range)
                float ratio = visRange > 0 ? distance / visRange : 1f;
                float alpha = alphaCurve.Evaluate(ratio);

                // icon alpha
                Color iconColor = slot.icon.color;
                iconColor.a = alpha;
                slot.icon.color = iconColor;

                // health bar alpha
                foreach (Image image in slot.healthSlider.GetComponentsInChildren<Image>())
                {
                    Color color = image.color;
                    color.a = alpha;
                    image.color = color;
                }

                // mana bar alpha
                foreach (Image image in slot.manaSlider.GetComponentsInChildren<Image>())
                {
                    Color color = image.color;
                    color.a = alpha;
                    image.color = color;
                }
            }
        }
        else panel.SetActive(false);
    }
}

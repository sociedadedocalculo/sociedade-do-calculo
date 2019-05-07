using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Mirror;

public partial class UIGuild : MonoBehaviour
{
    public KeyCode hotKey = KeyCode.G;
    public GameObject panel;
    public Text nameText;
    public Text masterText;
    public Text currentCapacityText;
    public Text maximumCapacityText;
    public InputField noticeInput;
    public Button noticeEditButton;
    public Button noticeSetButton;
    public UIGuildMemberSlot slotPrefab;
    public Transform memberContent;
    public Color onlineColor = Color.cyan;
    public Color offlineColor = Color.gray;
    public Button leaveButton;

    void Update()
    {
        Player player = Utils.ClientLocalPlayer();
        if (!player) return;

        // hotkey (not while typing in chat, etc.)
        if (Input.GetKeyDown(hotKey) && !UIUtils.AnyInputActive())
            panel.SetActive(!panel.activeSelf);

        // only update the panel if it's active
        if (panel.activeSelf)
        {
            Guild guild = player.guild;
            int memberCount = guild.members != null ? guild.members.Length : 0;

            // guild properties
            nameText.text = player.guildName;
            masterText.text = guild.MasterName();
            currentCapacityText.text = memberCount.ToString();
            maximumCapacityText.text = Guild.Capacity.ToString();

            // notice edit button
            noticeEditButton.interactable = guild.CanNotify(player.name) &&
                                            !noticeInput.interactable;
            noticeEditButton.onClick.SetListener(() => {
                noticeInput.interactable = true;
            });

            // notice set button
            noticeSetButton.interactable = guild.CanNotify(player.name) &&
                                           noticeInput.interactable &&
                                           NetworkTime.time >= player.nextRiskyActionTime;
            noticeSetButton.onClick.SetListener(() => {
                noticeInput.interactable = false;
                if (noticeInput.text.Length > 0 &&
                    !Utils.IsNullOrWhiteSpace(noticeInput.text) &&
                    noticeInput.text != guild.notice) {
                    player.CmdSetGuildNotice(noticeInput.text);
                }
            });

            // notice input: copies notice while not editing it
            if (!noticeInput.interactable) noticeInput.text = guild.notice ?? "";
            noticeInput.characterLimit = Guild.NoticeMaxLength;

            // leave
            leaveButton.interactable = guild.CanLeave(player.name);
            leaveButton.onClick.SetListener(() => {
                player.CmdLeaveGuild();
            });

            // instantiate/destroy enough slots
            UIUtils.BalancePrefabs(slotPrefab.gameObject, memberCount, memberContent);

            // refresh all members
            for (int i = 0; i < memberCount; ++i)
            {
                UIGuildMemberSlot slot = memberContent.GetChild(i).GetComponent<UIGuildMemberSlot>();
                GuildMember member = guild.members[i];

                slot.onlineStatusImage.color = member.online ? onlineColor : offlineColor;
                slot.nameText.text = member.name;
                slot.levelText.text = member.level.ToString();
                slot.rankText.text = member.rank.ToString();
                slot.promoteButton.interactable = guild.CanPromote(player.name, member.name);
                slot.promoteButton.onClick.SetListener(() => {
                    player.CmdGuildPromote(member.name);
                });
                slot.demoteButton.interactable = guild.CanDemote(player.name, member.name);
                slot.demoteButton.onClick.SetListener(() => {
                    player.CmdGuildDemote(member.name);
                });
                slot.kickButton.interactable = guild.CanKick(player.name, member.name);
                slot.kickButton.onClick.SetListener(() => {
                    player.CmdGuildKick(member.name);
                });
            }
        }
    }
}

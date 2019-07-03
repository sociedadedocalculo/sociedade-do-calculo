// a regular portal that teleports the player from A to B.
using UnityEngine;
using Mirror;

[RequireComponent(typeof(Collider))]
public class Portal : MonoBehaviour
{
    public int requiredLevel = 1;
    public Transform destination;

    void OnPortal(Player player)
    {
        if (destination != null)
            player.agent.Warp(destination.position);
    }

    void OnTriggerEnter(Collider co)
    {
        // collider might be in player's bone structure. look in parents.
        Player player = co.GetComponentInParent<Player>();
        if (player != null)
        {
            // required level?
            if (player.level >= requiredLevel)
            {
                // server? then enter the portal
                if (player.isServer)
                    OnPortal(player);
            }
            else
            {
                // client? then show info message directly. no need to send it
                // from the server to the client via TargetRpc.
                if (player.isClient)
                    player.chat.AddMsgInfo("Portal requires level " + requiredLevel);
            }
        }
    }
}

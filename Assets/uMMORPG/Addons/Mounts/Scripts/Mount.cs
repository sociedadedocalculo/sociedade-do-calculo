// The Monster class has a few different features that all aim to make monsters
// behave as realistically as possible.
//
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(Animator))]
public class Mount : NetworkBehaviour
{
	[Header("Mount Stuff")]
	[HideInInspector]
	public Entity owner;
	[HideInInspector]
	public bool dead;
	[SerializeField]
	public Animator animator;
	[SerializeField]
	public Collider collision;
	[SerializeField]
	public GameObject seat;

	void LateUpdate()
	{
		if (isClient && owner)
		{ // no need for animations on the server
			animator.SetBool("MOVING",!dead && owner.state == "MOVING" && owner.agent.velocity != Vector3.zero);
            animator.SetBool("DEAD", dead || owner.state == "DEAD");
		}

		// addon system hooks
		Utils.InvokeMany(typeof(Mount), this, "LateUpdate_");
	}

}

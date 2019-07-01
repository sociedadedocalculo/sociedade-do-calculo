using UnityEngine;
using Mirror;

public class OneTimeTargetSkillEffect : SkillEffect
{
    void Update()
    {
        // follow the target's position (because we can't make a NetworkIdentity
        // a child of another NetworkIdentity)
        if (target != null)
            transform.position = target.collider.bounds.center;

        // destroy self if target disappeared or particle ended
        if (isServer)
            if (target == null || !GetComponent<ParticleSystem>().IsAlive())
                NetworkServer.Destroy(gameObject);
    }
}

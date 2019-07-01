// Projectile skill effects like arrows, flaming fire balls, etc. that deal
// damage on the target.
//
// Note: we could move it on the server and use NetworkTransform to synchronize
// the position to all clients, which is the easy method. But we just move it on
// the server and the on the client to save bandwidth. Same result.
using UnityEngine;
using Mirror;

public class ProjectileSkillEffect : SkillEffect
{
    public float speed = 1;
    [HideInInspector] public int damage = 1; // set by skill

    // update here already so that it doesn't spawn with a weird rotation
    void Start() { FixedUpdate(); }

    // fixedupdate on client and server to simulate the same effect without
    // using a NetworkTransform
    void FixedUpdate()
    {
        // target and caster still around?
        // note: we keep flying towards it even if it died already, because
        //       it looks weird if fireballs would be canceled inbetween.
        if (target != null && caster != null)
        {
            // move closer and look at the target
            Vector3 goal = target.collider.bounds.center;
            transform.position = Vector3.MoveTowards(transform.position, goal, speed);
            transform.LookAt(goal);

            // server: reached it? apply skill and destroy self
            if (isServer && transform.position == goal)
            {
                if (target.health > 0)
                {
                    // find the skill that we casted this effect with
                    caster.DealDamageAt(target, caster.damage + damage);
                }
                NetworkServer.Destroy(gameObject);
            }
        }
        else if (isServer) NetworkServer.Destroy(gameObject);
    }
}

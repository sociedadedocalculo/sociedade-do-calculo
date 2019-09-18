// Projectile skill effects like arrows, flaming fire balls, etc. that deal
// damage on the target.
//
// Note: we could move it on the server and use NetworkTransform to synchronize
// the position to all clients, which is the easy method. But we just move it on
// the server and the on the client to save bandwidth. Same result.
using UnityEngine;
using UnityEngine.Events;
using Mirror;

public class ProjectileSkillEffect : SkillEffect
{
    public float speed = 35;
    [HideInInspector] public int damage = 1; // set by skill
    [HideInInspector] public float stunChance; // set by skill
    [HideInInspector] public float stunTime; // set by skill

    // effects like a trail or particles need to have their initial positions
    // corrected too. simply connect their .Clear() functions to the event.
    public UnityEvent onSetInitialPosition;

    public override void OnStartClient()
    {
        SetInitialPosition();
    }

    void SetInitialPosition()
    {
        // the projectile should always start at the effectMount position.
        // -> server doesn't run animations, so it will never spawn it exactly
        //    where the effectMount is on the client by the time the packet
        //    reaches the client.
        // -> the best solution is to correct it here once
        if (target != null && caster != null)
        {
            transform.position = caster.effectMount.position;
            transform.LookAt(target.collider.bounds.center);
            onSetInitialPosition.Invoke();
        }
    }

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
            transform.position = Vector3.MoveTowards(transform.position, goal, speed * Time.fixedDeltaTime);
            transform.LookAt(goal);

            // server: reached it? apply skill and destroy self
            if (isServer && transform.position == goal)
            {
                if (target.health > 0)
                {
                    // find the skill that we casted this effect with
                    caster.DealDamageAt(target, caster.damage + damage, stunChance, stunTime);
                }
                NetworkServer.Destroy(gameObject);
            }
        }
        else if (isServer) NetworkServer.Destroy(gameObject);
    }
}

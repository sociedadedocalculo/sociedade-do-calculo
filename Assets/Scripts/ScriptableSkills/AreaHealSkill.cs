// Group heal that heals all entities of same type in cast range
// => player heals players in cast range
// => monster heals monsters in cast range
using System.Text;
using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName="uMMORPG Skill/Area Heal", order=999)]
public class AreaHealSkill : HealSkillTemplate
{
    public override bool CheckTarget(Entity caster)
    {
        // no target necessary, but still set to self so that LookAt(target)
        // doesn't cause the player to look at a target that doesn't even matter
        caster.target = caster;
        return true;
    }

    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        // can cast anywhere
        destination = caster.transform.position;
        return true;
    }

    public override void Apply(Entity caster, int skillLevel)
    {
        // find all entities of same type in castRange around the caster
        Collider[] colliders = Physics.OverlapSphere(caster.transform.position, castRange.Get(skillLevel));
        foreach (Collider co in colliders)
        {
            Entity candidate = co.GetComponentInParent<Entity>();
            if (candidate != null && candidate.GetType() == caster.GetType())
            {
                // can't heal dead people
                if (candidate.health > 0)
                {
                    candidate.health += healsHealth.Get(skillLevel);
                    candidate.mana += healsMana.Get(skillLevel);

                    // show effect on candidate
                    SpawnEffect(caster, candidate);
                }
            }
        }
    }
}

using System.Text;
using UnityEngine;
using Mirror;

[CreateAssetMenu(menuName = "uMMORPG Skill/Target Damage", order = 999)]
public class TargetDamageSkill : ScriptableSkill
{
    public LevelBasedInt damage = new LevelBasedInt { baseValue = 1 };

    public override bool CheckTarget(Entity caster)
    {
        // target exists, alive, not self, oktype?
        return caster.target != null && caster.CanAttack(caster.target);
    }

    public override bool CheckDistance(Entity caster, int skillLevel, out Vector3 destination)
    {
        // target still around?
        if (caster.target != null)
        {
            destination = caster.target.collider.ClosestPointOnBounds(caster.transform.position);
            return Utils.ClosestDistance(caster.collider, caster.target.collider) <= castRange.Get(skillLevel);
        }
        destination = caster.transform.position;
        return false;
    }

    public override void Apply(Entity caster, int skillLevel)
    {
        // deal damage directly with base damage + skill damage
        caster.DealDamageAt(caster.target, caster.damage + damage.Get(skillLevel));
    }

    // tooltip /////////////////////////////////////////////////////////////////
    public override string ToolTip(int skillLevel, bool showRequirements = false)
    {
        StringBuilder tip = new StringBuilder(base.ToolTip(skillLevel, showRequirements));
        tip.Replace("{DAMAGE}", damage.Get(skillLevel).ToString());
        return tip.ToString();
    }
}

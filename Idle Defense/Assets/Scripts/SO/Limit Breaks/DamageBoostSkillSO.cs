using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/DamageBoost")]
public class DamageBoostSkillSO : LimitBreakSkillSO
{
    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateDamageBoost(Magnitude, Duration, this);
    }
}

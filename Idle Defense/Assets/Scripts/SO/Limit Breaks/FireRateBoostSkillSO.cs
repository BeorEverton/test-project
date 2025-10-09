using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/FireRateBoost")]
public class FireRateBoostSkillSO : LimitBreakSkillSO
{
    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateFireRateBoost(Magnitude, Duration, this);
    }

}

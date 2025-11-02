using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Survivor's Roar")]
public class SurvivorsRoarSkillSO : LimitBreakSkillSO
{
    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateSurvivorsRoar(Magnitude, Duration, this);
    }
}

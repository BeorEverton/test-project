using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Anima Infusion")]
public class AnimaInfusionSkillSO : LimitBreakSkillSO
{
    [Header("Anima Infusion")]
    public float FireRateMultiplier = 1.5f;
    public float HealPct = 6f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateAnimaInfusion(FireRateMultiplier, Duration, HealPct);
    }
}

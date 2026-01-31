using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Abyssal Hymn")]
public class AbyssalHymnSkillSO : LimitBreakSkillSO
{
    [Header("Abyssal Hymn")]
    public float HealPct = 8f;

    [Tooltip("Adds this % of each target's LimitBreakMax to their current charge.")]
    public float ChargePctOfTargetMax = 25f;

    public bool IncludeCaster = false;

    public override void Activate(LimitBreakContext ctx)
    {
        if (ctx.Runtime == null) return;

        LimitBreakManager.Instance?.ActivateAbyssalHymn(
            ctx.GunnerId,
            HealPct,
            ChargePctOfTargetMax,
            IncludeCaster
        );
    }
}

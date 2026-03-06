using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Heat Management")]
public class HeatManagementSkillSO : LimitBreakSkillSO
{
    [Header("Heat Management")]
    [Tooltip("Heals the activating gunner over time. Percent of Max HP per second.")]
    [Range(0f, 50f)] public float RegenPctPerSecond = 5f;

    [Tooltip("Reduces incoming damage for the activating gunner only.")]
    [Range(0f, 95f)] public float SelfDefenseReductionPct = 35f;

    [Tooltip("Reduces outgoing damage for the activating gunner only (multiplier = 1 - pct/100).")]
    [Range(0f, 95f)] public float SelfDamagePenaltyPct = 25f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateHeatManagement(
            ctx.GunnerId,
            RegenPctPerSecond,
            SelfDefenseReductionPct,
            SelfDamagePenaltyPct,
            Duration,
            this
        );
    }
}

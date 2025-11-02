using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Heat Management")]
public class HeatManagementSkillSO : LimitBreakSkillSO
{
    [Header("Heat Management")]
    public float ExtraHP = 25f;                // temporary overhealth
    [Range(0, 100)] public float DamagePenaltyPct = 20f; // outgoing damage penalty

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateHeatManagement(ctx.GunnerId, ExtraHP, DamagePenaltyPct, Duration);
    }
}

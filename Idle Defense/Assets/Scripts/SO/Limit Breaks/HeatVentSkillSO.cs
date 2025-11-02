using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Heat Vent")]
public class HeatVentSkillSO : LimitBreakSkillSO
{
    [Header("Heat Vent")]
    public float Radius = 6f;
    public float KnockTime = 0.35f;
    public float KnockSpeed = 10f;
    public float HealPct = 8f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateHeatVent(Radius, KnockTime, KnockSpeed, HealPct);
    }
}

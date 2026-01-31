using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Pressure Sync")]
public class PressureSyncSkillSO : LimitBreakSkillSO
{
    [Header("Pressure Sync")]
    [Tooltip("Heal equipped gunners by % of their max HP (same behavior as Heat Vent).")]
    public float HealPct = 10f;

    public override void Activate(LimitBreakContext ctx)
    {
        // One call into the manager, consistent with the other skills.
        LimitBreakManager.Instance?.ActivatePressureSync(Magnitude, Duration, HealPct, this);
    }
}

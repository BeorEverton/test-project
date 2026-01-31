using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Polen Pulse", fileName = "PolenPulseSkillSO")]
public class PolenPulseSkillSO : LimitBreakSkillSO
{
    [Header("Timing")]
    public float Duration = 6f;
    public float TickInterval = 1f;

    [Header("Area")]
    public float RadiusWorld = 6f;

    [Header("Turret Range Boost (percent)")]
    public float MinRangeBoostPct = 10f;
    public float MaxRangeBoostPct = 40f;

    [Header("Enemy Damage per Tick (% of gunner damage)")]
    public float MinDamagePctOfGunnerDamage = 20f;
    public float MaxDamagePctOfGunnerDamage = 60f;

    [Header("Click Scaling")]
    public float ClickGain = 10f; // how much "charge" per click (0..100)
    public float MaxClickCharge = 100f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivatePolenPulse(
            ctx.GunnerId,
            Duration,
            TickInterval,
            RadiusWorld,
            MinRangeBoostPct,
            MaxRangeBoostPct,
            MinDamagePctOfGunnerDamage,
            MaxDamagePctOfGunnerDamage,
            ClickGain,
            MaxClickCharge,
            this
        );
    }
}

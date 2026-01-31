using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Sandstorm Collapse")]
public class SandstormCollapseSkillSO : LimitBreakSkillSO
{
    [Header("Sandstorm Shape")]
    public float StartRadius = 2.5f;
    public float EndRadius = 10f;

    [Header("Damage (DPS)")]
    [Tooltip("If > 0, DPS becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDPS.")]
    public float DpsPctOfGunnerDamage = 0f;

    public float FlatDPS = 50f;
    public float MinDPS = 0f;
    public float MaxDPS = 999999f;

    [Header("Armor")]
    [Range(0, 100)] public float ArmorPenetrationPct = 0f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dps = lbm.ResolvePower(ctx, FlatDPS, DpsPctOfGunnerDamage, MinDPS, MaxDPS);

        lbm.ActivateSandstormCollapse(
            ctx.GunnerId,
            dps,
            StartRadius,
            EndRadius,
            Duration,
            ArmorPenetrationPct,
            this
        );
    }
}

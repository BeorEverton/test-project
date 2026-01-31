using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Frost Infusion")]
public class FrostInfusionSkillSO : LimitBreakSkillSO
{
    [Header("Needle Wave Shape")]
    public float Length = 12f;
    public float Width = 1.2f;

    [Header("Slow")]
    [Range(0, 100)] public float SlowPct = 40f;
    public float SlowSeconds = 3f;

    [Header("Damage Scaling")]
    [Tooltip("If > 0, Damage becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDamage.")]
    public float DamagePctOfGunnerDamage = 200f; // "massive damage" default
    public float FlatDamage = 100f;
    public float MinDamage = 0f;
    public float MaxDamage = 999999f;

    [Header("Armor")]
    [Range(0, 100)] public float ArmorPenetrationPct = 0f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dmg = lbm.ResolvePower(ctx, FlatDamage, DamagePctOfGunnerDamage, MinDamage, MaxDamage);

        lbm.ActivateFrostInfusion(
            ctx.GunnerId,
            dmg,
            Length,
            Width,
            SlowPct,
            SlowSeconds,
            ArmorPenetrationPct,
            this
        );
    }
}

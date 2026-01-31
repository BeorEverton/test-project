using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Cold Execution")]
public class ColdExecutionSkillSO : LimitBreakSkillSO
{
    [Header("Cone")]
    public float Range = 14f;
    public float AngleDegrees = 60f;

    [Header("Damage")]
    [Tooltip("Damage = EffectiveGunnerDamage * DamageMultiplier")]
    public float DamageMultiplier = 4f;

    [Range(0f, 100f)]
    public float ArmorPenetrationPct = 100f;

    [Header("Cold")]
    [Range(0f, 100f)]
    public float SlowPct = 60f;
    public float SlowSeconds = 3f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateColdExecution(
            ctx.GunnerId,
            Range,
            AngleDegrees,
            DamageMultiplier,
            ArmorPenetrationPct,
            SlowPct,
            SlowSeconds
        );
    }
}

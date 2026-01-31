using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Rust Execution")]
public class RustExecutionSkillSO : LimitBreakSkillSO
{
    [Header("Rust Execution")]
    [Tooltip("Damage = EffectiveGunnerDamage * DamageMultiplier")]
    public float DamageMultiplier = 8f;

    [Range(0f, 100f)]
    public float ArmorPenetrationPct = 100f;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateRustExecution(ctx.GunnerId, DamageMultiplier, ArmorPenetrationPct);
    }
}

using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Cinderslash")]
public class CinderslashSkillSO : LimitBreakSkillSO
{
    [Header("Cinderslash Tunables")]
    [Tooltip("Slash damage = GunnerEffectiveDamage * SlashDamageMultiplier (default 2.0 = 200%)")]
    public float SlashDamageMultiplier = 2f;

    [Tooltip("Radius (world units) for the initial click hit around the cursor")]
    public float SlashRadius = 0.75f;

    [Tooltip("Trap damage = GunnerEffectiveDamage * TrapDamageMultiplier (default 2.0 = 200%)")]
    public float TrapDamageMultiplier = 2f;

    [Range(0f, 100f)]
    public float ArmorPenetrationPct = 100f;

    [Tooltip("Trap AoE radius in world units")]
    public float TrapRadius = 1.0f;

    [Tooltip("Delay before trap explodes after triggering")]
    public float TrapDelay = 0f;

    [Header("Pooling")]
    public GameObject LBTrapPrefab;
    public int LBTrapPoolSize = 6;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateCinderslash(
            ctx.GunnerId,
            SlashDamageMultiplier,
            SlashRadius,
            ArmorPenetrationPct,
            0f,                 // width NOT used anymore (kept for signature)
            TrapRadius,
            TrapDamageMultiplier,
            TrapDelay,
            LBTrapPrefab,
            LBTrapPoolSize
        );
    }
}

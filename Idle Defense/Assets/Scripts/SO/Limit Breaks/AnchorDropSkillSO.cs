using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Anchor Drop")]
public class AnchorDropSkillSO : LimitBreakSkillSO
{
    [Header("Anchor Drop")]
    public float Radius = 6f;

    [Tooltip("Delay before impact damage/knockback happens (seconds).")]
    public float ImpactDelay = 0.12f;

    [Header("Knockback")]
    public float KnockTime = 0.4f;
    public float KnockSpeed = 12f;

    [Header("Damage")]
    [Tooltip("Armor penetration percent for this AoE hit.")]
    [Range(0f, 100f)]
    public float ArmorPenetrationPct = 0f;

    [Tooltip("If > 0, Damage becomes (EffectiveGunnerDamage * pct / 100). If 0, uses FlatDamage.")]
    public float DamagePctOfGunnerDamage = 0f;

    public float FlatDamage = 90f;
    public float MinDamage = 0f;
    public float MaxDamage = 999999f;

    [Header("Optional VFX")]
    [Tooltip("Optional impact prefab to spawn at the target point.")]
    public GameObject ImpactPrefab;

    [Tooltip("Spawn offset on Y for the impact prefab.")]
    public float ImpactPrefabYOffset = 0f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dmg = lbm.ResolvePower(ctx, FlatDamage, DamagePctOfGunnerDamage, MinDamage, MaxDamage);

        lbm.ActivateAnchorDrop(
            radius: Radius,
            impactDelay: ImpactDelay,
            knockTime: KnockTime,
            knockSpeed: KnockSpeed,
            damage: dmg,
            armorPenetrationPct: ArmorPenetrationPct,
            impactPrefab: ImpactPrefab,
            impactPrefabYOffset: ImpactPrefabYOffset
        );
    }
}

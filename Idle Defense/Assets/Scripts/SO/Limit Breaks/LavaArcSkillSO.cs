using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Lava Arc")]
public class LavaArcSkillSO : LimitBreakSkillSO
{
    [Header("Path")]
    public float TotalDuration = 1.15f;

    [Tooltip("How far forward the boomerang travels before returning.")]
    public float ForwardDistance = 16f;

    [Tooltip("How wide the arc swings sideways (in world units).")]
    public float SideDistance = 9f;

    [Tooltip("If you want the boomerang to start slightly offset from the gunner.")]
    public Vector3 SpawnOffset = Vector3.zero;

    [Header("Damage")]
    public float DamagePctOfGunnerDamage = 0f; // if > 0 use pct, else use FlatDamage
    public float FlatDamage = 140f;
    public float MinDamage = 0f;
    public float MaxDamage = 999999f;

    [Range(0f, 100f)]
    public float ArmorPenetrationPct = 0f;

    [Tooltip("Radius around the boomerang to apply AoE ticks.")]
    public float DamageRadius = 2.2f;

    [Tooltip("How often we apply damage checks while moving.")]
    public float TickRate = 45f;

    [Tooltip("Minimum time before the same enemy can be hit again by this boomerang.")]
    public float PerEnemyHitInterval = 0.18f;

    [Header("Prefab")]
    public GameObject BoomerangPrefab;

    [Tooltip("Optional: rotate boomerang around Y while traveling.")]
    public float SpinDegreesPerSecond = 720f;

    public override void Activate(LimitBreakContext ctx)
    {
        var lbm = LimitBreakManager.Instance;
        if (lbm == null) return;

        float dmg = lbm.ResolvePower(ctx, FlatDamage, DamagePctOfGunnerDamage, MinDamage, MaxDamage);

        lbm.ActivateLavaArc(
            ctx: ctx,
            prefab: BoomerangPrefab,
            spawnOffset: SpawnOffset,
            totalDuration: TotalDuration,
            forwardDistance: ForwardDistance,
            sideDistance: SideDistance,
            damage: dmg,
            armorPenetrationPct: ArmorPenetrationPct,
            damageRadius: DamageRadius,
            tickRate: TickRate,
            perEnemyHitInterval: PerEnemyHitInterval,
            spinDps: SpinDegreesPerSecond
        );
    }
}

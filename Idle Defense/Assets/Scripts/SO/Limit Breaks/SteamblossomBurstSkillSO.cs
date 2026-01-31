using UnityEngine;

[CreateAssetMenu(menuName = "LimitBreak/Steamblossom Burst")]
public class SteamblossomBurstSkillSO : LimitBreakSkillSO
{
    [Header("Spawn")]
    [Tooltip("How many flower traps to spawn.")]
    public int TrapCount = 8;

    [Tooltip("Random placement radius (world units) around the center Z.")]
    public float SpawnRadius = 6f;

    [Tooltip("Optional time between each spawn (0 = instant burst).")]
    public float SpawnInterval = 0.05f;

    [Header("Trap Behavior")]
    [Tooltip("Trap explosion delay (0 = explode on trigger immediately).")]
    public float TrapDelay = 0f;

    [Tooltip("Trap AoE radius in world units.")]
    public float TrapRadius = 1.25f;

    [Header("Trap Power")]
    [Tooltip("If PercentOfGunnerDamage > 0, this is ignored.")]
    public float FlatDamage = 10f;

    [Tooltip("0 = use FlatDamage. Otherwise uses (GunnerDamage * Percent/100).")]
    [Range(0f, 500f)]
    public float PercentOfGunnerDamage = 0f;

    [Header("Pooling")]
    public GameObject LBTrapPrefab;
    public int LBTrapPoolSize = 16;

    public override void Activate(LimitBreakContext ctx)
    {
        LimitBreakManager.Instance?.ActivateSteamblossomBurst(
            ctx.GunnerId,
            TrapCount,
            SpawnRadius,
            TrapRadius,
            TrapDelay,
            0f, // armorPenetrationPct
            FlatDamage,
            PercentOfGunnerDamage,
            LBTrapPrefab,
            LBTrapPoolSize
        );

    }
}

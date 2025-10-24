using Assets.Scripts.SO; // TurretInfoSO, TurretType, EnemyInfoSO
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// GunnerSO, GunnerStatKey, LimitBreakType, PrestigeNodeSO are in global ns in your project

public class GameIconManager : MonoBehaviour
{
    public static GameIconManager Instance { get; private set; }

    [SerializeField] private GameIconLibrarySO library;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ---------- Generic access ----------
    public Sprite Get(string key) => library ? library.Get(key) : null;

    // ----- Turret -----
    public Sprite IconForTurretType(TurretType t)                // e.g., turret.Sniper
        => Get($"turret.{t}");

    public Sprite IconForTurretStat(TurretStatKey stat)          // e.g., stat.damage
        => Get($"stat.{Key(stat)}");

    // ----- Gunner -----
    public Sprite IconForGunnerStat(GunnerStatKey stat)          // maps to shared stat keys
        => Get($"stat.{Key(stat)}");

    public Sprite IconForGunnerId(string gunnerId)               // optional bespoke icon
        => string.IsNullOrWhiteSpace(gunnerId) ? null : Get($"gunner.{gunnerId}");

    public Sprite IconForLimitBreak(LimitBreakType lb)           // unlock.limitbreak or specific lb
        => Get(lb == LimitBreakType.None ? "unlock.limitbreak" : "unlock.limitbreak");

    // ----- Enemy -----
    public Sprite IconForEnemyClass(Assets.Scripts.SO.EnemyClass cls)
        => Get($"enemyclass.{cls}");

    public Sprite IconForEnemyStat(EnemyStatKey stat)            // e.g., stat.enemy_hp
        => Get($"stat.{Key(stat)}");

    // ----- Prestige Node (effects + unlocks) -----
    public IEnumerable<Sprite> GetEffectIcons(PrestigeNodeSO node, int max = 2)
    {
        if (!node) yield break;

        // Unlocks take priority over stats (as requested)
        if (node.UnlockTurretTypes != null && node.UnlockTurretTypes.Count > 0)
            yield return Get("unlock.turret");

        if (node.UnlockGunnerIds != null && node.UnlockGunnerIds.Count > 0)
            yield return Get("unlock.gunner");

        if (node.UnlockLimitBreaks != null && node.UnlockLimitBreaks.Count > 0)
            yield return Get("unlock.limitbreak");

        // Fill remaining slots with strongest non-zero stats
        var pairs = PrestigeStatPairs(node); // (key, weight)
        foreach (var key in pairs.Select(p => p.key).Distinct())
        {
            if (max <= 0) yield break;
            yield return Get($"stat.{key}");
            max--;
        }
    }

    // ---------- Helpers / mapping ----------

    private static string Key(TurretStatKey s) => s switch
    {
        TurretStatKey.Damage => "damage",
        TurretStatKey.FireRate => "firerate",
        TurretStatKey.Range => "range",
        TurretStatKey.RotationSpeed => "rotation",
        TurretStatKey.CritChance => "crit_chance",
        TurretStatKey.CritDamage => "crit_damage",
        TurretStatKey.SplashDamage => "splash",
        TurretStatKey.PierceChance => "pierce_chance",
        TurretStatKey.PierceFalloff => "pierce_falloff",
        TurretStatKey.PelletCount => "pellet",
        TurretStatKey.DamageFalloff => "dist_falloff",
        TurretStatKey.BonusDps => "bonus_dps",
        TurretStatKey.SlowEffect => "slow",
        TurretStatKey.Knockback => "knockback",
        TurretStatKey.ExplosionRadius => "explo_radius",
        TurretStatKey.ExplosionDelay => "explo_delay",
        TurretStatKey.BounceCount => "bounce_count",
        TurretStatKey.BounceRange => "bounce_range",
        TurretStatKey.BounceDelay => "bounce_delay",
        TurretStatKey.BounceLoss => "bounce_loss",
        TurretStatKey.ConeAngle => "cone_angle",
        TurretStatKey.AheadDistance => "ahead_dist",
        TurretStatKey.MaxTrapsActive => "max_traps",
        TurretStatKey.ArmorPenetration => "armorpen",
        _ => "unknown"
    };

    private static string Key(GunnerStatKey s) => s switch
    {
        GunnerStatKey.Damage => "damage",
        GunnerStatKey.FireRate => "firerate",
        GunnerStatKey.Range => "range",
        GunnerStatKey.CriticalChance => "crit_chance",
        GunnerStatKey.CriticalDamageMultiplier => "crit_damage",
        GunnerStatKey.SplashDamage => "splash",
        GunnerStatKey.PierceChance => "pierce_chance",
        GunnerStatKey.PierceDamageFalloff => "pierce_falloff",
        GunnerStatKey.SlowEffect => "slow",
        GunnerStatKey.KnockbackStrength => "knockback",
        GunnerStatKey.PercentBonusDamagePerSec => "bonus_dps",
        GunnerStatKey.ArmorPenetration => "armorpen",
        GunnerStatKey.Health => "health",
        _ => "unknown"
    };

    private static string Key(EnemyStatKey s) => s switch
    {
        EnemyStatKey.MaxHealth => "enemy_hp",
        EnemyStatKey.MovementSpeed => "speed",
        EnemyStatKey.Damage => "damage",
        EnemyStatKey.AttackRange => "range",
        EnemyStatKey.AttackSpeed => "firerate",
        EnemyStatKey.CoinDrop => "currency",
        _ => "unknown"
    };

    // Build weighted list of prestige effects (strongest first)
    private static IEnumerable<(string key, float weight)> PrestigeStatPairs(PrestigeNodeSO n)
    {
        var L = new List<(string, float)>();
        void Add(string k, float v) { if (Mathf.Abs(v) > 0.0001f) L.Add((k, Mathf.Abs(v))); }

        Add("damage", n.GlobalDamagePct);
        Add("firerate", n.GlobalFireRatePct);
        Add("crit_chance", n.GlobalCritChancePct);
        Add("crit_damage", n.GlobalCritDamagePct);
        Add("pierce_chance", n.GlobalPierceChancePct);
        Add("range", n.RangePct);
        Add("rotation", n.RotationSpeedPct);
        Add("explo_radius", n.ExplosionRadiusPct);
        Add("splash", n.SplashDamagePct);
        Add("pierce_falloff", -n.PierceDamageFalloffPct);
        Add("pellet", n.PelletCountPct);
        Add("dist_falloff", -n.DamageFalloffOverDistancePct);
        Add("bonus_dps", n.PercentBonusDamagePerSecPct);
        Add("slow", n.SlowEffectPct);
        Add("knockback", n.KnockbackStrengthPct);
        Add("bounce_count", n.BounceCountPct);
        Add("bounce_range", n.BounceRangePct);
        Add("bounce_delay", -n.BounceDelayPct);
        Add("bounce_loss", -n.BounceDamagePctPct);
        Add("cone_angle", n.ConeAnglePct);
        Add("explo_delay", -n.ExplosionDelayPct);
        Add("ahead_dist", n.AheadDistancePct);
        Add("max_traps", n.MaxTrapsActivePct);
        Add("armorpen", n.ArmorPenetrationPct);
        Add("scraps", n.ScrapsGainPct);
        Add("blacksteel", n.BlackSteelGainPct);
        Add("enemy_hp", -n.EnemyHealthPct);
        Add("enemy_count", -n.EnemyCountPct);
        Add("speed_cap", n.SpeedMultiplierCapBonus);
        Add("damage_cap", n.DamageMultiplierCapBonus);

        // order & cap to two icons
        return L.OrderByDescending(p => p.Item2).Take(2);
    }
}

public enum TurretStatKey
{
    Damage, FireRate, Range, RotationSpeed,
    CritChance, CritDamage, SplashDamage,
    PierceChance, PierceFalloff, PelletCount,
    DamageFalloff, BonusDps, SlowEffect, Knockback,
    ExplosionRadius, ExplosionDelay, BounceCount, BounceRange, BounceDelay, BounceLoss,
    ConeAngle, AheadDistance, MaxTrapsActive, ArmorPenetration
}

public enum EnemyStatKey
{
    MaxHealth, MovementSpeed, Damage, AttackRange, AttackSpeed, CoinDrop
}

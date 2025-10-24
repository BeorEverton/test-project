using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "IdleDefense/Icons/Game Icon Library")]
public class GameIconLibrarySO : ScriptableObject
{
    [Serializable] public struct Pair { public string key; public Sprite sprite; }

    [Header("Sprite entries (key → sprite)")]
    public List<Pair> entries = new();

    [Header("Optional fallback")]
    public Sprite fallback;

    Dictionary<string, Sprite> _map;

    void OnEnable()
    {
        _map = new(StringComparer.OrdinalIgnoreCase);
        foreach (var e in entries)
        {
            if (!string.IsNullOrWhiteSpace(e.key) && e.sprite)
                _map[e.key] = e.sprite;
        }
    }

    public Sprite Get(string key)
        => (!string.IsNullOrWhiteSpace(key) && _map != null && _map.TryGetValue(key, out var s)) ? s : fallback;
}
public static class GameIconKeys
{
    // Stats (shared)
    public const string Health = "stat.health";
    public const string Damage = "stat.damage";
    public const string FireRate = "stat.firerate";
    public const string Range = "stat.range";
    public const string Rotation = "stat.rotation";
    public const string CritChance = "stat.crit_chance";
    public const string CritDamage = "stat.crit_damage";
    public const string Splash = "stat.splash";
    public const string PierceChance = "stat.pierce_chance";
    public const string PierceFalloff = "stat.pierce_falloff";
    public const string Pellet = "stat.pellet";
    public const string Knockback = "stat.knockback";
    public const string ExploRadius = "stat.explo_radius";
    public const string ExploDelay = "stat.explo_delay";
    public const string BounceCount = "stat.bounce_count";
    public const string BounceRange = "stat.bounce_range";
    public const string BounceDelay = "stat.bounce_delay";
    public const string BounceLoss = "stat.bounce_loss";
    public const string ConeAngle = "stat.cone_angle";
    public const string DistFalloff = "stat.dist_falloff";
    public const string BonusDps = "stat.bonus_dps";
    public const string Slow = "stat.slow";
    public const string AheadDist = "stat.ahead_dist";
    public const string MaxTraps = "stat.max_traps";
    public const string ArmorPen = "stat.armorpen";

    // Currencies
    public const string Crimson = "currency.crimson";
    public const string Scraps = "currency.scraps";
    public const string BlackSteel = "currency.blacksteel";

    // Unlocks
    public const string UnlockTurret = "unlock.turret";
    public const string UnlockGunner = "unlock.gunner";
    public const string UnlockLimitBreak = "unlock.limitbreak";

    // Types
    public static string TurretType(Assets.Scripts.SO.TurretType t) => $"turret.{t}";
    public static string GunnerId(string gunnerId) => $"gunner.{gunnerId}";
    public static string LimitBreakType(LimitBreakType t) => $"limitbreak.{t}";

    // ── Prestige/Economy stat gains ─────────────────────────────────────────────
    public const string ScrapsGainStat = "stat.scraps";      // ScrapsGainPct
    public const string BlackSteelGainStat = "stat.blacksteel";  // BlackSteelGainPct

    // ── Enemy modifiers (multiplicative) ────────────────────────────────────────
    public const string EnemyHealth = "stat.enemy_hp";    // EnemyHealthPct (negative is "good")
    public const string EnemyCount = "stat.enemy_count"; // EnemyCountPct  (negative is "good")

    // ── LimitBreak caps (scalar bonuses) ────────────────────────────────────────
    public const string SpeedCap = "stat.speed_cap";   // SpeedMultiplierCapBonus
    public const string DamageCap = "stat.damage_cap";  // DamageMultiplierCapBonus

    // ── Upgrade cost reductions ────────────────────────────────────────────────
    public const string UpgradeCostAll = "stat.upgrade_cost";          // AllUpgradeCostPct
    public static string UpgradeCostType(Assets.Scripts.Systems.TurretUpgradeType t)
        => $"stat.upgrade_cost.{t}";
}

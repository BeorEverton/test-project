using Assets.Scripts.SO;
using UnityEngine;

/// <summary>
/// This class is used to store and upgrade the stats of a turret instance.
/// </summary>
[System.Serializable]
public class TurretStatsInstance
{
    public float Damage;
    public float DamageLevel;
    public float FireRate;
    public float FireRateLevel;
    public float CriticalChance;
    public float CriticalChanceLevel;
    public float CriticalDamageMultiplier;
    public float CriticalDamageMultiplierLevel;
    public float ExplosionRadius;
    public float ExplosionRadiusLevel;
    public float SplashDamage;
    public float SplashDamageLevel;
    public int PierceCount;
    public int PierceCountLevel;
    public float PierceDamageFalloff;
    public float PierceDamageFalloffLevel;
    public int PelletCount;
    public int PelletCountLevel;
    public float DamageFalloffOverDistance;
    public float DamageFalloffOverDistanceLevel;
    public float ProcentBonusDamagePerSec;
    public float ProcentBonusDamagePerSecLevel;
    public float SlowEffect;
    public float SlowEffectLevel;

    // These are static and not upgradeable
    public readonly float RotationSpeed;
    public readonly float AngleThreshold;

    public TurretStatsInstance(TurretInfoSO source)
    {
        Damage = source.Damage;
        DamageLevel = source.DamageLevel;
        FireRate = source.FireRate;
        FireRateLevel = source.FireRateLevel;   
        CriticalChance = source.CriticalChance;
        CriticalChanceLevel = source.CriticalChanceLevel;
        CriticalDamageMultiplier = source.CriticalDamageMultiplier;
        CriticalDamageMultiplierLevel = source.CriticalDamageMultiplierLevel;
        ExplosionRadius = source.ExplosionRadius;
        ExplosionRadiusLevel = source.ExplosionRadiusLevel;
        SplashDamage = source.SplashDamage;
        SplashDamageLevel = source.SplashDamageLevel;
        PierceCount = source.PierceCount;
        PierceCountLevel = source.PierceCountLevel;
        PierceDamageFalloff = source.PierceDamageFalloff;
        PierceDamageFalloffLevel = source.PierceDamageFalloffLevel;
        PelletCount = source.PelletCount;
        PelletCountLevel = source.PelletCountLevel;
        DamageFalloffOverDistance = source.DamageFalloffOverDistance;
        DamageFalloffOverDistanceLevel = source.DamageFalloffOverDistanceLevel;
        ProcentBonusDamagePerSec = source.ProcentBonusDamagePerSec;
        ProcentBonusDamagePerSecLevel = source.ProcentBonusDamagePerSecLevel;
        SlowEffect = source.SlowEffect;
        SlowEffectLevel = source.SlowEffectLevel;

        RotationSpeed = source.RotationSpeed;
        AngleThreshold = source.AngleThreshold;
    }
}

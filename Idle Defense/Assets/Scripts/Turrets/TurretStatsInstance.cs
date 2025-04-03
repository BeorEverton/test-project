using Assets.Scripts.SO;
using UnityEngine;

namespace Assets.Scripts.Turrets
{
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
        public float PercentBonusDamagePerSec;
        public float PercentBonusDamagePerSecLevel;
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
            PercentBonusDamagePerSec = source.PercentBonusDamagePerSec;
            PercentBonusDamagePerSecLevel = source.PercentBonusDamagePerSecLevel;
            SlowEffect = source.SlowEffect;
            SlowEffectLevel = source.SlowEffectLevel;

            RotationSpeed = source.RotationSpeed;
            AngleThreshold = source.AngleThreshold;
        }

        public int GetUpgradeCost(string statName)
        {
            return statName switch
            {
                nameof(DamageLevel) => (int)Mathf.Pow(2, DamageLevel),
                nameof(FireRateLevel) => (int)Mathf.Pow(2, FireRateLevel),
                nameof(CriticalChanceLevel) => (int)Mathf.Pow(2, CriticalChanceLevel),
                nameof(CriticalDamageMultiplierLevel) => (int)Mathf.Pow(2, CriticalDamageMultiplierLevel),
                nameof(ExplosionRadiusLevel) => (int)Mathf.Pow(2, ExplosionRadiusLevel),
                nameof(SplashDamageLevel) => (int)Mathf.Pow(2, SplashDamageLevel),
                nameof(PierceCountLevel) => (int)Mathf.Pow(2, PierceCountLevel),
                nameof(PierceDamageFalloffLevel) => (int)Mathf.Pow(2, PierceDamageFalloffLevel),
                nameof(PelletCountLevel) => (int)Mathf.Pow(2, PelletCountLevel),
                nameof(DamageFalloffOverDistanceLevel) => (int)Mathf.Pow(2, DamageFalloffOverDistanceLevel),
                nameof(PercentBonusDamagePerSecLevel) => (int)Mathf.Pow(2, PercentBonusDamagePerSecLevel),
                nameof(SlowEffectLevel) => (int)Mathf.Pow(2, SlowEffectLevel),
                _ => 0
            };
        }

    }
}

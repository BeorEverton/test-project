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
        public bool IsUnlocked;
        public TurretType TurretType;   // add at top – nothing else changes

        [Header("Base Stats")]
        //DO NOT TOUCH AT RUNTIME
        public float BaseDamage;
        public float BaseFireRate;
        public float BaseCritChance;
        public float BaseCritDamage;

        public float Damage;
        public int DamageLevel;
        public float DamageUpgradeAmount;
        public int DamageUpgradeBaseCost;
        public float DamageCostExponentialMultiplier;

        public float FireRate;
        public int FireRateLevel;
        public float FireRateUpgradeAmount;
        public int FireRateUpgradeBaseCost;
        public float FireRateCostExponentialMultiplier;

        public float CriticalChance;
        public int CriticalChanceLevel;
        public float CriticalChanceUpgradeAmount;
        public int CriticalChanceUpgradeBaseCost;
        public float CriticalChanceCostExponentialMultiplier;

        public float CriticalDamageMultiplier;
        public int CriticalDamageMultiplierLevel;
        public float CriticalDamageMultiplierUpgradeAmount;
        public int CriticalDamageMultiplierUpgradeBaseCost;
        public float CriticalDamageCostExponentialMultiplier;

        public float ExplosionRadius;
        public int ExplosionRadiusLevel;
        public float ExplosionRadiusUpgradeAmount;
        public int ExplosionRadiusUpgradeBaseCost;
        public float ExplosionRadiusCostExponentialMultiplier;

        public float SplashDamage;
        public int SplashDamageLevel;
        public float SplashDamageUpgradeAmount;
        public int SplashDamageUpgradeBaseCost;
        public float SplashDamageCostExponentialMultiplier;

        public float PierceChance;
        public int PierceChanceLevel;
        public float PierceChanceUpgradeAmount;
        public int PierceChanceUpgradeBaseCost;
        public float PierceChanceCostExponentialMultiplier;

        public float PierceDamageFalloff;
        public int PierceDamageFalloffLevel;
        public float PierceDamageFalloffUpgradeAmount;
        public int PierceDamageFalloffUpgradeBaseCost;
        public float PierceDamageFalloffCostExponentialMultiplier;

        public int PelletCount;
        public int PelletCountLevel;
        public int PelletCountUpgradeAmount;
        public int PelletCountUpgradeBaseCost;
        public float PelletCountCostExponentialMultiplier;

        public float DamageFalloffOverDistance;
        public int DamageFalloffOverDistanceLevel;
        public float DamageFalloffOverDistanceUpgradeAmount;
        public int DamageFalloffOverDistanceUpgradeBaseCost;
        public float DamageFalloffOverDistanceCostExponentialMultiplier;

        public float KnockbackStrength;
        public int KnockbackStrengthLevel;
        public float KnockbackStrengthUpgradeAmount;
        public int KnockbackStrengthUpgradeBaseCost;
        public float KnockbackStrengthCostExponentialMultiplier;

        public float PercentBonusDamagePerSec;
        public int PercentBonusDamagePerSecLevel;
        public float PercentBonusDamagePerSecUpgradeAmount;
        public int PercentBonusDamagePerSecUpgradeBaseCost;
        public float PercentBonusDamagePerSecCostExponentialMultiplier;

        public float SlowEffect;
        public int SlowEffectLevel;
        public float SlowEffectUpgradeAmount;
        public int SlowEffectUpgradeBaseCost;
        public float SlowEffectCostExponentialMultiplier;

        public float RotationSpeed;
        public float AngleThreshold;

        public TurretStatsInstance(TurretInfoSO source)
        {
            IsUnlocked = source.IsUnlocked;
            BaseDamage = source.Damage;
            BaseFireRate = source.FireRate;
            BaseCritChance = source.CriticalChance;
            BaseCritDamage = source.CriticalDamageMultiplier;

            Damage = source.Damage;
            DamageLevel = source.DamageLevel;
            DamageUpgradeAmount = source.DamageUpgradeAmount;
            DamageUpgradeBaseCost = source.DamageUpgradeBaseCost;
            DamageCostExponentialMultiplier = source.DamageCostExponentialMultiplier;

            FireRate = source.FireRate;
            FireRateLevel = source.FireRateLevel;
            FireRateUpgradeAmount = source.FireRateUpgradeAmount;
            FireRateUpgradeBaseCost = source.FireRateUpgradeBaseCost;
            FireRateCostExponentialMultiplier = source.FireRateCostExponentialMultiplier;

            CriticalChance = source.CriticalChance;
            CriticalChanceLevel = source.CriticalChanceLevel;
            CriticalChanceUpgradeAmount = source.CriticalChanceUpgradeAmount;
            CriticalChanceUpgradeBaseCost = source.CriticalChanceUpgradeBaseCost;
            CriticalChanceCostExponentialMultiplier = source.CriticalChanceCostExponentialMultiplier;

            CriticalDamageMultiplier = source.CriticalDamageMultiplier;
            CriticalDamageMultiplierLevel = source.CriticalDamageMultiplierLevel;
            CriticalDamageMultiplierUpgradeAmount = source.CriticalDamageMultiplierUpgradeAmount;
            CriticalDamageMultiplierUpgradeBaseCost = source.CriticalDamageMultiplierUpgradeBaseCost;
            CriticalDamageCostExponentialMultiplier = source.CriticalDamageCostExponentialMultiplier;

            ExplosionRadius = source.ExplosionRadius;
            ExplosionRadiusLevel = source.ExplosionRadiusLevel;
            ExplosionRadiusUpgradeAmount = source.ExplosionRadiusUpgradeAmount;
            ExplosionRadiusUpgradeBaseCost = source.ExplosionRadiusUpgradeBaseCost;
            ExplosionRadiusCostExponentialMultiplier = source.ExplosionRadiusCostExponentialMultiplier;

            SplashDamage = source.SplashDamage;
            SplashDamageLevel = source.SplashDamageLevel;
            SplashDamageUpgradeAmount = source.SplashDamageUpgradeAmount;
            SplashDamageUpgradeBaseCost = source.SplashDamageUpgradeBaseCost;
            SplashDamageCostExponentialMultiplier = source.SplashDamageCostExponentialMultiplier;

            PierceChance = source.PierceChance;
            PierceChanceLevel = source.PierceChanceLevel;
            PierceChanceUpgradeAmount = source.PierceChanceUpgradeAmount;
            PierceChanceUpgradeBaseCost = source.PierceChanceUpgradeBaseCost;
            PierceChanceCostExponentialMultiplier = source.PierceChanceCostExponentialMultiplier;

            PierceDamageFalloff = source.PierceDamageFalloff;
            PierceDamageFalloffLevel = source.PierceDamageFalloffLevel;
            PierceDamageFalloffUpgradeAmount = source.PierceDamageFalloffUpgradeAmount;
            PierceDamageFalloffUpgradeBaseCost = source.PierceDamageFalloffUpgradeBaseCost;
            PierceDamageFalloffCostExponentialMultiplier = source.PierceDamageFalloffCostExponentialMultiplier;

            PelletCount = source.PelletCount;
            PelletCountLevel = source.PelletCountLevel;
            PelletCountUpgradeAmount = source.PelletCountUpgradeAmount;
            PelletCountUpgradeBaseCost = source.PelletCountUpgradeBaseCost;
            PelletCountCostExponentialMultiplier = source.PelletCountCostExponentialMultiplier;

            DamageFalloffOverDistance = source.DamageFalloffOverDistance;
            DamageFalloffOverDistanceLevel = source.DamageFalloffOverDistanceLevel;
            DamageFalloffOverDistanceUpgradeAmount = source.DamageFalloffOverDistanceUpgradeAmount;
            DamageFalloffOverDistanceUpgradeBaseCost = source.DamageFalloffOverDistanceUpgradeBaseCost;
            DamageFalloffOverDistanceCostExponentialMultiplier = source.DamageFalloffOverDistanceCostExponentialMultiplier;

            KnockbackStrength = source.KnockbackStrength;
            KnockbackStrengthLevel = source.KnockbackStrengthLevel;
            KnockbackStrengthUpgradeAmount = source.KnockbackStrengthUpgradeAmount;
            KnockbackStrengthUpgradeBaseCost = source.KnockbackStrengthUpgradeBaseCost;
            KnockbackStrengthCostExponentialMultiplier = source.KnockbackStrengthCostExponentialMultiplier;

            PercentBonusDamagePerSec = source.PercentBonusDamagePerSec;
            PercentBonusDamagePerSecLevel = source.PercentBonusDamagePerSecLevel;
            PercentBonusDamagePerSecUpgradeAmount = source.PercentBonusDamagePerSecUpgradeAmount;
            PercentBonusDamagePerSecUpgradeBaseCost = source.PercentBonusDamagePerSecUpgradeBaseCost;
            PercentBonusDamagePerSecCostExponentialMultiplier = source.PercentBonusDamagePerSecCostExponentialMultiplier;

            SlowEffect = source.SlowEffect;
            SlowEffectLevel = source.SlowEffectLevel;
            SlowEffectUpgradeAmount = source.SlowEffectUpgradeAmount;
            SlowEffectUpgradeBaseCost = source.SlowEffectUpgradeBaseCost;
            SlowEffectCostExponentialMultiplier = source.SlowEffectCostExponentialMultiplier;

            RotationSpeed = source.RotationSpeed;
            AngleThreshold = source.AngleThreshold;
        }

        public TurretStatsInstance() { }//Used to load from DTO
    }
}
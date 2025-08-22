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
        public float DamageUpgradeBaseCost;
        public float DamageCostExponentialMultiplier;

        public float FireRate;
        public int FireRateLevel;
        public float FireRateUpgradeAmount;
        public float FireRateUpgradeBaseCost;
        public float FireRateCostExponentialMultiplier;

        public float RotationSpeed;
        public float AngleThreshold;
        public float Range;

        public float CriticalChance;
        public int CriticalChanceLevel;
        public float CriticalChanceUpgradeAmount;
        public float CriticalChanceUpgradeBaseCost;
        public float CriticalChanceCostExponentialMultiplier;

        public float CriticalDamageMultiplier;
        public int CriticalDamageMultiplierLevel;
        public float CriticalDamageMultiplierUpgradeAmount;
        public float CriticalDamageMultiplierUpgradeBaseCost;
        public float CriticalDamageCostExponentialMultiplier;

        public float ExplosionRadius;
        public int ExplosionRadiusLevel;
        public float ExplosionRadiusUpgradeAmount;
        public float ExplosionRadiusUpgradeBaseCost;

        public float SplashDamage;
        public int SplashDamageLevel;
        public float SplashDamageUpgradeAmount;
        public float SplashDamageUpgradeBaseCost;

        public float PierceChance;
        public int PierceChanceLevel;
        public float PierceChanceUpgradeAmount;
        public float PierceChanceUpgradeBaseCost;

        public float PierceDamageFalloff;
        public int PierceDamageFalloffLevel;
        public float PierceDamageFalloffUpgradeAmount;
        public float PierceDamageFalloffUpgradeBaseCost;

        public int PelletCount;
        public int PelletCountLevel;
        public int PelletCountUpgradeAmount;
        public float PelletCountUpgradeBaseCost;

        public float DamageFalloffOverDistance;
        public int DamageFalloffOverDistanceLevel;
        public float DamageFalloffOverDistanceUpgradeAmount;
        public float DamageFalloffOverDistanceUpgradeBaseCost;

        public float KnockbackStrength;
        public int KnockbackStrengthLevel;
        public float KnockbackStrengthUpgradeAmount;
        public float KnockbackStrengthUpgradeBaseCost;
        public float KnockbackStrengthCostExponentialMultiplier;

        public float PercentBonusDamagePerSec;
        public int PercentBonusDamagePerSecLevel;
        public float PercentBonusDamagePerSecUpgradeAmount;
        public float PercentBonusDamagePerSecUpgradeBaseCost;

        public float SlowEffect;
        public int SlowEffectLevel;
        public float SlowEffectUpgradeAmount;
        public float SlowEffectUpgradeBaseCost;

        // ===== Range (upgradeable) =====
        public float RangeUpgradeAmount;
        public float RangeUpgradeBaseCost;
        public int RangeLevel;
        public float RangeCostExponentialMultiplier;

        // ===== Rotation Speed (upgradeable) =====
        public float RotationSpeedUpgradeAmount;
        public float RotationSpeedUpgradeBaseCost;
        public int RotationSpeedLevel;
        public float RotationSpeedCostExponentialMultiplier;

        // ===== Can target flyers =====
        public bool CanHitFlying;

        // ===== Armor Penetration (upgradeable) =====
        public float ArmorPenetration;                    // percent (0-100)
        public int ArmorPenetrationLevel;
        public float ArmorPenetrationUpgradeAmount;
        public float ArmorPenetrationUpgradeBaseCost;
        public float ArmorPenetrationCostExponentialMultiplier;

        [Header("Bounce Pattern")]
        public int BounceCount;
        public int BounceCountLevel;
        public float BounceCountUpgradeAmount;
        public float BounceCountUpgradeBaseCost;
        public float BounceCountCostExponentialMultiplier;

        public float BounceRange;
        public int BounceRangeLevel;
        public float BounceRangeUpgradeAmount;
        public float BounceRangeUpgradeBaseCost;
        public float BounceRangeCostExponentialMultiplier;

        public float BounceDelay;
        public int BounceDelayLevel;
        public float BounceDelayUpgradeAmount;
        public float BounceDelayUpgradeBaseCost;
        public float BounceDelayCostExponentialMultiplier;

        public float BounceDamagePct;
        public int BounceDamagePctLevel;
        public float BounceDamagePctUpgradeAmount;
        public float BounceDamagePctUpgradeBaseCost;
        public float BounceDamagePctCostExponentialMultiplier;

        [Header("Cone AOE Pattern")]
        public float ConeAngle;
        public int ConeAngleLevel;
        public float ConeAngleUpgradeAmount;
        public float ConeAngleUpgradeBaseCost;
        public float ConeAngleCostExponentialMultiplier;

        [Header("Trap Pattern")]
        public float AheadDistance;
        public int AheadDistanceLevel;
        public float AheadDistanceUpgradeAmount;
        public float AheadDistanceUpgradeBaseCost;
        public float AheadDistanceCostExponentialMultiplier;

        [Header("Trap Settings")]
        public GameObject TrapPrefab;

        public int MaxTrapsActive;
        public int MaxTrapsActiveLevel;
        public float MaxTrapsActiveUpgradeAmount;
        public float MaxTrapsActiveUpgradeBaseCost;
        public float MaxTrapsActiveCostExponentialMultiplier;


        [Header("Delayed AOE Pattern")]
        public float ExplosionDelay;
        public int ExplosionDelayLevel;
        public float ExplosionDelayUpgradeAmount;
        public float ExplosionDelayUpgradeBaseCost;
        public float ExplosionDelayCostExponentialMultiplier;


        public TurretStatsInstance(TurretInfoSO source)
        {
            TurretType = source.TurretType;

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

            RotationSpeed = source.RotationSpeed;
            AngleThreshold = source.AngleThreshold;
            Range = source.Range;

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

            SplashDamage = source.SplashDamage;
            SplashDamageLevel = source.SplashDamageLevel;
            SplashDamageUpgradeAmount = source.SplashDamageUpgradeAmount;
            SplashDamageUpgradeBaseCost = source.SplashDamageUpgradeBaseCost;

            PierceChance = source.PierceChance;
            PierceChanceLevel = source.PierceChanceLevel;
            PierceChanceUpgradeAmount = source.PierceChanceUpgradeAmount;
            PierceChanceUpgradeBaseCost = source.PierceChanceUpgradeBaseCost;

            PierceDamageFalloff = source.PierceDamageFalloff;
            PierceDamageFalloffLevel = source.PierceDamageFalloffLevel;
            PierceDamageFalloffUpgradeAmount = source.PierceDamageFalloffUpgradeAmount;
            PierceDamageFalloffUpgradeBaseCost = source.PierceDamageFalloffUpgradeBaseCost;

            PelletCount = source.PelletCount;
            PelletCountLevel = source.PelletCountLevel;
            PelletCountUpgradeAmount = source.PelletCountUpgradeAmount;
            PelletCountUpgradeBaseCost = source.PelletCountUpgradeBaseCost;

            DamageFalloffOverDistance = source.DamageFalloffOverDistance;
            DamageFalloffOverDistanceLevel = source.DamageFalloffOverDistanceLevel;
            DamageFalloffOverDistanceUpgradeAmount = source.DamageFalloffOverDistanceUpgradeAmount;
            DamageFalloffOverDistanceUpgradeBaseCost = source.DamageFalloffOverDistanceUpgradeBaseCost;

            KnockbackStrength = source.KnockbackStrength;
            KnockbackStrengthLevel = source.KnockbackStrengthLevel;
            KnockbackStrengthUpgradeAmount = source.KnockbackStrengthUpgradeAmount;
            KnockbackStrengthUpgradeBaseCost = source.KnockbackStrengthUpgradeBaseCost;
            KnockbackStrengthCostExponentialMultiplier = source.KnockbackStrengthCostExponentialMultiplier;

            PercentBonusDamagePerSec = source.PercentBonusDamagePerSec;
            PercentBonusDamagePerSecLevel = source.PercentBonusDamagePerSecLevel;
            PercentBonusDamagePerSecUpgradeAmount = source.PercentBonusDamagePerSecUpgradeAmount;
            PercentBonusDamagePerSecUpgradeBaseCost = source.PercentBonusDamagePerSecUpgradeBaseCost;

            SlowEffect = source.SlowEffect;
            SlowEffectLevel = source.SlowEffectLevel;
            SlowEffectUpgradeAmount = source.SlowEffectUpgradeAmount;
            SlowEffectUpgradeBaseCost = source.SlowEffectUpgradeBaseCost;
            
            // Bounce Pattern
            BounceCount = source.BounceCount;
            BounceCountLevel = source.BounceCountLevel;
            BounceCountUpgradeAmount = source.BounceCountUpgradeAmount;
            BounceCountUpgradeBaseCost = source.BounceCountUpgradeBaseCost;
            BounceCountCostExponentialMultiplier = source.BounceCountCostExponentialMultiplier;

            BounceRange = source.BounceRange;
            BounceRangeLevel = source.BounceRangeLevel;
            BounceRangeUpgradeAmount = source.BounceRangeUpgradeAmount;
            BounceRangeUpgradeBaseCost = source.BounceRangeUpgradeBaseCost;
            BounceRangeCostExponentialMultiplier = source.BounceRangeCostExponentialMultiplier;

            BounceDelay = source.BounceDelay;
            BounceDelayLevel = source.BounceDelayLevel;
            BounceDelayUpgradeAmount = source.BounceDelayUpgradeAmount;
            BounceDelayUpgradeBaseCost = source.BounceDelayUpgradeBaseCost;
            BounceDelayCostExponentialMultiplier = source.BounceDelayCostExponentialMultiplier;

            BounceDamagePct = source.BounceDamagePct;
            BounceDamagePctLevel = source.BounceDamagePctLevel;
            BounceDamagePctUpgradeAmount = source.BounceDamagePctUpgradeAmount;
            BounceDamagePctUpgradeBaseCost = source.BounceDamagePctUpgradeBaseCost;
            BounceDamagePctCostExponentialMultiplier = source.BounceDamagePctCostExponentialMultiplier;

            // Cone AOE Pattern
            ConeAngle = source.ConeAngle;
            ConeAngleLevel = source.ConeAngleLevel;
            ConeAngleUpgradeAmount = source.ConeAngleUpgradeAmount;
            ConeAngleUpgradeBaseCost = source.ConeAngleUpgradeBaseCost;
            ConeAngleCostExponentialMultiplier = source.ConeAngleCostExponentialMultiplier;

            // Trap Pattern
            AheadDistance = source.AheadDistance;
            AheadDistanceLevel = source.AheadDistanceLevel;
            AheadDistanceUpgradeAmount = source.AheadDistanceUpgradeAmount;
            AheadDistanceUpgradeBaseCost = source.AheadDistanceUpgradeBaseCost;
            AheadDistanceCostExponentialMultiplier = source.AheadDistanceCostExponentialMultiplier;
            // Trap Settings
            TrapPrefab = source.TrapPrefab;

            MaxTrapsActive = source.MaxTrapsActive;
            MaxTrapsActiveLevel = source.MaxTrapsActiveLevel;
            MaxTrapsActiveUpgradeAmount = source.MaxTrapsActiveUpgradeAmount;
            MaxTrapsActiveUpgradeBaseCost = source.MaxTrapsActiveUpgradeBaseCost;
            MaxTrapsActiveCostExponentialMultiplier = source.MaxTrapsActiveCostExponentialMultiplier;


            // Delayed AOE Pattern
            ExplosionDelay = source.ExplosionDelay;
            ExplosionDelayLevel = source.ExplosionDelayLevel;
            ExplosionDelayUpgradeAmount = source.ExplosionDelayUpgradeAmount;
            ExplosionDelayUpgradeBaseCost = source.ExplosionDelayUpgradeBaseCost;
            ExplosionDelayCostExponentialMultiplier = source.ExplosionDelayCostExponentialMultiplier;

            // Range upgrades            
            RangeUpgradeAmount = source.RangeUpgradeAmount;
            RangeUpgradeBaseCost = source.RangeUpgradeBaseCost;
            RangeLevel = source.RangeLevel;
            RangeCostExponentialMultiplier = source.RangeCostExponentialMultiplier;

            // Rotation speed upgrades
            RotationSpeedUpgradeAmount = source.RotationSpeedUpgradeAmount;
            RotationSpeedUpgradeBaseCost = source.RotationSpeedUpgradeBaseCost;
            RotationSpeedLevel = source.RotationSpeedLevel;
            RotationSpeedCostExponentialMultiplier = source.RotationSpeedCostExponentialMultiplier;

            // Targeting flag
            CanHitFlying = source.CanHitFlying;

            // Armor penetration
            ArmorPenetration = source.ArmorPenetration;
            ArmorPenetrationLevel = source.ArmorPenetrationLevel;
            ArmorPenetrationUpgradeAmount = source.ArmorPenetrationUpgradeAmount;
            ArmorPenetrationUpgradeBaseCost = source.ArmorPenetrationUpgradeBaseCost;
            ArmorPenetrationCostExponentialMultiplier = source.ArmorPenetrationCostExponentialMultiplier;

        }

        public TurretStatsInstance() { }//Used to load from DTO

        public int TotalLevel()
        {
            return Mathf.FloorToInt(
                  DamageLevel
                + FireRateLevel
                + CriticalChanceLevel
                + CriticalDamageMultiplierLevel
                + ExplosionRadiusLevel
                + SplashDamageLevel
                + PierceChanceLevel
                + PierceDamageFalloffLevel
                + PelletCountLevel
                + DamageFalloffOverDistanceLevel
                + PercentBonusDamagePerSecLevel
                + SlowEffectLevel
                // Bounce Pattern
                + BounceCountLevel
                + BounceRangeLevel
                + BounceDelayLevel
                + BounceDamagePctLevel

                // Cone AOE Pattern
                + ConeAngleLevel

                // Delayed AOE Pattern
                + ExplosionDelayLevel

                // Trap Pattern
                + AheadDistanceLevel)
                + MaxTrapsActiveLevel

                + RangeLevel
                + RotationSpeedLevel
                + ArmorPenetrationLevel;

        }
    }
}
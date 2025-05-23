// Assets/Editor/Simulation/Core/Blueprints.cs
using Assets.Scripts.SO;      // TurretInfoSO
using Assets.Scripts.Turrets; // TurretType

namespace IdleDefense.Editor.Simulation
{
    public readonly struct TurretBlueprint
    {
        // ---------------------
        // Base stats
        // ---------------------
        public readonly TurretType Type;
        public readonly float Damage;
        public readonly float FireRate;   // shots/sec
        public readonly float Range;
        public readonly float RotationSpeed;
        public readonly float AngleThreshold;

        public readonly float CritChance;           // percent [0–100]
        public readonly float CritDamageMultiplier; // bonus multiplier

        public readonly float ExplosionRadius;
        public readonly float SplashDamage;

        public readonly float PierceChance;         // percent
        public readonly float PierceDamageFalloff;

        public readonly int PelletCount;

        public readonly float KnockbackStrength;

        public readonly float DamageFalloffOverDistance;

        public readonly float PercentBonusDamagePerSec;
        public readonly float SlowEffect;

        // ---------------------
        // Upgrade parameters
        // ---------------------
        public readonly float DamageUpgradeAmount;
        public readonly float DamageUpgradeBaseCost;
        public readonly float DamageCostExponentialMultiplier;

        public readonly float FireRateUpgradeAmount;
        public readonly float FireRateUpgradeBaseCost;
        public readonly float FireRateCostExponentialMultiplier;

        public readonly float CritChanceUpgradeAmount;
        public readonly float CritChanceUpgradeBaseCost;
        public readonly float CritChanceCostExponentialMultiplier;

        public readonly float CritDamageUpgradeAmount;
        public readonly float CritDamageUpgradeBaseCost;
        public readonly float CritDamageCostExponentialMultiplier;

        public readonly float ExplosionRadiusUpgradeAmount;
        public readonly float ExplosionRadiusUpgradeBaseCost;
        public readonly float ExplosionRadiusCostExponentialMultiplier;

        public readonly float SplashDamageUpgradeAmount;
        public readonly float SplashDamageUpgradeBaseCost;
        public readonly float SplashDamageCostExponentialMultiplier;

        public readonly float PierceChanceUpgradeAmount;
        public readonly float PierceChanceUpgradeBaseCost;
        public readonly float PierceChanceCostExponentialMultiplier;

        public readonly float PierceDamageFalloffUpgradeAmount;
        public readonly float PierceDamageFalloffUpgradeBaseCost;
        public readonly float PierceDamageFalloffCostExponentialMultiplier;

        public readonly int PelletCountUpgradeAmount;
        public readonly float PelletCountUpgradeBaseCost;
        public readonly float PelletCountCostExponentialMultiplier;

        public readonly float KnockbackStrengthUpgradeAmount;
        public readonly float KnockbackStrengthUpgradeBaseCost;
        public readonly float KnockbackStrengthCostExponentialMultiplier;

        public readonly float DamageFalloffOverDistanceUpgradeAmount;
        public readonly float DamageFalloffOverDistanceUpgradeBaseCost;
        public readonly float DamageFalloffOverDistanceCostExponentialMultiplier;

        public readonly float PercentBonusDamagePerSecUpgradeAmount;
        public readonly float PercentBonusDamagePerSecUpgradeBaseCost;
        public readonly float PercentBonusDamagePerSecCostExponentialMultiplier;

        public readonly float SlowEffectUpgradeAmount;
        public readonly float SlowEffectUpgradeBaseCost;
        public readonly float SlowEffectCostExponentialMultiplier;

        // ---------------------
        // Constructor from SO
        // ---------------------
        public TurretBlueprint(TurretInfoSO so)
        {
            Type = so.TurretType;
            Damage = so.Damage;
            FireRate = so.FireRate;
            Range = so.Range;
            RotationSpeed = so.RotationSpeed;
            AngleThreshold = so.AngleThreshold;

            CritChance = so.CriticalChance;
            CritDamageMultiplier = so.CriticalDamageMultiplier;

            ExplosionRadius = so.ExplosionRadius;
            SplashDamage = so.SplashDamage;

            PierceChance = so.PierceChance;
            PierceDamageFalloff = so.PierceDamageFalloff;

            PelletCount = so.PelletCount;

            KnockbackStrength = so.KnockbackStrength;

            DamageFalloffOverDistance = so.DamageFalloffOverDistance;

            PercentBonusDamagePerSec = so.PercentBonusDamagePerSec;
            SlowEffect = so.SlowEffect;

            DamageUpgradeAmount = so.DamageUpgradeAmount;
            DamageUpgradeBaseCost = so.DamageUpgradeBaseCost;
            DamageCostExponentialMultiplier = so.DamageCostExponentialMultiplier;

            FireRateUpgradeAmount = so.FireRateUpgradeAmount;
            FireRateUpgradeBaseCost = so.FireRateUpgradeBaseCost;
            FireRateCostExponentialMultiplier = so.FireRateCostExponentialMultiplier;

            CritChanceUpgradeAmount = so.CriticalChanceUpgradeAmount;
            CritChanceUpgradeBaseCost = so.CriticalChanceUpgradeBaseCost;
            CritChanceCostExponentialMultiplier = so.CriticalChanceCostExponentialMultiplier;

            CritDamageUpgradeAmount = so.CriticalDamageMultiplierUpgradeAmount;
            CritDamageUpgradeBaseCost = so.CriticalDamageMultiplierUpgradeBaseCost;
            CritDamageCostExponentialMultiplier = so.CriticalDamageCostExponentialMultiplier;

            ExplosionRadiusUpgradeAmount = so.ExplosionRadiusUpgradeAmount;
            ExplosionRadiusUpgradeBaseCost = so.ExplosionRadiusUpgradeBaseCost;
            ExplosionRadiusCostExponentialMultiplier = so.ExplosionRadiusCostExponentialMultiplier;

            SplashDamageUpgradeAmount = so.SplashDamageUpgradeAmount;
            SplashDamageUpgradeBaseCost = so.SplashDamageUpgradeBaseCost;
            SplashDamageCostExponentialMultiplier = so.SplashDamageCostExponentialMultiplier;

            PierceChanceUpgradeAmount = so.PierceChanceUpgradeAmount;
            PierceChanceUpgradeBaseCost = so.PierceChanceUpgradeBaseCost;
            PierceChanceCostExponentialMultiplier = so.PierceChanceCostExponentialMultiplier;

            PierceDamageFalloffUpgradeAmount = so.PierceDamageFalloffUpgradeAmount;
            PierceDamageFalloffUpgradeBaseCost = so.PierceDamageFalloffUpgradeBaseCost;
            PierceDamageFalloffCostExponentialMultiplier = so.PierceDamageFalloffCostExponentialMultiplier;

            PelletCountUpgradeAmount = so.PelletCountUpgradeAmount;
            PelletCountUpgradeBaseCost = so.PelletCountUpgradeBaseCost;
            PelletCountCostExponentialMultiplier = so.PelletCountCostExponentialMultiplier;

            KnockbackStrengthUpgradeAmount = so.KnockbackStrengthUpgradeAmount;
            KnockbackStrengthUpgradeBaseCost = so.KnockbackStrengthUpgradeBaseCost;
            KnockbackStrengthCostExponentialMultiplier = so.KnockbackStrengthCostExponentialMultiplier;

            DamageFalloffOverDistanceUpgradeAmount = so.DamageFalloffOverDistanceUpgradeAmount;
            DamageFalloffOverDistanceUpgradeBaseCost = so.DamageFalloffOverDistanceUpgradeBaseCost;
            DamageFalloffOverDistanceCostExponentialMultiplier = so.DamageFalloffOverDistanceCostExponentialMultiplier;

            PercentBonusDamagePerSecUpgradeAmount = so.PercentBonusDamagePerSecUpgradeAmount;
            PercentBonusDamagePerSecUpgradeBaseCost = so.PercentBonusDamagePerSecUpgradeBaseCost;
            PercentBonusDamagePerSecCostExponentialMultiplier = so.PercentBonusDamagePerSecCostExponentialMultiplier;

            SlowEffectUpgradeAmount = so.SlowEffectUpgradeAmount;
            SlowEffectUpgradeBaseCost = so.SlowEffectUpgradeBaseCost;
            SlowEffectCostExponentialMultiplier = so.SlowEffectCostExponentialMultiplier;
        }

        // ---------------------
        // Private full-args ctor
        // ---------------------
        private TurretBlueprint(
            TurretType type,
            float damage,
            float fireRate,
            float range,
            float rotationSpeed,
            float angleThreshold,
            float critChance,
            float critDamageMultiplier,
            float explosionRadius,
            float splashDamage,
            float pierceChance,
            float pierceDamageFalloff,
            int pelletCount,
            float knockbackStrength,
            float damageFalloffOverDistance,
            float percentBonusDamagePerSec,
            float slowEffect,
            float damageUpgradeAmount,
            float damageUpgradeBaseCost,
            float damageCostExponentialMultiplier,
            float fireRateUpgradeAmount,
            float fireRateUpgradeBaseCost,
            float fireRateCostExponentialMultiplier,
            float critChanceUpgradeAmount,
            float critChanceUpgradeBaseCost,
            float critChanceCostExponentialMultiplier,
            float critDamageUpgradeAmount,
            float critDamageUpgradeBaseCost,
            float critDamageCostExponentialMultiplier,
            float explosionRadiusUpgradeAmount,
            float explosionRadiusUpgradeBaseCost,
            float explosionRadiusCostExponentialMultiplier,
            float splashDamageUpgradeAmount,
            float splashDamageUpgradeBaseCost,
            float splashDamageCostExponentialMultiplier,
            float pierceChanceUpgradeAmount,
            float pierceChanceUpgradeBaseCost,
            float pierceChanceCostExponentialMultiplier,
            float pierceDamageFalloffUpgradeAmount,
            float pierceDamageFalloffUpgradeBaseCost,
            float pierceDamageFalloffCostExponentialMultiplier,
            int pelletCountUpgradeAmount,
            float pelletCountUpgradeBaseCost,
            float pelletCountCostExponentialMultiplier,
            float knockbackStrengthUpgradeAmount,
            float knockbackStrengthUpgradeBaseCost,
            float knockbackStrengthCostExponentialMultiplier,
            float damageFalloffOverDistanceUpgradeAmount,
            float damageFalloffOverDistanceUpgradeBaseCost,
            float damageFalloffOverDistanceCostExponentialMultiplier,
            float percentBonusDamagePerSecUpgradeAmount,
            float percentBonusDamagePerSecUpgradeBaseCost,
            float percentBonusDamagePerSecCostExponentialMultiplier,
            float slowEffectUpgradeAmount,
            float slowEffectUpgradeBaseCost,
            float slowEffectCostExponentialMultiplier
        ) : this()
        {
            Type = type;
            Damage = damage;
            FireRate = fireRate;
            Range = range;
            RotationSpeed = rotationSpeed;
            AngleThreshold = angleThreshold;
            CritChance = critChance;
            CritDamageMultiplier = critDamageMultiplier;
            ExplosionRadius = explosionRadius;
            SplashDamage = splashDamage;
            PierceChance = pierceChance;
            PierceDamageFalloff = pierceDamageFalloff;
            PelletCount = pelletCount;
            KnockbackStrength = knockbackStrength;
            DamageFalloffOverDistance = damageFalloffOverDistance;
            PercentBonusDamagePerSec = percentBonusDamagePerSec;
            SlowEffect = slowEffect;
            DamageUpgradeAmount = damageUpgradeAmount;
            DamageUpgradeBaseCost = damageUpgradeBaseCost;
            DamageCostExponentialMultiplier = damageCostExponentialMultiplier;
            FireRateUpgradeAmount = fireRateUpgradeAmount;
            FireRateUpgradeBaseCost = fireRateUpgradeBaseCost;
            FireRateCostExponentialMultiplier = fireRateCostExponentialMultiplier;
            CritChanceUpgradeAmount = critChanceUpgradeAmount;
            CritChanceUpgradeBaseCost = critChanceUpgradeBaseCost;
            CritChanceCostExponentialMultiplier = critChanceCostExponentialMultiplier;
            CritDamageUpgradeAmount = critDamageUpgradeAmount;
            CritDamageUpgradeBaseCost = critDamageUpgradeBaseCost;
            CritDamageCostExponentialMultiplier = critDamageCostExponentialMultiplier;
            ExplosionRadiusUpgradeAmount = explosionRadiusUpgradeAmount;
            ExplosionRadiusUpgradeBaseCost = explosionRadiusUpgradeBaseCost;
            ExplosionRadiusCostExponentialMultiplier = explosionRadiusCostExponentialMultiplier;
            SplashDamageUpgradeAmount = splashDamageUpgradeAmount;
            SplashDamageUpgradeBaseCost = splashDamageUpgradeBaseCost;
            SplashDamageCostExponentialMultiplier = splashDamageCostExponentialMultiplier;
            PierceChanceUpgradeAmount = pierceChanceUpgradeAmount;
            PierceChanceUpgradeBaseCost = pierceChanceUpgradeBaseCost;
            PierceChanceCostExponentialMultiplier = pierceChanceCostExponentialMultiplier;
            PierceDamageFalloffUpgradeAmount = pierceDamageFalloffUpgradeAmount;
            PierceDamageFalloffUpgradeBaseCost = pierceDamageFalloffUpgradeBaseCost;
            PierceDamageFalloffCostExponentialMultiplier = pierceDamageFalloffCostExponentialMultiplier;
            PelletCountUpgradeAmount = pelletCountUpgradeAmount;
            PelletCountUpgradeBaseCost = pelletCountUpgradeBaseCost;
            PelletCountCostExponentialMultiplier = pelletCountCostExponentialMultiplier;
            KnockbackStrengthUpgradeAmount = knockbackStrengthUpgradeAmount;
            KnockbackStrengthUpgradeBaseCost = knockbackStrengthUpgradeBaseCost;
            KnockbackStrengthCostExponentialMultiplier = knockbackStrengthCostExponentialMultiplier;
            DamageFalloffOverDistanceUpgradeAmount = damageFalloffOverDistanceUpgradeAmount;
            DamageFalloffOverDistanceUpgradeBaseCost = damageFalloffOverDistanceUpgradeBaseCost;
            DamageFalloffOverDistanceCostExponentialMultiplier = damageFalloffOverDistanceCostExponentialMultiplier;
            PercentBonusDamagePerSecUpgradeAmount = percentBonusDamagePerSecUpgradeAmount;
            PercentBonusDamagePerSecUpgradeBaseCost = percentBonusDamagePerSecUpgradeBaseCost;
            PercentBonusDamagePerSecCostExponentialMultiplier = percentBonusDamagePerSecCostExponentialMultiplier;
            SlowEffectUpgradeAmount = slowEffectUpgradeAmount;
            SlowEffectUpgradeBaseCost = slowEffectUpgradeBaseCost;
            SlowEffectCostExponentialMultiplier = slowEffectCostExponentialMultiplier;
        }

        // ---------------------
        // Compute DPS
        // ---------------------
        public float DamagePerSecond(float clickBonus = 0f)
        {
            return (Damage * (1f + clickBonus)) * FireRate;
        }

        // ---------------------
        // All WithXUpgraded methods
        // ---------------------
        public TurretBlueprint WithDamageUpgraded()
        {
            return new TurretBlueprint(
                Type, Damage + DamageUpgradeAmount, FireRate, Range,
                RotationSpeed, AngleThreshold, CritChance, CritDamageMultiplier,
                ExplosionRadius, SplashDamage, PierceChance, PierceDamageFalloff,
                PelletCount, KnockbackStrength, DamageFalloffOverDistance,
                PercentBonusDamagePerSec, SlowEffect,
                DamageUpgradeAmount, DamageUpgradeBaseCost, DamageCostExponentialMultiplier,
                FireRateUpgradeAmount, FireRateUpgradeBaseCost, FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount, CritChanceUpgradeBaseCost, CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount, CritDamageUpgradeBaseCost, CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount, ExplosionRadiusUpgradeBaseCost, ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount, SplashDamageUpgradeBaseCost, SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount, PierceChanceUpgradeBaseCost, PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount, PierceDamageFalloffUpgradeBaseCost, PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount, PelletCountUpgradeBaseCost, PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount, KnockbackStrengthUpgradeBaseCost, KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount, DamageFalloffOverDistanceUpgradeBaseCost, DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount, PercentBonusDamagePerSecUpgradeBaseCost, PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithFireRateUpgraded()
        {
            return new TurretBlueprint(
                Type, Damage, FireRate + FireRateUpgradeAmount, Range,
                RotationSpeed, AngleThreshold, CritChance, CritDamageMultiplier,
                ExplosionRadius, SplashDamage, PierceChance, PierceDamageFalloff,
                PelletCount, KnockbackStrength, DamageFalloffOverDistance,
                PercentBonusDamagePerSec, SlowEffect,
                DamageUpgradeAmount, DamageUpgradeBaseCost, DamageCostExponentialMultiplier,
                FireRateUpgradeAmount, FireRateUpgradeBaseCost, FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount, CritChanceUpgradeBaseCost, CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount, CritDamageUpgradeBaseCost, CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount, ExplosionRadiusUpgradeBaseCost, ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount, SplashDamageUpgradeBaseCost, SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount, PierceChanceUpgradeBaseCost, PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount, PierceDamageFalloffUpgradeBaseCost, PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount, PelletCountUpgradeBaseCost, PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount, KnockbackStrengthUpgradeBaseCost, KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount, DamageFalloffOverDistanceUpgradeBaseCost, DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount, PercentBonusDamagePerSecUpgradeBaseCost, PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithCritChanceUpgraded()
        {
            return new TurretBlueprint(
                Type, Damage, FireRate, Range,
                RotationSpeed, AngleThreshold,
                CritChance + CritChanceUpgradeAmount, CritDamageMultiplier,
                ExplosionRadius, SplashDamage, PierceChance, PierceDamageFalloff,
                PelletCount, KnockbackStrength, DamageFalloffOverDistance,
                PercentBonusDamagePerSec, SlowEffect,
                DamageUpgradeAmount, DamageUpgradeBaseCost, DamageCostExponentialMultiplier,
                FireRateUpgradeAmount, FireRateUpgradeBaseCost, FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount, CritChanceUpgradeBaseCost, CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount, CritDamageUpgradeBaseCost, CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount, ExplosionRadiusUpgradeBaseCost, ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount, SplashDamageUpgradeBaseCost, SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount, PierceChanceUpgradeBaseCost, PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount, PierceDamageFalloffUpgradeBaseCost, PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount, PelletCountUpgradeBaseCost, PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount, KnockbackStrengthUpgradeBaseCost, KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount, DamageFalloffOverDistanceUpgradeBaseCost, DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount, PercentBonusDamagePerSecUpgradeBaseCost, PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithCritDamageUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier + CritDamageUpgradeAmount,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithExplosionRadiusUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius + ExplosionRadiusUpgradeAmount,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithSplashDamageUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage + SplashDamageUpgradeAmount,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithPierceChanceUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance + PierceChanceUpgradeAmount,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithPierceDamageFalloffUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff + PierceDamageFalloffUpgradeAmount,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithPelletCountUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount + PelletCountUpgradeAmount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithKnockbackStrengthUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength + KnockbackStrengthUpgradeAmount,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithDamageFalloffOverDistanceUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance + DamageFalloffOverDistanceUpgradeAmount,
                PercentBonusDamagePerSec,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithPercentBonusDamagePerSecUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec + PercentBonusDamagePerSecUpgradeAmount,
                SlowEffect,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }

        public TurretBlueprint WithSlowEffectUpgraded()
        {
            return new TurretBlueprint(
                Type,
                Damage,
                FireRate,
                Range,
                RotationSpeed,
                AngleThreshold,
                CritChance,
                CritDamageMultiplier,
                ExplosionRadius,
                SplashDamage,
                PierceChance,
                PierceDamageFalloff,
                PelletCount,
                KnockbackStrength,
                DamageFalloffOverDistance,
                PercentBonusDamagePerSec,
                SlowEffect + SlowEffectUpgradeAmount,
                DamageUpgradeAmount,
                DamageUpgradeBaseCost,
                DamageCostExponentialMultiplier,
                FireRateUpgradeAmount,
                FireRateUpgradeBaseCost,
                FireRateCostExponentialMultiplier,
                CritChanceUpgradeAmount,
                CritChanceUpgradeBaseCost,
                CritChanceCostExponentialMultiplier,
                CritDamageUpgradeAmount,
                CritDamageUpgradeBaseCost,
                CritDamageCostExponentialMultiplier,
                ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost,
                ExplosionRadiusCostExponentialMultiplier,
                SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost,
                SplashDamageCostExponentialMultiplier,
                PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost,
                PierceChanceCostExponentialMultiplier,
                PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost,
                PierceDamageFalloffCostExponentialMultiplier,
                PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost,
                PelletCountCostExponentialMultiplier,
                KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost,
                KnockbackStrengthCostExponentialMultiplier,
                DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost,
                DamageFalloffOverDistanceCostExponentialMultiplier,
                PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost,
                PercentBonusDamagePerSecCostExponentialMultiplier,
                SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost,
                SlowEffectCostExponentialMultiplier
            );
        }



    }
}

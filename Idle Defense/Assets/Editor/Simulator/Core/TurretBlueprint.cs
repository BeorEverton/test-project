// Assets/Editor/Simulation/Core/Blueprints.cs
using Assets.Scripts.SO;      // TurretInfoSO
using Assets.Scripts.Turrets;
using UnityEngine; // TurretType

namespace IdleDefense.Editor.Simulation
{
    public readonly struct TurretBlueprint
    {/*
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

        public readonly int DamageLevel;
        public readonly int FireRateLevel;
        public readonly int CriticalChanceLevel;
        public readonly int CriticalDamageMultiplierLevel;
        public readonly int ExplosionRadiusLevel;
        public readonly int SplashDamageLevel;
        public readonly int PierceChanceLevel;
        public readonly int PierceDamageFalloffLevel;
        public readonly int PelletCountLevel;
        public readonly int KnockbackStrengthLevel;
        public readonly int DamageFalloffOverDistanceLevel;
        public readonly int PercentBonusDamagePerSecLevel;
        public readonly int SlowEffectLevel;
        public float CriticalChanceUpgradeBaseCost => CritChanceUpgradeBaseCost;
        public float CriticalChanceCostExponentialMultiplier => CritChanceCostExponentialMultiplier;

        public float CriticalDamageMultiplierUpgradeBaseCost => CritDamageUpgradeBaseCost;
        public float CriticalDamageCostExponentialMultiplier => CritDamageCostExponentialMultiplier;


        // ---------------------
        // Constructor from SO
        // ---------------------
        public TurretBlueprint(TurretInfoSO so) : this()
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
            float slowEffectCostExponentialMultiplier,
            int damageLevel,
            int fireRateLevel,
            int criticalChanceLevel,
            int criticalDamageMultiplierLevel,
            int explosionRadiusLevel,
            int splashDamageLevel,
            int pierceChanceLevel,
            int pierceDamageFalloffLevel,
            int pelletCountLevel,
            int knockbackStrengthLevel,
            int damageFalloffOverDistanceLevel,
            int percentBonusDamagePerSecLevel,
            int slowEffectLevel
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
            DamageLevel = damageLevel;
            FireRateLevel = fireRateLevel;
            CriticalChanceLevel = criticalChanceLevel;
            CriticalDamageMultiplierLevel = criticalDamageMultiplierLevel;
            ExplosionRadiusLevel = explosionRadiusLevel;
            SplashDamageLevel = splashDamageLevel;
            PierceChanceLevel = pierceChanceLevel;
            PierceDamageFalloffLevel = pierceDamageFalloffLevel;
            PelletCountLevel = pelletCountLevel;
            KnockbackStrengthLevel = knockbackStrengthLevel;
            DamageFalloffOverDistanceLevel = damageFalloffOverDistanceLevel;
            PercentBonusDamagePerSecLevel = percentBonusDamagePerSecLevel;
            SlowEffectLevel = slowEffectLevel;
        }

        // ---------------------
        // Compute DPS
        // ---------------------
        // add this improved DPS calculation in its stead:
        public float DamagePerSecond(float clickBonus = 0f)
        {
            // match BaseTurret.GetDPS logic for default turrets:
            // effective damage per shot with crit  bonus dps
            float critChanceClamped = Mathf.Clamp01(CritChance / 100f);
            float critMultiplierNorm = CritDamageMultiplier / 100f;
            float bonusDpsPercent = PercentBonusDamagePerSec / 100f;
            float effectiveDamage = Damage * (1f + critChanceClamped * (critMultiplierNorm - 1f));
            effectiveDamage *= (1f + bonusDpsPercent);

            // apply click speed bonus to fire rate
            float rate = FireRate * (1f + clickBonus);

            switch (Type)
            {
                case TurretType.Shotgun:
                    // ShotgunTurret.GetDPS: pelletCount × damage × rate 
                    return effectiveDamage * PelletCount * rate;

                case TurretType.Sniper:
                    // SniperTurret.GetDPS: account for average pierce hits 
                    float pierceChanceNorm = Mathf.Clamp01(PierceChance / 100f);
                    float averageHits = 1f + pierceChanceNorm * 0.5f;
                    float sniperDamage = effectiveDamage * averageHits;
                    return sniperDamage * rate;

                default:
                    // BaseTurret.GetDPS for Laser, MachineGun, Missile, etc
                    return effectiveDamage * rate;
            }
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
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier,
                DamageLevel + 1, FireRateLevel,CriticalChanceLevel,CriticalDamageMultiplierLevel,ExplosionRadiusLevel,
                SplashDamageLevel,PierceChanceLevel,PierceDamageFalloffLevel,PelletCountLevel,KnockbackStrengthLevel,                DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel,SlowEffectLevel
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
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier,
                DamageLevel, FireRateLevel +1, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectUpgradeAmount, SlowEffectUpgradeBaseCost, SlowEffectCostExponentialMultiplier,
                DamageLevel, FireRateLevel, CriticalChanceLevel+1, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel+1, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel+1,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel+1, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel+1, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel+1, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel+1, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel, FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel+1, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel+1,
                PercentBonusDamagePerSecLevel, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel+1, SlowEffectLevel
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
                SlowEffectCostExponentialMultiplier,
                DamageLevel , FireRateLevel, CriticalChanceLevel, CriticalDamageMultiplierLevel, ExplosionRadiusLevel,
                SplashDamageLevel, PierceChanceLevel, PierceDamageFalloffLevel, PelletCountLevel, KnockbackStrengthLevel, DamageFalloffOverDistanceLevel,
                PercentBonusDamagePerSecLevel, SlowEffectLevel+1
            );
        }*/



    }
}

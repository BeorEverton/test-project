#nullable enable
using Assets.Scripts.PlayerBase;
using Assets.Scripts.SO;
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems.Save
{
    public static class SaveDataDTOs
    {
        public static GameDataDTO CreateGameDataDTO(int waveNumber)
        {
            var dto = new GameDataDTO
            {
                WaveNumber = waveNumber,
                TutorialStep = GameTutorialManager.Instance != null ? GameTutorialManager.Instance._currentStep : 0,
                MusicVolume = SettingsManager.Instance.MusicVolume,
                SFXVolume = SettingsManager.Instance.SFXVolume,
                PopupsEnabled = SettingsManager.Instance.AllowPopups,
                TooltipsEnabled = SettingsManager.Instance.AllowTooltips,
                MuteAll = SettingsManager.Instance.Mute,
                Currencies = new CurrencyEntry[3]
            };

            var currencyList = new List<CurrencyEntry>();
            foreach (Currency currency in Enum.GetValues(typeof(Currency)))
            {
                currencyList.Add(new CurrencyEntry
                {
                    Currency = currency,
                    Amount = GameManager.Instance.GetCurrency(currency)
                });
            }
            dto.Currencies = currencyList.ToArray();


            return dto;

        }

        public static PlayerInfoDTO CreatePlayerInfoDTO(PlayerBaseStatsInstance player)
        {
            return new PlayerInfoDTO
            {
                MaxHealth = player.MaxHealth,
                RegenAmount = player.RegenAmount,
                RegenInterval = player.RegenInterval,
                MaxHealthUpgradeAmount = player.MaxHealthUpgradeAmount,
                MaxHealthUpgradeBaseCost = player.MaxHealthUpgradeBaseCost,
                MaxHealthLevel = player.MaxHealthLevel,
                RegenAmountUpgradeAmount = player.RegenAmountUpgradeAmount,
                RegenAmountUpgradeBaseCost = player.RegenAmountUpgradeBaseCost,
                RegenAmountLevel = player.RegenAmountLevel,
                RegenIntervalUpgradeAmount = player.RegenIntervalUpgradeAmount,
                RegenIntervalUpgradeBaseCost = player.RegenIntervalUpgradeBaseCost,
                RegenIntervalLevel = player.RegenIntervalLevel
            };
        }

        public static TurretBaseInfoDTO? CreateTurretBaseInfoDTO(TurretStatsInstance? turret)
        {
            if (turret == null)
                return null;

            return new TurretBaseInfoDTO
            {
                BaseDamage = turret.BaseDamage,
                BaseFireRate = turret.BaseFireRate,
                BaseCritChance = turret.BaseCritChance,
                BaseCritDamage = turret.BaseCritDamage,
                DamageCostExponentialMultiplier = turret.DamageCostExponentialMultiplier,
                FireRateCostExponentialMultiplier = turret.FireRateCostExponentialMultiplier,
                CriticalChanceCostExponentialMultiplier = turret.CriticalChanceCostExponentialMultiplier,
                CriticalDamageCostExponentialMultiplier = turret.CriticalDamageCostExponentialMultiplier,
            };
        }

        public static TurretInfoDTO? CreateTurretInfoDTO(TurretStatsInstance? turret)
        {
            if (turret == null)
                return null;

            return new TurretInfoDTO
            {
                IsUnlocked = turret.IsUnlocked,
                Damage = turret.Damage,
                DamageLevel = turret.DamageLevel,
                DamageUpgradeAmount = turret.DamageUpgradeAmount,
                DamageUpgradeBaseCost = turret.DamageUpgradeBaseCost,
                FireRate = turret.FireRate,
                FireRateLevel = turret.FireRateLevel,
                FireRateUpgradeAmount = turret.FireRateUpgradeAmount,
                FireRateUpgradeBaseCost = turret.FireRateUpgradeBaseCost,
                CriticalChance = turret.CriticalChance,
                CriticalChanceLevel = turret.CriticalChanceLevel,
                CriticalChanceUpgradeAmount = turret.CriticalChanceUpgradeAmount,
                CriticalChanceUpgradeBaseCost = turret.CriticalChanceUpgradeBaseCost,
                CriticalDamageMultiplier = turret.CriticalDamageMultiplier,
                CriticalDamageMultiplierLevel = turret.CriticalDamageMultiplierLevel,
                CriticalDamageMultiplierUpgradeAmount = turret.CriticalDamageMultiplierUpgradeAmount,
                CriticalDamageMultiplierUpgradeBaseCost = turret.CriticalDamageMultiplierUpgradeBaseCost,
                ExplosionRadius = turret.ExplosionRadius,
                ExplosionRadiusLevel = turret.ExplosionRadiusLevel,
                ExplosionRadiusUpgradeAmount = turret.ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost = turret.ExplosionRadiusUpgradeBaseCost,
                SplashDamage = turret.SplashDamage,
                SplashDamageLevel = turret.SplashDamageLevel,
                SplashDamageUpgradeAmount = turret.SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost = turret.SplashDamageUpgradeBaseCost,
                PierceChance = turret.PierceChance,
                PierceChanceLevel = turret.PierceChanceLevel,
                PierceChanceUpgradeAmount = turret.PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost = turret.PierceChanceUpgradeBaseCost,
                PierceDamageFalloff = turret.PierceDamageFalloff,
                PierceDamageFalloffLevel = turret.PierceDamageFalloffLevel,
                PierceDamageFalloffUpgradeAmount = turret.PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost = turret.PierceDamageFalloffUpgradeBaseCost,
                PelletCount = turret.PelletCount,
                PelletCountLevel = turret.PelletCountLevel,
                PelletCountUpgradeAmount = turret.PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost = turret.PelletCountUpgradeBaseCost,
                DamageFalloffOverDistance = turret.DamageFalloffOverDistance,
                DamageFalloffOverDistanceLevel = turret.DamageFalloffOverDistanceLevel,
                DamageFalloffOverDistanceUpgradeAmount = turret.DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost = turret.DamageFalloffOverDistanceUpgradeBaseCost,
                PercentBonusDamagePerSec = turret.PercentBonusDamagePerSec,
                PercentBonusDamagePerSecLevel = turret.PercentBonusDamagePerSecLevel,
                PercentBonusDamagePerSecUpgradeAmount = turret.PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost = turret.PercentBonusDamagePerSecUpgradeBaseCost,
                SlowEffect = turret.SlowEffect,
                SlowEffectLevel = turret.SlowEffectLevel,
                SlowEffectUpgradeAmount = turret.SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost = turret.SlowEffectUpgradeBaseCost,
                RotationSpeed = turret.RotationSpeed,
                AngleThreshold = turret.AngleThreshold,
                Range = turret.Range

            };
        }

        public static StatsDTO CreateStatsDTO()
        {
            return new StatsDTO
            {
                TotalDamage = StatsManager.Instance.TotalDamage,
                MaxZone = StatsManager.Instance.MaxZone,
                TotalZonesSecured = StatsManager.Instance.TotalZonesSecured,
                EnemiesKilled = StatsManager.Instance.EnemiesKilled,
                BossesKilled = StatsManager.Instance.BossesKilled,
                MoneySpent = StatsManager.Instance.MoneySpent,
                UpgradeAmount = StatsManager.Instance.UpgradeAmount,
                TotalDamageTaken = StatsManager.Instance.TotalDamageTaken,
                TotalHealthRepaired = StatsManager.Instance.TotalHealthRepaired,
                MissionsFailed = StatsManager.Instance.MissionsFailed,
                SpeedBoostClicks = StatsManager.Instance.SpeedBoostClicks,
                MachineGunDamage = StatsManager.Instance.MachineGunDamage,
                ShotgunDamage = StatsManager.Instance.ShotgunDamage,
                SniperDamage = StatsManager.Instance.SniperDamage,
                MissileLauncherDamage = StatsManager.Instance.MissileLauncherDamage,
                LaserDamage = StatsManager.Instance.LaserDamage,
                GameTime = StatsManager.Instance.GameTime
            };
        }
    }

    public static class LoadDataDTOs
    {
        public static PlayerBaseStatsInstance CreatePlayerBaseSO(PlayerInfoDTO playerInfo)
        {
            PlayerBaseStatsInstance playerStats = new()
            {
                MaxHealth = playerInfo.MaxHealth,
                RegenAmount = playerInfo.RegenAmount,
                RegenDelay = playerInfo.RegenDelay,
                RegenInterval = playerInfo.RegenInterval,
                MaxHealthUpgradeAmount = playerInfo.MaxHealthUpgradeAmount,
                MaxHealthUpgradeBaseCost = playerInfo.MaxHealthUpgradeBaseCost,
                MaxHealthLevel = playerInfo.MaxHealthLevel,
                RegenAmountUpgradeAmount = playerInfo.RegenAmountUpgradeAmount,
                RegenAmountUpgradeBaseCost = playerInfo.RegenAmountUpgradeBaseCost,
                RegenAmountLevel = playerInfo.RegenAmountLevel,
                RegenIntervalUpgradeAmount = playerInfo.RegenIntervalUpgradeAmount,
                RegenIntervalUpgradeBaseCost = playerInfo.RegenIntervalUpgradeBaseCost,
                RegenIntervalLevel = playerInfo.RegenIntervalLevel
            };

            return playerStats;
        }

        public static TurretStatsInstance CreateTurretStatsInstance(TurretInfoDTO turret, TurretBaseInfoDTO baseInfo)
        {
            return new TurretStatsInstance
            {
                IsUnlocked = turret.IsUnlocked,
                BaseDamage = baseInfo.BaseDamage,
                BaseFireRate = baseInfo.BaseFireRate,
                BaseCritChance = baseInfo.BaseCritChance,
                BaseCritDamage = baseInfo.BaseCritDamage,
                DamageCostExponentialMultiplier = baseInfo.DamageCostExponentialMultiplier,
                FireRateCostExponentialMultiplier = baseInfo.FireRateCostExponentialMultiplier,
                CriticalChanceCostExponentialMultiplier = baseInfo.CriticalChanceCostExponentialMultiplier,
                CriticalDamageCostExponentialMultiplier = baseInfo.CriticalDamageCostExponentialMultiplier,

                RotationSpeed = turret.RotationSpeed,
                RotationSpeedUpgradeAmount = turret.RotationSpeedUpgradeAmount,
                RotationSpeedUpgradeBaseCost = turret.RotationSpeedUpgradeBaseCost,
                AngleThreshold = turret.AngleThreshold,

                Range = turret.Range,
                RangeUpgradeAmount = turret.RangeUpgradeAmount,
                RangeUpgradeBaseCost = turret.RangeUpgradeBaseCost,
                RangeLevel = turret.RangeLevel,

                Damage = turret.Damage,
                DamageLevel = turret.DamageLevel,
                DamageUpgradeAmount = turret.DamageUpgradeAmount,
                DamageUpgradeBaseCost = turret.DamageUpgradeBaseCost,

                FireRate = turret.FireRate,
                FireRateLevel = turret.FireRateLevel,
                FireRateUpgradeAmount = turret.FireRateUpgradeAmount,
                FireRateUpgradeBaseCost = turret.FireRateUpgradeBaseCost,

                ArmorPenetration = turret.ArmorPenetration,
                ArmorPenetrationLevel = turret.ArmorPenetrationLevel,
                ArmorPenetrationUpgradeAmount = turret.ArmorPenetrationUpgradeAmount,
                ArmorPenetrationUpgradeBaseCost = turret.ArmorPenetrationUpgradeBaseCost,

                CanHitFlying = turret.CanHitFlying,

                CriticalChance = turret.CriticalChance,
                CriticalChanceLevel = turret.CriticalChanceLevel,
                CriticalChanceUpgradeAmount = turret.CriticalChanceUpgradeAmount,
                CriticalChanceUpgradeBaseCost = turret.CriticalChanceUpgradeBaseCost,
                CriticalDamageMultiplier = turret.CriticalDamageMultiplier,
                CriticalDamageMultiplierLevel = turret.CriticalDamageMultiplierLevel,
                CriticalDamageMultiplierUpgradeAmount = turret.CriticalDamageMultiplierUpgradeAmount,
                CriticalDamageMultiplierUpgradeBaseCost = turret.CriticalDamageMultiplierUpgradeBaseCost,

                ExplosionRadius = turret.ExplosionRadius,
                ExplosionRadiusLevel = turret.ExplosionRadiusLevel,
                ExplosionRadiusUpgradeAmount = turret.ExplosionRadiusUpgradeAmount,
                ExplosionRadiusUpgradeBaseCost = turret.ExplosionRadiusUpgradeBaseCost,
                SplashDamage = turret.SplashDamage,
                SplashDamageLevel = turret.SplashDamageLevel,
                SplashDamageUpgradeAmount = turret.SplashDamageUpgradeAmount,
                SplashDamageUpgradeBaseCost = turret.SplashDamageUpgradeBaseCost,

                PierceChance = turret.PierceChance,
                PierceChanceLevel = turret.PierceChanceLevel,
                PierceChanceUpgradeAmount = turret.PierceChanceUpgradeAmount,
                PierceChanceUpgradeBaseCost = turret.PierceChanceUpgradeBaseCost,
                PierceDamageFalloff = turret.PierceDamageFalloff,
                PierceDamageFalloffLevel = turret.PierceDamageFalloffLevel,
                PierceDamageFalloffUpgradeAmount = turret.PierceDamageFalloffUpgradeAmount,
                PierceDamageFalloffUpgradeBaseCost = turret.PierceDamageFalloffUpgradeBaseCost,

                PelletCount = turret.PelletCount,
                PelletCountLevel = turret.PelletCountLevel,
                PelletCountUpgradeAmount = turret.PelletCountUpgradeAmount,
                PelletCountUpgradeBaseCost = turret.PelletCountUpgradeBaseCost,

                KnockbackStrength = turret.KnockbackStrength,
                KnockbackStrengthLevel = turret.KnockbackStrengthLevel,
                KnockbackStrengthUpgradeAmount = turret.KnockbackStrengthUpgradeAmount,
                KnockbackStrengthUpgradeBaseCost = turret.KnockbackStrengthUpgradeBaseCost,

                DamageFalloffOverDistance = turret.DamageFalloffOverDistance,
                DamageFalloffOverDistanceLevel = turret.DamageFalloffOverDistanceLevel,
                DamageFalloffOverDistanceUpgradeAmount = turret.DamageFalloffOverDistanceUpgradeAmount,
                DamageFalloffOverDistanceUpgradeBaseCost = turret.DamageFalloffOverDistanceUpgradeBaseCost,

                PercentBonusDamagePerSec = turret.PercentBonusDamagePerSec,
                PercentBonusDamagePerSecLevel = turret.PercentBonusDamagePerSecLevel,
                PercentBonusDamagePerSecUpgradeAmount = turret.PercentBonusDamagePerSecUpgradeAmount,
                PercentBonusDamagePerSecUpgradeBaseCost = turret.PercentBonusDamagePerSecUpgradeBaseCost,

                SlowEffect = turret.SlowEffect,
                SlowEffectLevel = turret.SlowEffectLevel,
                SlowEffectUpgradeAmount = turret.SlowEffectUpgradeAmount,
                SlowEffectUpgradeBaseCost = turret.SlowEffectUpgradeBaseCost,

                BounceCount = turret.BounceCount,
                BounceCountLevel = turret.BounceCountLevel,
                BounceCountUpgradeAmount = turret.BounceCountUpgradeAmount,
                BounceCountUpgradeBaseCost = turret.BounceCountUpgradeBaseCost,
                BounceRange = turret.BounceRange,
                BounceRangeLevel = turret.BounceRangeLevel,
                BounceRangeUpgradeAmount = turret.BounceRangeUpgradeAmount,
                BounceRangeUpgradeBaseCost = turret.BounceRangeUpgradeBaseCost,
                BounceDelay = turret.BounceDelay,
                BounceDelayLevel = turret.BounceDelayLevel,
                BounceDelayUpgradeAmount = turret.BounceDelayUpgradeAmount,
                BounceDelayUpgradeBaseCost = turret.BounceDelayUpgradeBaseCost,
                BounceDamagePct = turret.BounceDamagePct,
                BounceDamagePctLevel = turret.BounceDamagePctLevel,
                BounceDamagePctUpgradeAmount = turret.BounceDamagePctUpgradeAmount,
                BounceDamagePctUpgradeBaseCost = turret.BounceDamagePctUpgradeBaseCost,
                
                ConeAngle = turret.ConeAngle,
                ConeAngleLevel = turret.ConeAngleLevel,
                ConeAngleUpgradeAmount = turret.ConeAngleUpgradeAmount,
                ConeAngleUpgradeBaseCost = turret.ConeAngleUpgradeBaseCost,
                
                ExplosionDelay = turret.ExplosionDelay,
                ExplosionDelayLevel = turret.ExplosionDelayLevel,
                ExplosionDelayUpgradeAmount = turret.ExplosionDelayUpgradeAmount,
                ExplosionDelayUpgradeBaseCost = turret.ExplosionDelayUpgradeBaseCost,
                
                AheadDistance = turret.AheadDistance,
                AheadDistanceLevel = turret.AheadDistanceLevel,
                AheadDistanceUpgradeAmount = turret.AheadDistanceUpgradeAmount,
                AheadDistanceUpgradeBaseCost = turret.AheadDistanceUpgradeBaseCost,
                TrapPrefab = turret.TrapPrefab,
                MaxTrapsActive = turret.MaxTrapsActive,
                MaxTrapsActiveLevel = turret.MaxTrapsActiveLevel,
                MaxTrapsActiveUpgradeAmount = turret.MaxTrapsActiveUpgradeAmount,
                MaxTrapsActiveUpgradeBaseCost = turret.MaxTrapsActiveUpgradeBaseCost
            };
        }
    }
}

[Serializable]
public class GameDataDTO
{
    public int WaveNumber;
    public ulong Money;
    public CurrencyEntry[] Currencies;
    public int TutorialStep;

    public float MusicVolume;
    public float SFXVolume;
    public bool MuteAll;
    public bool PopupsEnabled;
    public bool TooltipsEnabled;
}

[Serializable]
public class CurrencyEntry
{
    public Currency Currency;
    public ulong Amount;
}


[Serializable]
public class PlayerInfoDTO
{
    public float MaxHealth;
    public float RegenAmount;
    public float RegenDelay;
    public float RegenInterval;

    public float MaxHealthUpgradeAmount;
    public float MaxHealthUpgradeBaseCost;
    public int MaxHealthLevel;

    public float RegenAmountUpgradeAmount;
    public float RegenAmountUpgradeBaseCost;
    public int RegenAmountLevel;

    public float RegenIntervalUpgradeAmount;
    public float RegenIntervalUpgradeBaseCost;
    public int RegenIntervalLevel;
}

[Serializable]
public class TurretBaseInfoDTO
{
    public float BaseDamage;
    public float BaseFireRate;
    public float BaseCritChance;
    public float BaseCritDamage;
    public float DamageCostExponentialMultiplier;
    public float FireRateCostExponentialMultiplier;
    public float CriticalChanceCostExponentialMultiplier;
    public float CriticalDamageCostExponentialMultiplier;
}

[Serializable]
public class TurretInfoDTO
{
    [Header("Base Turret")]
    [Tooltip("If true, this turret can target flying enemies.")]
    public bool CanHitFlying;

    [Tooltip("What type of turret is this?")]
    public TurretType TurretType;
    [Tooltip("Is the turret unlocked?")]
    public bool IsUnlocked;

    [Tooltip("How fast the turretHead rotates towards the target")]
    public float RotationSpeed;
    public float RotationSpeedUpgradeAmount;
    public float RotationSpeedUpgradeBaseCost;
    public int RotationSpeedLevel;
    public float RotationSpeedCostExponentialMultiplier;

    [Tooltip("Angle threshold between target and turret before being able to shoot")]
    public float AngleThreshold;

    [Tooltip("Amount of damage dealt per hit")]
    public float Damage;
    public int DamageLevel;
    [Tooltip("Exponential damage upgrade amount")]
    public float DamageUpgradeAmount;
    public float DamageUpgradeBaseCost;
    public float DamageCostExponentialMultiplier;

    [Tooltip("Amount of shots per second")]
    public float FireRate;
    public int FireRateLevel;
    [Tooltip("Exponential fire rate upgrade amount")]
    public float FireRateUpgradeAmount;
    public float FireRateUpgradeBaseCost;
    public float FireRateCostExponentialMultiplier;

    [Tooltip("How far the turret shoots")]
    public float Range;
    public float RangeUpgradeAmount;
    public float RangeUpgradeBaseCost;
    public int RangeLevel;
    public float RangeCostExponentialMultiplier;

    [Header("Armor Penetration")]
    [Tooltip("Percent of enemy armor ignored by this turret (0-100).")]
    public float ArmorPenetration;
    public int ArmorPenetrationLevel;
    [Tooltip("Linear upgrade amount for Armor Penetration (percent).")]
    public float ArmorPenetrationUpgradeAmount;
    public float ArmorPenetrationUpgradeBaseCost;
    public float ArmorPenetrationCostExponentialMultiplier;

    [Header("Machine Gun Turret")]
    [Tooltip("Chance for a critical attack in Percent")]
    public float CriticalChance;
    public int CriticalChanceLevel;
    [Tooltip("Linear critical chance upgrade amount")]
    public float CriticalChanceUpgradeAmount;
    public float CriticalChanceUpgradeBaseCost;
    public float CriticalChanceCostExponentialMultiplier;

    [Tooltip("Multiplier for critical damage in Percent")]
    public float CriticalDamageMultiplier;
    public int CriticalDamageMultiplierLevel;
    [Tooltip("Linear critical damage upgrade amount")]
    public float CriticalDamageMultiplierUpgradeAmount;
    public float CriticalDamageMultiplierUpgradeBaseCost;
    public float CriticalDamageCostExponentialMultiplier;

    [Header("Missile Launcher Turret")]
    [Tooltip("Radius of the explosion, used by missile launcher, trap, multi target")]
    public float ExplosionRadius;
    public int ExplosionRadiusLevel;
    public float ExplosionRadiusUpgradeAmount;
    public float ExplosionRadiusUpgradeBaseCost;
    public float ExplosionRadiusCostExponentialMultiplier;

    [Tooltip("Amount of damage dealt to adjacent enemies in the explosion radius")]
    public float SplashDamage;
    public int SplashDamageLevel;
    public float SplashDamageUpgradeAmount;
    public float SplashDamageUpgradeBaseCost;
    public float SplashDamageCostExponentialMultiplier;

    [Header("Sniper Turret")]
    [Tooltip("Chance for the bullet to pierce through an enemy")]
    public float PierceChance;
    public int PierceChanceLevel;
    public float PierceChanceUpgradeAmount;
    public float PierceChanceUpgradeBaseCost;
    public float PierceChanceCostExponentialMultiplier;

    [Tooltip("Amount of damage falloff for each enemy the bullet pierces through")]
    public float PierceDamageFalloff;
    public int PierceDamageFalloffLevel;
    public float PierceDamageFalloffUpgradeAmount;
    public float PierceDamageFalloffUpgradeBaseCost;
    public float PierceDamageFalloffCostExponentialMultiplier;

    [Header("Shotgun Turret")]
    [Tooltip("Amount of pellets shot per shot, used by shotgun and multi target")]
    public int PelletCount;
    public int PelletCountLevel;
    public int PelletCountUpgradeAmount;
    public float PelletCountUpgradeBaseCost;
    public float PelletCountCostExponentialMultiplier;

    [Header("Knockback Settings")]
    public float KnockbackStrength;
    public int KnockbackStrengthLevel;
    public float KnockbackStrengthUpgradeAmount;
    public float KnockbackStrengthUpgradeBaseCost;
    public float KnockbackStrengthCostExponentialMultiplier;

    [Tooltip("Amount of damage falloff over distance, measured in 1 unit")]
    public float DamageFalloffOverDistance;
    public int DamageFalloffOverDistanceLevel;
    public float DamageFalloffOverDistanceUpgradeAmount;
    public float DamageFalloffOverDistanceUpgradeBaseCost;
    public float DamageFalloffOverDistanceCostExponentialMultiplier;

    [Header("Laser Turret")]
    [Tooltip("Amount of damage added to initial Damage per second active on the same target")]
    public float PercentBonusDamagePerSec;
    public int PercentBonusDamagePerSecLevel;
    public float PercentBonusDamagePerSecUpgradeAmount;
    public float PercentBonusDamagePerSecUpgradeBaseCost;
    public float PercentBonusDamagePerSecCostExponentialMultiplier;

    [Tooltip("Amount of slow effect applied to the target")]
    public float SlowEffect;
    public int SlowEffectLevel;
    public float SlowEffectUpgradeAmount;
    public float SlowEffectUpgradeBaseCost;
    public float SlowEffectCostExponentialMultiplier;

    [Header("Bounce Pattern")]
    [Tooltip("Number of times the projectile or effect can bounce to a new target")]
    public int BounceCount;
    public int BounceCountLevel;
    public float BounceCountUpgradeAmount;
    public float BounceCountUpgradeBaseCost;
    public float BounceCountCostExponentialMultiplier;

    [Tooltip("Maximum range in units for each bounce to find a new target")]
    public float BounceRange;
    public int BounceRangeLevel;
    public float BounceRangeUpgradeAmount;
    public float BounceRangeUpgradeBaseCost;
    public float BounceRangeCostExponentialMultiplier;

    [Tooltip("Delay in seconds between each bounce hit")]
    public float BounceDelay;
    public int BounceDelayLevel;
    public float BounceDelayUpgradeAmount;
    public float BounceDelayUpgradeBaseCost;
    public float BounceDelayCostExponentialMultiplier;

    [Tooltip("How much damage it loses per bounce - percentage")]
    public float BounceDamagePct;
    public int BounceDamagePctLevel;
    public float BounceDamagePctUpgradeAmount;
    public float BounceDamagePctUpgradeBaseCost;
    public float BounceDamagePctCostExponentialMultiplier;

    [Header("Cone AOE Pattern")]
    [Tooltip("Angle in degrees for the cone attack area")]
    public float ConeAngle;
    public int ConeAngleLevel;
    public float ConeAngleUpgradeAmount;
    public float ConeAngleUpgradeBaseCost;
    public float ConeAngleCostExponentialMultiplier;

    [Header("Delayed AOE Pattern")]
    [Tooltip("Delay in seconds before the explosion triggers")]
    public float ExplosionDelay;
    public int ExplosionDelayLevel;
    public float ExplosionDelayUpgradeAmount;
    public float ExplosionDelayUpgradeBaseCost;
    public float ExplosionDelayCostExponentialMultiplier;

    [Header("Trap Pattern")]
    [Tooltip("Cells in front of the enemy to place trap when targeting them")]
    public float AheadDistance;
    public int AheadDistanceLevel;
    public float AheadDistanceUpgradeAmount;
    public float AheadDistanceUpgradeBaseCost;
    public float AheadDistanceCostExponentialMultiplier;

    [Tooltip("The prefab used for traps placed by this turret")]
    public GameObject TrapPrefab;

    [Tooltip("Maximum number of active traps this turret can have at once")]
    public int MaxTrapsActive;
    public int MaxTrapsActiveLevel;
    public float MaxTrapsActiveUpgradeAmount;
    public float MaxTrapsActiveUpgradeBaseCost;
    public float MaxTrapsActiveCostExponentialMultiplier;
}

[Serializable]
public class TurretInventoryDTO
{
    public List<TurretStatsInstance> Owned;
    public List<int> EquippedIds;          // -1  = runtime copy
    public List<TurretStatsInstance> EquippedRuntimeStats;   
    public List<EquippedTurretDTO> EquippedTurrets;
    public List<TurretType> UnlockedTypes;
    public List<bool> SlotPurchased;
}

[Serializable]
public class EquippedTurretDTO
{
    public TurretType Type;
    public TurretInfoDTO PermanentStats;
    public TurretInfoDTO RuntimeStats;
    public TurretBaseInfoDTO PermanentBase;
    public TurretBaseInfoDTO RuntimeBase;
    public int SlotIndex;
}


[Serializable]
public class StatsDTO
{
    public double TotalDamage;
    public int MaxZone;
    public int TotalZonesSecured;
    public int EnemiesKilled;
    public int BossesKilled;
    public double MoneySpent;
    public int UpgradeAmount;
    public double TotalDamageTaken;
    public double TotalHealthRepaired;
    public int MissionsFailed;
    public int SpeedBoostClicks;
    public double GameTime;

    public double MachineGunDamage;
    public double ShotgunDamage;
    public double SniperDamage;
    public double MissileLauncherDamage;
    public double LaserDamage;
}
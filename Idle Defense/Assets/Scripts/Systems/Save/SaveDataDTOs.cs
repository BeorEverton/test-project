#nullable enable
using Assets.Scripts.PlayerBase;
using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems.Save
{
    public static class SaveDataDTOs
    {
        public static GameDataDTO CreateGameDataDTO(int waveNumber, ulong money)
        {
            return new GameDataDTO
            {
                WaveNumber = waveNumber,
                Money = money,
                TutorialStep = GameTutorialManager.Instance != null ? GameTutorialManager.Instance._currentStep : 0
            };
        }

        public static PlayerInfoDTO CreatePlayerInfoDTO(PlayerBaseStatsInstance player)
        {
            return new PlayerInfoDTO
            {
                MaxHealth = player.MaxHealth,
                RegenAmount = player.RegenAmount,
                RegenDelay = player.RegenDelay,
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
                AngleThreshold = turret.AngleThreshold
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
                LaserDamage = StatsManager.Instance.LaserDamage
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

                DamageLevel = turret.DamageLevel,
                Damage = turret.Damage,
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
                AngleThreshold = turret.AngleThreshold
            };
        }
    }
}

[Serializable]
public class GameDataDTO
{
    public int WaveNumber;
    public ulong Money;
    public int TutorialStep;
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
    public bool IsUnlocked;

    public float Damage;
    public int DamageLevel;
    public float DamageUpgradeAmount;
    public float DamageUpgradeBaseCost;

    public float FireRate;
    public int FireRateLevel;
    public float FireRateUpgradeAmount;
    public float FireRateUpgradeBaseCost;

    public float CriticalChance;
    public int CriticalChanceLevel;
    public float CriticalChanceUpgradeAmount;
    public float CriticalChanceUpgradeBaseCost;

    public float CriticalDamageMultiplier;
    public int CriticalDamageMultiplierLevel;
    public float CriticalDamageMultiplierUpgradeAmount;
    public float CriticalDamageMultiplierUpgradeBaseCost;

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

    public float PercentBonusDamagePerSec;
    public int PercentBonusDamagePerSecLevel;
    public float PercentBonusDamagePerSecUpgradeAmount;
    public float PercentBonusDamagePerSecUpgradeBaseCost;

    public float SlowEffect;
    public int SlowEffectLevel;
    public float SlowEffectUpgradeAmount;
    public float SlowEffectUpgradeBaseCost;

    public float RotationSpeed;
    public float AngleThreshold;
}

[Serializable]
public class TurretInventoryDTO
{
    public List<TurretStatsInstance> Owned;
    public List<int> EquippedIds;
    public List<TurretType> UnlockedTypes;
    public List<bool> SlotPurchased;
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

    public double MachineGunDamage;
    public double ShotgunDamage;
    public double SniperDamage;
    public double MissileLauncherDamage;
    public double LaserDamage;
}
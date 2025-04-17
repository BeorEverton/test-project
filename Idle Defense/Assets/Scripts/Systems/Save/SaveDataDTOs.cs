#nullable enable
using Assets.Scripts.SO;
using Assets.Scripts.Turrets;
using System;
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
            };
        }

        public static PlayerInfoDTO CreatePlayerInfoDTO(PlayerBaseSO player)
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
    }

    public static class LoadDataDTOs
    {
        public static PlayerBaseSO CreatePlayerBaseSO(PlayerInfoDTO playerInfo)
        {
            return new PlayerBaseSO
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
        }

        public static TurretStatsInstance CreateTurretStatsInstance(TurretInfoDTO turret)
        {
            return new TurretStatsInstance
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
    }
}

[Serializable]
public class GameDataDTO
{
    public int WaveNumber;
    public ulong Money;
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
    public float MaxHealthLevel;

    public float RegenAmountUpgradeAmount;
    public float RegenAmountUpgradeBaseCost;
    public float RegenAmountLevel;

    public float RegenIntervalUpgradeAmount;
    public float RegenIntervalUpgradeBaseCost;
    public float RegenIntervalLevel;
}

[Serializable]
public class TurretInfoDTO
{
    public bool IsUnlocked;

    public float Damage;
    public float DamageLevel;
    public float DamageUpgradeAmount;
    public float DamageUpgradeBaseCost;

    public float FireRate;
    public float FireRateLevel;
    public float FireRateUpgradeAmount;
    public float FireRateUpgradeBaseCost;

    public float CriticalChance;
    public float CriticalChanceLevel;
    public float CriticalChanceUpgradeAmount;
    public float CriticalChanceUpgradeBaseCost;

    public float CriticalDamageMultiplier;
    public float CriticalDamageMultiplierLevel;
    public float CriticalDamageMultiplierUpgradeAmount;
    public float CriticalDamageMultiplierUpgradeBaseCost;

    public float ExplosionRadius;
    public float ExplosionRadiusLevel;
    public float ExplosionRadiusUpgradeAmount;
    public float ExplosionRadiusUpgradeBaseCost;

    public float SplashDamage;
    public float SplashDamageLevel;
    public float SplashDamageUpgradeAmount;
    public float SplashDamageUpgradeBaseCost;

    public float PierceChance;
    public int PierceChanceLevel;
    public float PierceChanceUpgradeAmount;
    public float PierceChanceUpgradeBaseCost;

    public float PierceDamageFalloff;
    public float PierceDamageFalloffLevel;
    public float PierceDamageFalloffUpgradeAmount;
    public float PierceDamageFalloffUpgradeBaseCost;

    public int PelletCount;
    public int PelletCountLevel;
    public int PelletCountUpgradeAmount;
    public float PelletCountUpgradeBaseCost;

    public float DamageFalloffOverDistance;
    public float DamageFalloffOverDistanceLevel;
    public float DamageFalloffOverDistanceUpgradeAmount;
    public float DamageFalloffOverDistanceUpgradeBaseCost;

    public float PercentBonusDamagePerSec;
    public float PercentBonusDamagePerSecLevel;
    public float PercentBonusDamagePerSecUpgradeAmount;
    public float PercentBonusDamagePerSecUpgradeBaseCost;

    public float SlowEffect;
    public float SlowEffectLevel;
    public float SlowEffectUpgradeAmount;
    public float SlowEffectUpgradeBaseCost;

    public float RotationSpeed;
    public float AngleThreshold;
}
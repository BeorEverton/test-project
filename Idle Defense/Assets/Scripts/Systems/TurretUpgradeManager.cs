using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.UpgradeSystem;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Systems
{
    public class TurretUpgradeManager : MonoBehaviour
    {
        public static event Action OnAnyTurretUpgraded;

        [Header("Cost Scaling Settings")]
        [SerializeField] private int hybridThreshold = 50;
        [SerializeField] private float quadraticFactor = 0.1f;
        [SerializeField] private float exponentialPower = 1.15f;

        private Dictionary<TurretUpgradeType, TurretUpgrade> _turretUpgrades;

        private void Start()
        {
            InitializeUpgrades();
        }

        #region UpgradeInitialization
        private void InitializeUpgrades()
        {
            _turretUpgrades = new Dictionary<TurretUpgradeType, TurretUpgrade>
            {
                [TurretUpgradeType.Damage] = new()
                {
                    GetCurrentValue = t => t.Damage,
                    SetCurrentValue = (t, v) => t.Damage = v,
                    GetLevel = t => t.DamageLevel,
                    SetLevel = (t, v) => t.DamageLevel = v,
                    GetBaseStat = t => t.BaseDamage,
                    GetBaseCost = t => t.DamageUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageUpgradeAmount,
                    GetCostMultiplier = t => t.DamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.Damage),
                    GetDisplayStrings = t =>
                        {
                            float current = t.Damage;
                            float nextLevel = t.DamageLevel + 1f;
                            float nextDamage = t.BaseDamage * Mathf.Pow(t.DamageUpgradeAmount, nextLevel) + nextLevel;
                            float bonus = nextDamage - current;
                            float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.Damage);

                            return (UIManager.AbbreviateNumber(current),
                                $"+{UIManager.AbbreviateNumber(bonus)}",
                                $"${UIManager.AbbreviateNumber(cost)}");
                        }
                },
                [TurretUpgradeType.FireRate] = new()
                {
                    GetCurrentValue = t => t.FireRate,
                    SetCurrentValue = (t, v) => t.FireRate = v,
                    GetLevel = t => t.FireRateLevel,
                    SetLevel = (t, v) => t.FireRateLevel = v,
                    GetBaseStat = t => t.BaseFireRate,
                    GetBaseCost = t => t.FireRateUpgradeBaseCost,
                    GetUpgradeAmount = t => t.FireRateUpgradeAmount,
                    GetCostMultiplier = t => t.FireRateCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.FireRate),
                    GetDisplayStrings = t =>
                    {
                        float currentFireRate = t.FireRate;
                        float bonusFireRate = t.FireRateUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.FireRate);

                        return (
                        $"{currentFireRate:F2}/s",
                        $"+{bonusFireRate:F2}/s",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.CriticalChance] = new()
                {
                    GetCurrentValue = t => t.CriticalChance,
                    SetCurrentValue = (t, v) => t.CriticalChance = v,
                    GetLevel = t => t.CriticalChanceLevel,
                    SetLevel = (t, v) => t.CriticalChanceLevel = v,
                    GetBaseStat = t => t.BaseCritChance,
                    GetBaseCost = t => t.CriticalChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.CriticalChanceUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalChanceCostExponentialMultiplier,
                    GetMaxValue = t => 50f,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost(t, TurretUpgradeType.CriticalChance),
                    GetDisplayStrings = t =>
                    {
                        float current = t.CriticalChance;
                        float bonus = t.CriticalChanceUpgradeAmount;
                        float cost = GetExponentialCost(t, TurretUpgradeType.CriticalChance);

                        if (current >= 50f)
                            return ($"{(int)current}%", "Max", "");

                        return (
                            $"{(int)current}%",
                            $"+{(int)bonus}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.CriticalDamageMultiplier] = new()
                {
                    GetCurrentValue = t => t.CriticalDamageMultiplier,
                    SetCurrentValue = (t, v) => t.CriticalDamageMultiplier = v,
                    GetLevel = t => t.CriticalDamageMultiplierLevel,
                    SetLevel = (t, v) => t.CriticalDamageMultiplierLevel = v,
                    GetBaseStat = t => t.BaseCritDamage,
                    GetBaseCost = t => t.CriticalDamageMultiplierUpgradeBaseCost,
                    GetUpgradeAmount = t => t.CriticalDamageMultiplierUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalDamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier),
                    GetDisplayStrings = t =>
                    {
                        float current = t.CriticalDamageMultiplier;
                        float bonus = t.CriticalDamageMultiplierUpgradeAmount;
                        float cost = GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier);

                        return (
                            $"{(int)current}%",
                            $"+{(int)bonus}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.ExplosionRadius] = new()
                {
                    GetCurrentValue = t => t.ExplosionRadius,
                    SetCurrentValue = (t, v) => t.ExplosionRadius = v,
                    GetLevel = t => t.ExplosionRadiusLevel,
                    SetLevel = (t, v) => t.ExplosionRadiusLevel = v,
                    GetBaseStat = t => t.ExplosionRadius,
                    GetBaseCost = t => t.ExplosionRadiusUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ExplosionRadiusUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.ExplosionRadius),
                    GetDisplayStrings = t =>
                    {
                        float current = t.ExplosionRadius;
                        float bonus = t.ExplosionRadiusUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.ExplosionRadius);

                        if (t.ExplosionRadius >= 5f)
                            return ($"{current:F1}", "Max", "");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.SplashDamage] = new()
                {
                    GetCurrentValue = t => t.SplashDamage,
                    SetCurrentValue = (t, v) => t.SplashDamage = v,
                    GetLevel = t => t.SplashDamageLevel,
                    SetLevel = (t, v) => t.SplashDamageLevel = v,
                    GetBaseStat = t => t.SplashDamage,
                    GetBaseCost = t => t.SplashDamageUpgradeBaseCost,
                    GetUpgradeAmount = t => t.SplashDamageUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.SplashDamage),
                    GetDisplayStrings = t =>
                    {
                        float current = t.SplashDamage;
                        float bonus = t.SplashDamageUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.SplashDamage);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.PierceChance] = new()
                {
                    GetCurrentValue = t => t.PierceChance,
                    SetCurrentValue = (t, v) => t.PierceChance = v,
                    GetLevel = t => t.PierceChanceLevel,
                    SetLevel = (t, v) => t.PierceChanceLevel = v,
                    GetBaseStat = t => t.PierceChance,
                    GetBaseCost = t => t.PierceChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceChanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.PierceChance),
                    GetDisplayStrings = t =>
                    {
                        float current = t.PierceChance;
                        float bonus = t.PierceChanceUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.PierceChance);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.PierceDamageFalloff] = new()
                {
                    GetCurrentValue = t => t.PierceDamageFalloff,
                    SetCurrentValue = (t, v) => t.PierceDamageFalloff = v,
                    GetLevel = t => t.PierceDamageFalloffLevel,
                    SetLevel = (t, v) => t.PierceDamageFalloffLevel = v,
                    GetBaseStat = t => t.PierceDamageFalloff,
                    GetBaseCost = t => t.PierceDamageFalloffUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceDamageFalloffUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.PierceDamageFalloff),
                    GetDisplayStrings = t =>
                    {
                        float currentFalloff = t.PierceDamageFalloff;
                        float currentRetained = 100f - currentFalloff;

                        float nextFalloff = Mathf.Max(0f, currentFalloff - t.PierceDamageFalloffUpgradeAmount);
                        float nextRetained = 100f - nextFalloff;

                        float bonus = nextRetained - currentRetained;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.PierceDamageFalloff);

                        return (
                            $"{currentRetained:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.PelletCount] = new()
                {
                    GetCurrentValue = t => t.PelletCount,
                    SetCurrentValue = (t, v) => t.PelletCount = (int)v,
                    GetLevel = t => t.PelletCountLevel,
                    SetLevel = (t, v) => t.PelletCountLevel = v,
                    GetBaseStat = t => t.PelletCount,
                    GetBaseCost = t => t.PelletCountUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PelletCountUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.PelletCount),
                    GetDisplayStrings = t =>
                    {
                        float current = t.PelletCount;
                        float bonus = t.PelletCountUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.PelletCount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.DamageFalloffOverDistance] = new()
                {
                    GetCurrentValue = t => t.DamageFalloffOverDistance,
                    SetCurrentValue = (t, v) => t.DamageFalloffOverDistance = v,
                    GetLevel = t => t.DamageFalloffOverDistanceLevel,
                    SetLevel = (t, v) => t.DamageFalloffOverDistanceLevel = v,
                    GetBaseStat = t => t.DamageFalloffOverDistance,
                    GetBaseCost = t => t.DamageFalloffOverDistanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageFalloffOverDistanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.DamageFalloffOverDistance),
                    GetDisplayStrings = t =>
                    {
                        float current = t.DamageFalloffOverDistance;
                        float bonus = t.DamageFalloffOverDistanceUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.DamageFalloffOverDistance);

                        if (current <= 0f)
                            return ($"{current:F1}%", "Max", "");

                        return (
                                $"{current:F1}%",
                                $"-{bonus:F1}%",
                                $"${UIManager.AbbreviateNumber(cost)}"
                            );
                    }
                },
                [TurretUpgradeType.KnockbackStrength] = new()
                {
                    GetCurrentValue = t => t.KnockbackStrength,
                    SetCurrentValue = (t, v) => t.KnockbackStrength = v,
                    GetLevel = t => t.KnockbackStrengthLevel,
                    SetLevel = (t, v) => t.KnockbackStrengthLevel = v,
                    GetBaseStat = t => t.KnockbackStrength,
                    GetBaseCost = t => t.KnockbackStrengthUpgradeBaseCost,
                    GetUpgradeAmount = t => t.KnockbackStrengthUpgradeAmount,
                    GetCostMultiplier = t => t.KnockbackStrengthCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.KnockbackStrength),
                    GetDisplayStrings = t =>
                    {
                        float current = t.KnockbackStrength;
                        float bonus = t.KnockbackStrengthUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.KnockbackStrength);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.PercentBonusDamagePerSec] = new()
                {
                    GetCurrentValue = t => t.PercentBonusDamagePerSec,
                    SetCurrentValue = (t, v) => t.PercentBonusDamagePerSec = v,
                    GetLevel = t => t.PercentBonusDamagePerSecLevel,
                    SetLevel = (t, v) => t.PercentBonusDamagePerSecLevel = v,
                    GetBaseStat = t => t.PercentBonusDamagePerSec,
                    GetBaseCost = t => t.PercentBonusDamagePerSecUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PercentBonusDamagePerSecUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.PercentBonusDamagePerSec),
                    GetDisplayStrings = t =>
                    {
                        float current = t.PercentBonusDamagePerSec;
                        float bonus = t.PercentBonusDamagePerSecUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.PercentBonusDamagePerSec);

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                },
                [TurretUpgradeType.SlowEffect] = new()
                {
                    GetCurrentValue = t => t.SlowEffect,
                    SetCurrentValue = (t, v) => t.SlowEffect = v,
                    GetLevel = t => t.SlowEffectLevel,
                    SetLevel = (t, v) => t.SlowEffectLevel = v,
                    GetBaseStat = t => t.SlowEffect,
                    GetBaseCost = t => t.SlowEffectUpgradeBaseCost,
                    GetUpgradeAmount = t => t.SlowEffectUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = t => GetExponentialCost_PlusLevel(t, TurretUpgradeType.SlowEffect),
                    GetDisplayStrings = t =>
                    {
                        float current = t.SlowEffect;
                        float bonus = t.SlowEffectUpgradeAmount;
                        float cost = GetExponentialCost_PlusLevel(t, TurretUpgradeType.SlowEffect);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}"
                        );
                    }
                }
            };
        }
        #endregion

        public void UpgradeTurretStat(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();
            float cost = upgrade.GetCost(turret);

            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(turret) >= upgrade.GetMaxValue(turret))
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            if (upgrade.GetMinValue != null && upgrade.GetCurrentValue(turret) <= upgrade.GetMinValue(turret))
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            if (TrySpend(cost))
            {
                float newValue = upgrade.GetCurrentValue(turret) + upgrade.GetUpgradeAmount(turret);
                upgrade.SetCurrentValue(turret, newValue);
                upgrade.SetLevel(turret, upgrade.GetLevel(turret) + 1);
                AudioManager.Instance.Play("Upgrade");
                button._baseTurret.UpdateTurretAppearance();
                upgrade.Upgrade?.Invoke(turret);
                UpdateUpgradeDisplay(turret, type, button);
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        private bool TrySpend(float cost) => GameManager.Instance.TrySpend(cost);

        public float GetTurretUpgradeCost(TurretStatsInstance turret, TurretUpgradeType type) =>
            !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0f : upgrade.GetCost(turret);

        private float GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);

            if (level < hybridThreshold)
                return baseCost * (1f + level * level * quadraticFactor);

            return baseCost * Mathf.Pow(exponentialPower, level);
        }

        private float GetExponentialCost_PlusLevel(TurretStatsInstance stats, TurretUpgradeType type)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            return upgrade.GetBaseCost(stats) + Mathf.Pow(upgrade.GetCostMultiplier(stats), upgrade.GetLevel(stats)) + upgrade.GetLevel(stats);
        }

        private float GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            return upgrade.GetBaseCost(stats) + Mathf.Pow(upgrade.GetCostMultiplier(stats), upgrade.GetLevel(stats));
        }

        public void UpdateUpgradeDisplay(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || turret == null)
                return;

            (string value, string bonus, string cost) = upgrade.GetDisplayStrings(turret);
            button.UpdateStats(value, bonus, cost);
        }
    }

    public enum TurretUpgradeType
    {
        Damage,
        FireRate,
        CriticalChance,
        CriticalDamageMultiplier,
        ExplosionRadius,
        SplashDamage,
        PierceChance,
        PierceDamageFalloff,
        PelletCount,
        DamageFalloffOverDistance,
        PercentBonusDamagePerSec,
        SlowEffect,
        KnockbackStrength
    }
}
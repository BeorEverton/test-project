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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.Damage, a),
                    GetAmount = t => GetMaxAmount(t.DamageUpgradeBaseCost, t.DamageCostExponentialMultiplier, t.DamageLevel),
                    GetDisplayStrings = (t, a) =>
                        {
                            float current = t.Damage;
                            float nextLevel = t.DamageLevel + 1f;
                            float nextDamage = t.BaseDamage * Mathf.Pow(t.DamageUpgradeAmount, nextLevel) + nextLevel;
                            float bonus = nextDamage - current;
                            GetExponentialCost(t, TurretUpgradeType.Damage, a, out float cost, out int amount);

                            return (UIManager.AbbreviateNumber(current),
                                $"+{UIManager.AbbreviateNumber(bonus)}",
                                $"${UIManager.AbbreviateNumber(cost)}",
                                $"{amount}X");
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.FireRate, a),
                    GetAmount = t => GetMaxAmount(t.FireRateUpgradeBaseCost, t.FireRateCostExponentialMultiplier, t.FireRateLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFireRate = t.FireRate;
                        float bonusFireRate = t.FireRateUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.FireRate, a, out float cost, out int amount);

                        return (
                            $"{currentFireRate:F2}/s",
                            $"+{bonusFireRate:F2}/s",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.CriticalChance, a),
                    GetAmount = t => GetMaxAmount(t.CriticalChanceUpgradeBaseCost, t.CriticalChanceCostExponentialMultiplier, t.CriticalChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.CriticalChance;
                        float bonus = t.CriticalChanceUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.CriticalChance, a, out float cost, out int amount);

                        if (current >= 50f)
                            return ($"{(int)current}%", "Max", "", "0X");

                        return (
                            $"{(int)current}%",
                            $"+{(int)bonus}%",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a),
                    GetAmount = t => GetMaxAmount(t.CriticalDamageMultiplierUpgradeBaseCost, t.CriticalDamageMultiplier, t.CriticalDamageMultiplierLevel),
                    GetDisplayStrings = (t, a) =>
                {
                    float current = t.CriticalDamageMultiplier;
                    float bonus = t.CriticalDamageMultiplierUpgradeAmount;
                    GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a, out float cost, out int amount);

                    return (
                        $"{(int)current}%",
                        $"+{(int)bonus}%",
                        $"${UIManager.AbbreviateNumber(cost)}",
                        $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.ExplosionRadius, a),
                    GetAmount = t => GetMaxAmount(t.ExplosionRadiusUpgradeBaseCost, 1.1f, t.ExplosionRadiusLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ExplosionRadius;
                        float bonus = t.ExplosionRadiusUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.ExplosionRadius, a, out float cost, out int amount);

                        if (t.ExplosionRadius >= 5f)
                            return ($"{current:F1}", "Max", "", "0X");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.SplashDamage, a),
                    GetAmount = t => GetMaxAmount(t.SplashDamageUpgradeBaseCost, exponentialPower, t.SplashDamageLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SplashDamage;
                        float bonus = t.SplashDamageUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.SplashDamage, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.PierceChance, a),
                    GetAmount = t => GetMaxAmount(t.PierceChanceUpgradeBaseCost, exponentialPower, t.PierceChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PierceChance;
                        float bonus = t.PierceChanceUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.PierceChance, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.PierceDamageFalloff, a),
                    GetAmount = t => GetMaxAmount(t.PierceDamageFalloffUpgradeBaseCost, exponentialPower, t.PierceDamageFalloffLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFalloff = t.PierceDamageFalloff;
                        float currentRetained = 100f - currentFalloff;

                        float nextFalloff = Mathf.Max(0f, currentFalloff - t.PierceDamageFalloffUpgradeAmount);
                        float nextRetained = 100f - nextFalloff;

                        float bonus = nextRetained - currentRetained;
                        GetExponentialCost(t, TurretUpgradeType.PierceDamageFalloff, a, out float cost, out int amount);

                        return (
                            $"{currentRetained:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.PelletCount, a),
                    GetAmount = t => GetMaxAmount(t.PelletCountUpgradeBaseCost, exponentialPower, t.PelletCountLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PelletCount;
                        float bonus = t.PelletCountUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.PelletCount, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.DamageFalloffOverDistance, a),
                    GetAmount = t => GetMaxAmount(t.DamageFalloffOverDistanceUpgradeBaseCost, exponentialPower, t.DamageFalloffOverDistanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.DamageFalloffOverDistance;
                        float bonus = t.DamageFalloffOverDistanceUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.DamageFalloffOverDistance, a, out float cost, out int amount);

                        if (current <= 0f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                                $"{current:F1}%",
                                $"-{bonus:F1}%",
                                $"${UIManager.AbbreviateNumber(cost)}",
                                $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.KnockbackStrength, a),
                    GetAmount = t => GetMaxAmount(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthCostExponentialMultiplier, t.KnockbackStrengthLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.KnockbackStrength;
                        float bonus = t.KnockbackStrengthUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.KnockbackStrength, a, out float cost, out int amount);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a),
                    GetAmount = t => GetMaxAmount(t.PercentBonusDamagePerSecUpgradeBaseCost, exponentialPower, t.PercentBonusDamagePerSecLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PercentBonusDamagePerSec;
                        float bonus = t.PercentBonusDamagePerSecUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a, out float cost, out int amount);

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.SlowEffect, a),
                    GetAmount = t => GetMaxAmount(t.SlowEffectUpgradeBaseCost, exponentialPower, t.SlowEffectLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SlowEffect;
                        float bonus = t.SlowEffectUpgradeAmount;
                        GetExponentialCost(t, TurretUpgradeType.SlowEffect, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"${UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                }
            };
        }
        #endregion

        public void UpgradeTurretStat(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button, int amount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return;

            float cost = upgrade.GetCost(turret, amount);

            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(turret) >= upgrade.GetMaxValue(turret))
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            if (upgrade.GetMinValue != null && upgrade.GetCurrentValue(turret) < upgrade.GetMinValue(turret))
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            if (TrySpend(cost))
            {
                float newValue = upgrade.GetCurrentValue(turret) + (upgrade.GetUpgradeAmount(turret) * amount);
                upgrade.SetCurrentValue(turret, newValue);
                upgrade.SetLevel(turret, upgrade.GetLevel(turret) + amount);
                AudioManager.Instance.Play("Upgrade");
                button._baseTurret.UpdateTurretAppearance();
                upgrade.Upgrade?.Invoke(turret);
                UpdateUpgradeDisplay(turret, type, button);
                button.UpdateInteractableState();
                OnAnyTurretUpgraded?.Invoke();
            }
        }

        private bool TrySpend(float cost) => GameManager.Instance.TrySpend(cost);

        public float GetTurretUpgradeCost(TurretStatsInstance turret, TurretUpgradeType type, int amount) =>
            !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0f : upgrade.GetCost(turret, amount);

        public int GetTurretAvailableUpgradeAmount(TurretStatsInstance turret, TurretUpgradeType type) =>
            !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0 : upgrade.GetAmount(turret);

        private int GetMaxAmount(float baseCost, float multiplier, int currentLevel)
        {
            ulong money = GameManager.Instance.Money;
            int minUpgradeAmount = 0, maxUpgradeAmount = 10000;
            int result = 0;

            while (minUpgradeAmount <= maxUpgradeAmount)
            {
                int mid = (minUpgradeAmount + maxUpgradeAmount) / 2;
                ulong start = (ulong)Mathf.Pow(multiplier, currentLevel);
                ulong end = (ulong)Mathf.Pow(multiplier, currentLevel + mid);
                ulong totalCost = (ulong)(baseCost * (end - start) / (multiplier - 1));

                if (totalCost <= money)
                {
                    result = mid;
                    minUpgradeAmount = mid + 1;
                }
                else
                {
                    maxUpgradeAmount = mid - 1;
                }
            }

            return result;
        }

        private void GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
            {
                cost = 0f;
                outAmount = 0;
                return;
            }

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);
            int maxAmount = GetMaxAmount(baseCost, multiplier, level);

            outAmount = inAmount == 9999
                ? maxAmount == 0
                    ? 1
                    : maxAmount
                : inAmount;

            cost = Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, outAmount));
        }

        private ulong GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int maxAmount = inAmount == 9999
                ? GetMaxAmount(baseCost, multiplier, level)
                : inAmount;

            return RecursiveExponentialCost(baseCost, multiplier, level, maxAmount);
        }

        private float RecursiveHybridCost(float baseCost, int level, int amount)
        {
            if (amount <= 0)
                return 0f;

            float cost = baseCost * (1f + level * level * quadraticFactor);

            return cost + RecursiveHybridCost(baseCost, level + 1, amount - 1);
        }

        private ulong RecursiveExponentialCost(float baseCost, float multiplier, int currentLevel, int amount)
        {
            if (amount <= 0)
                return 0;

            float start = Mathf.Pow(multiplier, currentLevel);
            float end = Mathf.Pow(multiplier, currentLevel + amount);
            return (ulong)(baseCost * (end - start) / (multiplier - 1));
        }

        public void UpdateUpgradeDisplay(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || turret == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();

            (string value, string bonus, string cost, string count) = upgrade.GetDisplayStrings(turret, amount);
            button.UpdateStats(value, bonus, cost, count);
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
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
                    GetBaseCode = t => t.BaseDamage,
                    GetUpgradeAmount = t => t.DamageUpgradeAmount,
                    GetCostMultiplier = t => t.DamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetDamageUpgradeCost,
                    GetDisplayStrings = t =>
                        {
                            float current = t.Damage;
                            float nextLevel = t.DamageLevel + 1f;
                            float nextDamage = t.BaseDamage * Mathf.Pow(t.DamageUpgradeAmount, nextLevel) + nextLevel;
                            float bonus = nextDamage - current;
                            float cost = GetDamageUpgradeCost(t);

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
                    GetBaseCode = t => t.BaseFireRate,
                    GetUpgradeAmount = t => t.FireRateUpgradeAmount,
                    GetCostMultiplier = t => t.FireRateCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetFireRateUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float currentFireRate = t.FireRate;
                        float bonusFireRate = t.FireRateUpgradeAmount;
                        float cost = GetFireRateUpgradeCost(t);

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
                    GetBaseCode = t => t.BaseCritChance,
                    GetUpgradeAmount = t => t.CriticalChanceUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalChanceCostExponentialMultiplier,
                    GetMaxValue = t => 50f,
                    GetMinValue = t => 0f,
                    GetCost = GetCriticalChanceUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.CriticalChance;
                        float bonus = t.CriticalChanceUpgradeAmount;
                        float cost = GetCriticalChanceUpgradeCost(t);

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
                    GetBaseCode = t => t.BaseCritDamage,
                    GetUpgradeAmount = t => t.CriticalDamageMultiplierUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalDamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetCriticalDamageMultiplierUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.CriticalDamageMultiplier;
                        float bonus = t.CriticalDamageMultiplierUpgradeAmount;
                        float cost = GetCriticalDamageMultiplierUpgradeCost(t);

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
                    GetBaseCode = t => t.ExplosionRadius,
                    GetUpgradeAmount = t => t.ExplosionRadiusUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetExplosionRadiusUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.ExplosionRadius;
                        float bonus = t.ExplosionRadiusUpgradeAmount;
                        float cost = GetExplosionRadiusUpgradeCost(t);

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
                    GetBaseCode = t => t.SplashDamage,
                    GetUpgradeAmount = t => t.SplashDamageUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetSplashDamageUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.SplashDamage;
                        float bonus = t.SplashDamageUpgradeAmount;
                        float cost = GetSplashDamageUpgradeCost(t);

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
                    GetBaseCode = t => t.PierceChance,
                    GetUpgradeAmount = t => t.PierceChanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = GetPierceChanceUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.PierceChance;
                        float bonus = t.PierceChanceUpgradeAmount;
                        float cost = GetPierceChanceUpgradeCost(t);

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
                    GetBaseCode = t => t.PierceDamageFalloff,
                    GetUpgradeAmount = t => t.PierceDamageFalloffUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetPierceDamageFalloffUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float currentFalloff = t.PierceDamageFalloff;
                        float currentRetained = 100f - currentFalloff;

                        float nextFalloff = Mathf.Max(0f, currentFalloff - t.PierceDamageFalloffUpgradeAmount);
                        float nextRetained = 100f - nextFalloff;

                        float bonus = nextRetained - currentRetained;
                        float cost = GetPierceDamageFalloffUpgradeCost(t);

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
                    GetBaseCode = t => t.PelletCount,
                    GetUpgradeAmount = t => t.PelletCountUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetPelletCountUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.PelletCount;
                        float bonus = t.PelletCountUpgradeAmount;
                        float cost = GetPelletCountUpgradeCost(t);

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
                    GetBaseCode = t => t.DamageFalloffOverDistance,
                    GetUpgradeAmount = t => t.DamageFalloffOverDistanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetDamageFalloffOverDistanceUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.DamageFalloffOverDistance;
                        float bonus = t.DamageFalloffOverDistanceUpgradeAmount;
                        float cost = GetDamageFalloffOverDistanceUpgradeCost(t);

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
                    GetBaseCode = t => t.KnockbackStrength,
                    GetUpgradeAmount = t => t.KnockbackStrengthUpgradeAmount,
                    GetCostMultiplier = t => t.KnockbackStrengthCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetKnockbackStrengthUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.KnockbackStrength;
                        float bonus = t.KnockbackStrengthUpgradeAmount;
                        float cost = GetKnockbackStrengthUpgradeCost(t);

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
                    GetBaseCode = t => t.PercentBonusDamagePerSec,
                    GetUpgradeAmount = t => t.PercentBonusDamagePerSecUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = GetBonusDamagePerSecUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.PercentBonusDamagePerSec;
                        float bonus = t.PercentBonusDamagePerSecUpgradeAmount;
                        float cost = GetBonusDamagePerSecUpgradeCost(t);

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
                    GetBaseCode = t => t.SlowEffect,
                    GetUpgradeAmount = t => t.SlowEffectUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = GetSlowEffectUpgradeCost,
                    GetDisplayStrings = t =>
                    {
                        float current = t.SlowEffect;
                        float bonus = t.SlowEffectUpgradeAmount;
                        float cost = GetSlowEffectUpgradeCost(t);

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

        private float GetHybridCost(float baseCost, float level)
        {
            if (level < hybridThreshold)
                return baseCost * (1f + level * level * quadraticFactor);

            return baseCost * Mathf.Pow(exponentialPower, level);
        }

        private float GetExponentialCost_PlusLevel(float baseCost, float level, float exponentialMultiplier) => baseCost + Mathf.Pow(exponentialMultiplier, level) + level;
        private float GetExponentialCost(float baseCost, float level, float multiplier) => baseCost * Mathf.Pow(multiplier, level);

        public void UpdateUpgradeDisplay(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || turret == null)
                return;

            (string value, string bonus, string cost) = upgrade.GetDisplayStrings(turret);
            button.UpdateStats(value, bonus, cost);
        }

        // Used for enabling/disabling the buttons on the UI based on their costs
        public float GetDamageUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost_PlusLevel(t.DamageUpgradeBaseCost, t.DamageLevel, t.DamageCostExponentialMultiplier);

        public float GetFireRateUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost_PlusLevel(t.FireRateUpgradeBaseCost, t.FireRateLevel, t.FireRateCostExponentialMultiplier);

        public float GetCriticalChanceUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost(t.CriticalChanceUpgradeBaseCost, t.CriticalChanceLevel, t.CriticalChanceCostExponentialMultiplier);

        public float GetCriticalDamageMultiplierUpgradeCost(TurretStatsInstance t) =>
            GetExponentialCost(t.CriticalDamageMultiplierUpgradeBaseCost, t.CriticalDamageMultiplierLevel, t.CriticalDamageCostExponentialMultiplier);

        public float GetExplosionRadiusUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.ExplosionRadiusUpgradeBaseCost, t.ExplosionRadiusLevel);

        public float GetSplashDamageUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.SplashDamageUpgradeBaseCost, t.SplashDamageLevel);

        public float GetPierceChanceUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PierceChanceUpgradeBaseCost, t.PierceChanceLevel);

        public float GetPierceDamageFalloffUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PierceDamageFalloffUpgradeBaseCost, t.PierceDamageFalloffLevel);

        public float GetPelletCountUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PelletCountUpgradeBaseCost, t.PelletCountLevel);

        public float GetDamageFalloffOverDistanceUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.DamageFalloffOverDistanceUpgradeBaseCost, t.DamageFalloffOverDistanceLevel);

        public float GetBonusDamagePerSecUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.PercentBonusDamagePerSecUpgradeBaseCost, t.PercentBonusDamagePerSecLevel);

        public float GetSlowEffectUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.SlowEffectUpgradeBaseCost, t.SlowEffectLevel);

        public float GetKnockbackStrengthUpgradeCost(TurretStatsInstance t) =>
            GetHybridCost(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthLevel);
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
using Assets.Scripts.Systems.Audio;
using Assets.Scripts.Systems.Save;
using Assets.Scripts.Turrets;
using Assets.Scripts.UI;
using Assets.Scripts.UpgradeSystem;
using DG.Tweening;
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

        [Header("Max/Min stats")]
        [SerializeField] private float maxRange = 40f;
        [SerializeField] private float maxRotationSpeed = 20f;
        [SerializeField] private float maxCriticalChance = 90f;
        [SerializeField] private int maxBounce = 100;
        [SerializeField] private float maxBounceRange = 50f;
        [SerializeField] private float minBounceDelay = 0.05f;
        [SerializeField] private float maxBounceDmgPct = 100f;
        [SerializeField] private float maxConeAngle = 120f;
        [SerializeField] private float minExplosionDelay = 0.2f;
        [SerializeField] private float maxTrapAheadDistance = 10f;
        [SerializeField] private int maxTrapPool = 100;
        [SerializeField] private float maxArmorPenetration = 100f;


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

                    // Always apply upgrades step-by-step with a minimum of +1 damage per step.
                    UpgradeTurret = (t, a) =>
                    {
                        for (int i = 0; i < a; i++)
                        {
                            // Target damage if we took ONE more level
                            float targetAfterOne = t.BaseDamage * Mathf.Pow(t.DamageUpgradeAmount, t.DamageLevel + 1);

                            // Commit at least +1 per purchase
                            int stepInc = Mathf.Max(1, Mathf.RoundToInt(targetAfterOne - t.Damage));

                            t.Damage += stepInc;
                            t.DamageLevel += 1;

                            if (t.Damage >= float.MaxValue)
                            {
                                t.Damage = float.MaxValue;
                                break;
                            }
                        }
                    },

                    GetLevel = t => t.DamageLevel,
                    GetBaseStat = t => t.BaseDamage,
                    GetBaseCost = t => t.DamageUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageUpgradeAmount,
                    GetCostMultiplier = t => t.DamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 1f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.Damage, a),

                    // Display mirrors the exact per-step logic so UI == applied result.
                    GetDisplayStrings = (t, a) =>
                    {
                        float startDamage = t.Damage;
                        int startLevel = t.DamageLevel;

                        float simDamage = startDamage;
                        int simLevel = startLevel;

                        for (int i = 0; i < a; i++)
                        {
                            float targetAfterOne = t.BaseDamage * Mathf.Pow(t.DamageUpgradeAmount, simLevel + 1);
                            int stepInc = Mathf.Max(1, Mathf.RoundToInt(targetAfterOne - simDamage));
                            simDamage += stepInc;
                            simLevel += 1;
                        }

                        int totalBonus = Mathf.Max(1, Mathf.RoundToInt(simDamage - startDamage));

                        GetExponentialCost(t, TurretUpgradeType.Damage, a, out float cost, out int amount);
                        cost = Mathf.Max(cost, 1f);

                        return (
                            UIManager.AbbreviateNumber(startDamage),
                            $"+{UIManager.AbbreviateNumber(totalBonus)}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.FireRate] = new()
                {
                    GetCurrentValue = t => t.FireRate,
                    UpgradeTurret = (t, a) =>
                    {
                        t.FireRateLevel += a;
                        t.FireRate += (t.FireRateUpgradeAmount * a);
                    },
                    GetLevel = t => t.FireRateLevel,
                    GetBaseStat = t => t.BaseFireRate,
                    GetBaseCost = t => t.FireRateUpgradeBaseCost,
                    GetUpgradeAmount = t => t.FireRateUpgradeAmount,
                    GetCostMultiplier = t => t.FireRateCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.FireRate, a),
                    //GetAmount = t => GetMaxAmount(t.FireRateUpgradeBaseCost, t.FireRateCostExponentialMultiplier, t.FireRateLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFireRate = t.FireRate;
                        float bonusFireRate = GetBonusAmount(t, TurretUpgradeType.FireRate, a);
                        GetExponentialCost(t, TurretUpgradeType.FireRate, a, out float cost, out int amount);

                        return (
                            $"{currentFireRate:F2}/s",
                            $"+{bonusFireRate:F2}/s",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.Range] = new()
                {
                    GetCurrentValue = t => t.Range,
                    UpgradeTurret = (t, a) =>
                    {
                        t.RangeLevel += a;
                        t.Range += (t.RangeUpgradeAmount * a);
                        t.Range = Mathf.Min(t.Range, maxRange);
                    },

                    GetLevel = t => t.RangeLevel,
                    GetBaseStat = t => t.Range,
                    GetBaseCost = t => t.RangeUpgradeBaseCost,
                    GetUpgradeAmount = t => t.RangeUpgradeAmount,
                    GetCostMultiplier = t => t.RangeCostExponentialMultiplier,
                    GetMaxValue = t => maxRange,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.Range, a),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.Range;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.Range, a);
                        GetExponentialCost(t, TurretUpgradeType.Range, a, out float cost, out int amount);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.RotationSpeed] = new()
                {
                    GetCurrentValue = t => t.RotationSpeed,
                    UpgradeTurret = (t, a) =>
                    {
                        t.RotationSpeedLevel += a;
                        t.RotationSpeed += (t.RotationSpeedUpgradeAmount * a);
                        t.RotationSpeed = Mathf.Min(t.RotationSpeed, maxRotationSpeed);
                    },

                    GetLevel = t => t.RotationSpeedLevel,
                    GetBaseStat = t => t.RotationSpeed,
                    GetBaseCost = t => t.RotationSpeedUpgradeBaseCost,
                    GetUpgradeAmount = t => t.RotationSpeedUpgradeAmount,
                    GetCostMultiplier = t => t.RotationSpeedCostExponentialMultiplier,
                    GetMaxValue = t => maxRotationSpeed,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.RotationSpeed, a),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.RotationSpeed;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.RotationSpeed, a);
                        GetExponentialCost(t, TurretUpgradeType.RotationSpeed, a, out float cost, out int amount);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.CriticalChance] = new()
                {
                    GetCurrentValue = t => t.CriticalChance,
                    UpgradeTurret = (t, a) =>
                    {
                        t.CriticalChanceLevel += a;
                        t.CriticalChance += (t.CriticalChanceUpgradeAmount * a);
                        t.CriticalChance = Mathf.Min(t.CriticalChance, maxCriticalChance);
                    },
                    GetLevel = t => t.CriticalChanceLevel,
                    GetBaseStat = t => t.BaseCritChance,
                    GetBaseCost = t => t.CriticalChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.CriticalChanceUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalChanceCostExponentialMultiplier,
                    GetMaxValue = t => maxCriticalChance,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.CriticalChance, a),
                    //GetAmount = t => GetMaxAmount(t.CriticalChanceUpgradeBaseCost, t.CriticalChanceCostExponentialMultiplier, t.CriticalChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.CriticalChance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.CriticalChance, a);
                        GetExponentialCost(t, TurretUpgradeType.CriticalChance, a, out float cost, out int amount);

                        if (current >= maxCriticalChance)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );

                    }
                },
                [TurretUpgradeType.CriticalDamageMultiplier] = new()
                {
                    GetCurrentValue = t => t.CriticalDamageMultiplier,
                    UpgradeTurret = (t, a) =>
                    {
                        t.CriticalDamageMultiplierLevel += a;
                        t.CriticalDamageMultiplier += (t.CriticalDamageMultiplierUpgradeAmount * a);
                    },
                    GetLevel = t => t.CriticalDamageMultiplierLevel,
                    GetBaseStat = t => t.BaseCritDamage,
                    GetBaseCost = t => t.CriticalDamageMultiplierUpgradeBaseCost,
                    GetUpgradeAmount = t => t.CriticalDamageMultiplierUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalDamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a),
                    //GetAmount = t => GetMaxAmount(t.CriticalDamageMultiplierUpgradeBaseCost, t.CriticalDamageMultiplier, t.CriticalDamageMultiplierLevel),
                    GetDisplayStrings = (t, a) =>
                {
                    float current = t.CriticalDamageMultiplier;
                    float bonus = GetBonusAmount(t, TurretUpgradeType.CriticalDamageMultiplier, a);
                    GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a, out float cost, out int amount);

                    return (
                        $"{current:F1}%",
                        $"+{bonus:F1}%",
                        $"{UIManager.AbbreviateNumber(cost)}",
                        $"{amount}X"
                    );

                }
                },
                [TurretUpgradeType.ExplosionRadius] = new()
                {
                    GetCurrentValue = t => t.ExplosionRadius,
                    UpgradeTurret = (t, a) =>
                    {
                        t.ExplosionRadiusLevel += a;
                        t.ExplosionRadius += (t.ExplosionRadiusUpgradeAmount * a);
                        // Enforce hard cap
                        t.ExplosionRadius = Mathf.Min(t.ExplosionRadius, 5f);
                    },
                    GetLevel = t => t.ExplosionRadiusLevel,
                    GetBaseStat = t => t.ExplosionRadius,
                    GetBaseCost = t => t.ExplosionRadiusUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ExplosionRadiusUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    // Real engine cap
                    GetMaxValue = t => 5f,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.ExplosionRadius, a),
                    //GetAmount = t => GetMaxAmount(t.ExplosionRadiusUpgradeBaseCost, 1.1f, t.ExplosionRadiusLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ExplosionRadius;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.ExplosionRadius, a);
                        GetHybridCost(t, TurretUpgradeType.ExplosionRadius, a, out float cost, out int amount);

                        if (t.ExplosionRadius >= 5f)
                            return ($"{current:F1}", "Max", "", "0X");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.SplashDamage] = new()
                {
                    GetCurrentValue = t => t.SplashDamage,
                    UpgradeTurret = (t, a) =>
                    {
                        t.SplashDamageLevel += a;
                        t.SplashDamage += (t.SplashDamageUpgradeAmount * a);
                    },
                    GetLevel = t => t.SplashDamageLevel,
                    GetBaseStat = t => t.SplashDamage,
                    GetBaseCost = t => t.SplashDamageUpgradeBaseCost,
                    GetUpgradeAmount = t => t.SplashDamageUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.SplashDamage, a),
                    //GetAmount = t => GetMaxAmount(t.SplashDamageUpgradeBaseCost, exponentialPower, t.SplashDamageLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SplashDamage;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.SplashDamage, a);
                        GetHybridCost(t, TurretUpgradeType.SplashDamage, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber((current * 100), true) + "%",
                            $"+{UIManager.AbbreviateNumber((bonus * 100), true) + "%"}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.PierceChance] = new()
                {
                    GetCurrentValue = t => t.PierceChance,
                    UpgradeTurret = (t, a) =>
                    {
                        t.PierceChanceLevel += a;
                        t.PierceChance += (t.PierceChanceUpgradeAmount * a);

                        t.PierceChance = Mathf.Min(t.PierceChance, 100f);
                    },
                    GetLevel = t => t.PierceChanceLevel,
                    GetBaseStat = t => t.PierceChance,
                    GetBaseCost = t => t.PierceChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceChanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.PierceChance, a),
                    //GetAmount = t => GetMaxAmount(t.PierceChanceUpgradeBaseCost, exponentialPower, t.PierceChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PierceChance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PierceChance, a);
                        GetHybridCost(t, TurretUpgradeType.PierceChance, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.PierceDamageFalloff] = new()
                {
                    GetCurrentValue = t => t.PierceDamageFalloff,
                    UpgradeTurret = (t, a) =>
                    {
                        t.PierceDamageFalloffLevel += a;
                        t.PierceDamageFalloff -= (t.PierceDamageFalloffUpgradeAmount * a);

                        t.PierceDamageFalloff = Mathf.Max(t.PierceDamageFalloff, 0f);
                    },
                    GetLevel = t => t.PierceDamageFalloffLevel,
                    GetBaseStat = t => t.PierceDamageFalloff,
                    GetBaseCost = t => t.PierceDamageFalloffUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceDamageFalloffUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.PierceDamageFalloff, a),
                    //GetAmount = t => GetMaxAmount(t.PierceDamageFalloffUpgradeBaseCost, exponentialPower, t.PierceDamageFalloffLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFalloff = t.PierceDamageFalloff;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PierceDamageFalloff, a);
                        GetHybridCost(t, TurretUpgradeType.PierceDamageFalloff, a, out float cost, out int amount);

                        return (
                            $"{currentFalloff:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.PelletCount] = new()
                {
                    GetCurrentValue = t => t.PelletCount,
                    UpgradeTurret = (t, a) =>
                    {
                        t.PelletCountLevel += a;
                        t.PelletCount += (t.PelletCountUpgradeAmount * a);
                    },
                    GetLevel = t => t.PelletCountLevel,
                    GetBaseStat = t => t.PelletCount,
                    GetBaseCost = t => t.PelletCountUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PelletCountUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.PelletCount, a),
                    //GetAmount = t => GetMaxAmount(t.PelletCountUpgradeBaseCost, exponentialPower, t.PelletCountLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PelletCount;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PelletCount, a);
                        GetHybridCost(t, TurretUpgradeType.PelletCount, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.DamageFalloffOverDistance] = new()
                {
                    GetCurrentValue = t => t.DamageFalloffOverDistance,
                    UpgradeTurret = (t, a) =>
                    {
                        t.DamageFalloffOverDistanceLevel += a;
                        t.DamageFalloffOverDistance -= (t.DamageFalloffOverDistanceUpgradeAmount * a);
                        t.DamageFalloffOverDistance = Mathf.Max(t.DamageFalloffOverDistance, 0f);
                    },
                    GetLevel = t => t.DamageFalloffOverDistanceLevel,
                    GetBaseStat = t => t.DamageFalloffOverDistance,
                    GetBaseCost = t => t.DamageFalloffOverDistanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageFalloffOverDistanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.DamageFalloffOverDistance, a),
                    //GetAmount = t => GetMaxAmount(t.DamageFalloffOverDistanceUpgradeBaseCost, exponentialPower, t.DamageFalloffOverDistanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.DamageFalloffOverDistance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.DamageFalloffOverDistance, a);
                        GetHybridCost(t, TurretUpgradeType.DamageFalloffOverDistance, a, out float cost, out int amount);

                        if (current <= 0f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                                $"{current:F1}%",
                                $"-{bonus:F1}%",
                                $"{UIManager.AbbreviateNumber(cost)}",
                                $"{amount}X"
                            );
                    }
                },
                [TurretUpgradeType.KnockbackStrength] = new()
                {
                    GetCurrentValue = t => t.KnockbackStrength,
                    UpgradeTurret = (t, a) =>
                    {
                        t.KnockbackStrengthLevel += a;
                        t.KnockbackStrength += (t.KnockbackStrengthUpgradeAmount * a);
                    },
                    GetLevel = t => t.KnockbackStrengthLevel,
                    GetBaseStat = t => t.KnockbackStrength,
                    GetBaseCost = t => t.KnockbackStrengthUpgradeBaseCost,
                    GetUpgradeAmount = t => t.KnockbackStrengthUpgradeAmount,
                    GetCostMultiplier = t => t.KnockbackStrengthCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.KnockbackStrength, a),
                    //GetAmount = t => GetMaxAmount(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthCostExponentialMultiplier, t.KnockbackStrengthLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.KnockbackStrength;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.KnockbackStrength, a);
                        GetHybridCost(t, TurretUpgradeType.KnockbackStrength, a, out float cost, out int amount);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.PercentBonusDamagePerSec] = new()
                {
                    GetCurrentValue = t => t.PercentBonusDamagePerSec,
                    UpgradeTurret = (t, a) =>
                    {
                        t.PercentBonusDamagePerSecLevel += a;
                        t.PercentBonusDamagePerSec += (t.PercentBonusDamagePerSecUpgradeAmount * a);
                    },
                    GetLevel = t => t.PercentBonusDamagePerSecLevel,
                    GetBaseStat = t => t.PercentBonusDamagePerSec,
                    GetBaseCost = t => t.PercentBonusDamagePerSecUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PercentBonusDamagePerSecUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a),
                    //GetAmount = t => GetMaxAmount(t.PercentBonusDamagePerSecUpgradeBaseCost, exponentialPower, t.PercentBonusDamagePerSecLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PercentBonusDamagePerSec;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PercentBonusDamagePerSec, a);
                        GetHybridCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a, out float cost, out int amount);

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.SlowEffect] = new()
                {
                    GetCurrentValue = t => t.SlowEffect,
                    UpgradeTurret = (t, a) =>
                    {
                        t.SlowEffectLevel += a;
                        t.SlowEffect += (t.SlowEffectUpgradeAmount * a);

                        t.SlowEffect = Mathf.Min(t.SlowEffect, 100f);
                    },
                    GetLevel = t => t.SlowEffectLevel,
                    GetBaseStat = t => t.SlowEffect,
                    GetBaseCost = t => t.SlowEffectUpgradeBaseCost,
                    GetUpgradeAmount = t => t.SlowEffectUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => 100f,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetHybridCost(t, TurretUpgradeType.SlowEffect, a),
                    //GetAmount = t => GetMaxAmount(t.SlowEffectUpgradeBaseCost, exponentialPower, t.SlowEffectLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SlowEffect;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.SlowEffect, a);
                        GetHybridCost(t, TurretUpgradeType.SlowEffect, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.BounceCount] = new()
                {
                    GetCurrentValue = t => t.BounceCount,
                    UpgradeTurret = (t, a) =>
                    {
                        t.BounceCountLevel += a;
                        t.BounceCount += Mathf.RoundToInt(t.BounceCountUpgradeAmount * a);
                        t.BounceCount = Mathf.Min(t.BounceCount, maxBounce); // hard cap
                    },
                    GetLevel = t => t.BounceCountLevel,
                    GetBaseStat = t => t.BounceCount,
                    GetBaseCost = t => t.BounceCountUpgradeBaseCost,
                    GetUpgradeAmount = t => t.BounceCountUpgradeAmount,
                    GetCostMultiplier = t => t.BounceCountCostExponentialMultiplier,
                    GetMaxValue = t => maxBounce,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.BounceCount, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.BounceCount;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.BounceCount, a);
                        GetExponentialCost(t, TurretUpgradeType.BounceCount, a, out float cost, out int amount);

                        if (current >= maxBounce)
                            return ($"{(int)current}", "Max", "", "0X");

                        return (
                            $"{(int)current}",
                            $"+{(int)bonus}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }
                },
                [TurretUpgradeType.BounceRange] = new()
                {
                    GetCurrentValue = t => t.BounceRange,
                    UpgradeTurret = (t, a) =>
                    {
                        t.BounceRangeLevel += a;
                        t.BounceRange += (t.BounceRangeUpgradeAmount * a);
                        t.BounceRange = Mathf.Min(t.BounceRange, maxBounceRange);
                    },
                    GetLevel = t => t.BounceRangeLevel,
                    GetBaseStat = t => t.BounceRange,
                    GetBaseCost = t => t.BounceRangeUpgradeBaseCost,
                    GetUpgradeAmount = t => t.BounceRangeUpgradeAmount,
                    GetCostMultiplier = t => t.BounceRangeCostExponentialMultiplier,
                    GetMaxValue = t => maxBounceRange,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.BounceRange, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.BounceRange;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.BounceRange, a);
                        GetExponentialCost(t, TurretUpgradeType.BounceRange, a, out float cost, out int amount);

                        if (current >= maxBounceRange)
                            return ($"{current:F1}", "Max", "", "0X");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.BounceDelay] = new()
                {
                    GetCurrentValue = t => t.BounceDelay,
                    UpgradeTurret = (t, a) =>
                    {
                        t.BounceDelayLevel += a;
                        t.BounceDelay -= (t.BounceDelayUpgradeAmount * a);
                        t.BounceDelay = Mathf.Max(t.BounceDelay, minBounceDelay); // min floor
                    },
                    GetLevel = t => t.BounceDelayLevel,
                    GetBaseStat = t => t.BounceDelay,
                    GetBaseCost = t => t.BounceDelayUpgradeBaseCost,
                    GetUpgradeAmount = t => t.BounceDelayUpgradeAmount, // interpreted as "seconds reduced"
                    GetCostMultiplier = t => t.BounceDelayCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue, // not used for decreasing upgrade
                    GetMinValue = t => minBounceDelay,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.BounceDelay, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.BounceDelay;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.BounceDelay, a);
                        GetExponentialCost(t, TurretUpgradeType.BounceDelay, a, out float cost, out int amount);

                        if (current <= minBounceDelay)
                            return ($"{current:F2}s", "Min", "", "0X");

                        return (
                            $"{current:F2}s",
                            $"-{bonus:F2}s",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.BounceDamagePct] = new()
                {
                    GetCurrentValue = t => t.BounceDamagePct,
                    UpgradeTurret = (t, a) =>
                    {
                        t.BounceDamagePctLevel += a;
                        t.BounceDamagePct += (t.BounceDamagePctUpgradeAmount * a);
                        t.BounceDamagePct = Mathf.Min(t.BounceDamagePct, maxBounceDmgPct);
                    },
                    GetLevel = t => t.BounceDamagePctLevel,
                    GetBaseStat = t => t.BounceDamagePct,
                    GetBaseCost = t => t.BounceDamagePctUpgradeBaseCost,
                    GetUpgradeAmount = t => t.BounceDamagePctUpgradeAmount,
                    GetCostMultiplier = t => t.BounceDamagePctCostExponentialMultiplier,
                    GetMaxValue = t => maxBounceDmgPct,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.BounceDamagePct, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.BounceDamagePct;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.BounceDamagePct, a);
                        GetExponentialCost(t, TurretUpgradeType.BounceDamagePct, a, out float cost, out int amount);

                        if (current >= maxBounceDmgPct)
                            return ($"{(current * 100f):F1}%", "Max", "", "0X");

                        return (
                            $"{(current * 100f):F1}%",
                            $"+{(bonus * 100f):F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );

                    }

                },
                [TurretUpgradeType.ConeAngle] = new()
                {
                    GetCurrentValue = t => t.ConeAngle,
                    UpgradeTurret = (t, a) =>
                    {
                        t.ConeAngleLevel += a;
                        t.ConeAngle += (t.ConeAngleUpgradeAmount * a);
                        t.ConeAngle = Mathf.Min(t.ConeAngle, maxConeAngle);
                    },
                    GetLevel = t => t.ConeAngleLevel,
                    GetBaseStat = t => t.ConeAngle,
                    GetBaseCost = t => t.ConeAngleUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ConeAngleUpgradeAmount,
                    GetCostMultiplier = t => t.ConeAngleCostExponentialMultiplier,
                    GetMaxValue = t => maxConeAngle,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.ConeAngle, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ConeAngle;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.ConeAngle, a);
                        GetExponentialCost(t, TurretUpgradeType.ConeAngle, a, out float cost, out int amount);

                        if (current >= maxConeAngle)
                            return ($"{current:F1}°", "Max", "", "0X");

                        return (
                            $"{current:F1}°",
                            $"+{bonus:F1}°",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.ExplosionDelay] = new()
                {
                    GetCurrentValue = t => t.ExplosionDelay,
                    UpgradeTurret = (t, a) =>
                    {
                        t.ExplosionDelayLevel += a;
                        t.ExplosionDelay -= (t.ExplosionDelayUpgradeAmount * a);
                        t.ExplosionDelay = Mathf.Max(t.ExplosionDelay, minExplosionDelay);
                    },
                    GetLevel = t => t.ExplosionDelayLevel,
                    GetBaseStat = t => t.ExplosionDelay,
                    GetBaseCost = t => t.ExplosionDelayUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ExplosionDelayUpgradeAmount, // seconds reduced
                    GetCostMultiplier = t => t.ExplosionDelayCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => minExplosionDelay,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.ExplosionDelay, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ExplosionDelay;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.ExplosionDelay, a);
                        GetExponentialCost(t, TurretUpgradeType.ExplosionDelay, a, out float cost, out int amount);

                        if (current <= minExplosionDelay)
                            return ($"{current:F2}s", "Min", "", "0X");

                        return (
                            $"{current:F2}s",
                            $"-{bonus:F2}s",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.AheadDistance] = new()
                {
                    GetCurrentValue = t => t.AheadDistance,
                    UpgradeTurret = (t, a) =>
                    {
                        t.AheadDistanceLevel += a;
                        t.AheadDistance += (t.AheadDistanceUpgradeAmount * a);
                        t.AheadDistance = Mathf.Min(t.AheadDistance, maxTrapAheadDistance);
                    },
                    GetLevel = t => t.AheadDistanceLevel,
                    GetBaseStat = t => t.AheadDistance,
                    GetBaseCost = t => t.AheadDistanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.AheadDistanceUpgradeAmount,
                    GetCostMultiplier = t => t.AheadDistanceCostExponentialMultiplier,
                    GetMaxValue = t => maxTrapAheadDistance,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.AheadDistance, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.AheadDistance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.AheadDistance, a);
                        GetExponentialCost(t, TurretUpgradeType.AheadDistance, a, out float cost, out int amount);

                        if (current >= maxTrapAheadDistance)
                            return ($"{current:F1}", "Max", "", "0X");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.MaxTrapsActive] = new()
                {
                    GetCurrentValue = t => t.MaxTrapsActive,
                    UpgradeTurret = (t, a) =>
                    {
                        t.MaxTrapsActiveLevel += a;
                        t.MaxTrapsActive += Mathf.RoundToInt(t.MaxTrapsActiveUpgradeAmount * a);
                        t.MaxTrapsActive = Mathf.Min(t.MaxTrapsActive, maxTrapPool);
                    },
                    GetLevel = t => t.MaxTrapsActiveLevel,
                    GetBaseStat = t => t.MaxTrapsActive,
                    GetBaseCost = t => t.MaxTrapsActiveUpgradeBaseCost,
                    GetUpgradeAmount = t => t.MaxTrapsActiveUpgradeAmount,
                    GetCostMultiplier = t => t.MaxTrapsActiveCostExponentialMultiplier,
                    GetMaxValue = t => maxTrapPool,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.MaxTrapsActive, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.MaxTrapsActive;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.MaxTrapsActive, a);
                        GetExponentialCost(t, TurretUpgradeType.MaxTrapsActive, a, out float cost, out int amount);

                        if (current >= maxTrapPool)
                            return ($"{(int)current}", "Max", "", "0X");

                        return (
                            $"{(int)current}",
                            $"+{(int)bonus}",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },
                [TurretUpgradeType.ArmorPenetration] = new()
                {
                    GetCurrentValue = t => t.ArmorPenetration,
                    UpgradeTurret = (t, a) =>
                    {
                        t.ArmorPenetrationLevel += a;
                        t.ArmorPenetration += (t.ArmorPenetrationUpgradeAmount * a);
                        t.ArmorPenetration = Mathf.Clamp(t.ArmorPenetration, 0f, maxArmorPenetration);
                    },
                    GetLevel = t => t.ArmorPenetrationLevel,
                    GetBaseStat = t => t.ArmorPenetration,
                    GetBaseCost = t => t.ArmorPenetrationUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ArmorPenetrationUpgradeAmount,
                    GetCostMultiplier = t => t.ArmorPenetrationCostExponentialMultiplier,
                    GetMaxValue = t => maxArmorPenetration,
                    GetMinValue = t => 0f,
                    GetCost = (t, a, c) => GetExponentialCost(t, TurretUpgradeType.ArmorPenetration, a, c),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ArmorPenetration;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.ArmorPenetration, a);
                        GetExponentialCost(t, TurretUpgradeType.ArmorPenetration, a, out float cost, out int amount);

                        if (current >= maxArmorPenetration)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"{UIManager.AbbreviateNumber(cost)}",
                            $"{amount}X"
                        );
                    }

                },

            };
        }
        #endregion

        public void UpgradeTurretStat(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button, int amount, Currency currency)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return;

            float cost;
            int effectiveAmount = GetEffectiveBuyAmount(turret, type, amount, currency, out cost);

            if (effectiveAmount <= 0)
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            if (TrySpend(upgrade.CurrencyUsed, cost))
            {
                upgrade.UpgradeTurret(turret, effectiveAmount);
                AudioManager.Instance.Play("Upgrade");
                button._baseTurret.UpdateTurretAppearance();
                UpdateUpgradeDisplay(turret, type, button);
                button.UpdateInteractableState();
                OnAnyTurretUpgraded?.Invoke();
                AnimateBuyButtonClick(button.GetComponent<RectTransform>());
            }

            SaveGameManager.Instance.SaveGame();
        }

        // OUT  DATE - use UpgradeTurretStat instead
        public void UpgradePermanentTurretStat(BaseTurret baseTurret,TurretUpgradeType type,TurretUpgradeButton button,int amount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return;

            // Work on the saved permanent sheet, not the runtime clone
            TurretStatsInstance stats = baseTurret.PermanentStats;

            // Cost in Black Steel
            float cost = upgrade.GetCost(stats, amount, Currency.BlackSteel);

            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(stats) >= upgrade.GetMaxValue(stats))
            {
                UpdateUpgradeDisplay(stats, type, button);
                return;
            }

            if (GameManager.Instance.TrySpendCurrency(Currency.BlackSteel, (ulong)cost))
            {
                upgrade.UpgradeTurret(stats, amount);                    // apply
                AudioManager.Instance.Play("Upgrade");
                baseTurret.UpdateTurretAppearance();                     // optional: show now
                UpdateUpgradeDisplay(stats, type, button);
                AnimateBuyButtonClick(button.GetComponent<RectTransform>());
            }

            SaveGameManager.Instance.SaveGame();
        }


        public void AnimateBuyButtonClick(RectTransform button)
        {
            // Cancel any ongoing tweens on this button
            button.DOKill();

            // Scale punch: 1 - 1.15 - 1
            button.DOScale(Vector3.one * 1.15f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() =>
                {
                    button.DOScale(Vector3.one, 0.1f).SetEase(Ease.InOutSine);
                });
        }

        private bool TrySpend(Currency currency, float cost)
        {
            return GameManager.Instance.TrySpendCurrency(currency, (ulong)cost);
        }

        public float GetTurretUpgradeCost(TurretStatsInstance turret, TurretUpgradeType type, int amount, Currency currency = Currency.Scraps) =>
        !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade)
            ? 0f
            : upgrade.GetCost(turret, amount, currency);


        // Used to show Max Amount of upgrades available
        //public int GetTurretAvailableUpgradeAmount(TurretStatsInstance turret, TurretUpgradeType type) =>
        //    !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0 : upgrade.GetAmount(turret);

        private int GetMaxAmount(float baseCost, float multiplier, int currentLevel, Currency currencyUsed)
        {
            int amount = 0;
            float totalCost = 0f;
            ulong money = GameManager.Instance.GetCurrency(currencyUsed);

            return amount;
        }

        private float GetBonusAmount(TurretStatsInstance stats, TurretUpgradeType type, int amount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || upgrade == null)
                return 0f;

            float step = upgrade.GetUpgradeAmount != null ? upgrade.GetUpgradeAmount(stats) : 0f;
            return step * amount;
        }


        // Determines how many steps we can legally buy without breaching stat caps.
        // We treat falloff-type upgrades as "decreasing".
        private int CapAmountByStatLimit(TurretStatsInstance stats, TurretUpgradeType type, int desiredAmount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade up))
                return desiredAmount;

            float current = up.GetCurrentValue(stats);
            float step = Mathf.Abs(up.GetUpgradeAmount(stats));
            float max = up.GetMaxValue != null ? up.GetMaxValue(stats) : float.PositiveInfinity;
            float min = up.GetMinValue != null ? up.GetMinValue(stats) : float.NegativeInfinity;

            // Identify decreasing stats explicitly
            bool isDecreasing =
                type == TurretUpgradeType.PierceDamageFalloff ||
                type == TurretUpgradeType.DamageFalloffOverDistance;

            if (step <= 0f)
                return 0;


            int capSteps;
            bool hasUpperBound = up.GetMaxValue != null && max < float.MaxValue;
            bool hasLowerBound = up.GetMinValue != null && min > float.MinValue;

            if (!isDecreasing)
            {
                if (!hasUpperBound) return desiredAmount; // no cap
                double stepsD = Math.Floor(((double)max - (double)current) / (double)step + 1e-9);
                if (stepsD < 0) stepsD = 0;
                if (stepsD > int.MaxValue) stepsD = int.MaxValue;
                capSteps = (int)stepsD;
            }
            else
            {
                if (!hasLowerBound) return desiredAmount; // no cap
                double stepsD = Math.Floor(((double)current - (double)min) / (double)step + 1e-9);
                if (stepsD < 0) stepsD = 0;
                if (stepsD > int.MaxValue) stepsD = int.MaxValue;
                capSteps = (int)stepsD;
            }


            return Mathf.Min(desiredAmount, capSteps);
        }

        private void GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount, Currency currencyUsed = Currency.Scraps)
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

            // First cap by money (legacy), then by stat caps.
            int moneyBound = GetMaxAmount(baseCost, multiplier, level, currencyUsed);
            outAmount = (inAmount == 9999)
                ? (moneyBound == 0 ? 1 : moneyBound)
                : inAmount;

            outAmount = CapAmountByStatLimit(stats, type, outAmount);

            cost = (outAmount > 0)
                ? Mathf.Floor(RecursiveHybridCost(baseCost, level, outAmount))
                : 0f;

            // Apply prestige discount
            if (PrestigeManager.Instance != null)
                cost *= PrestigeManager.Instance.GetUpgradeCostMultiplier(type);

            cost = Mathf.Max(1f, cost);
        }

        private float GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, Currency currencyUsed = Currency.Scraps)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int maxAmount = (inAmount == 9999)
                ? GetMaxAmount(baseCost, multiplier, level, currencyUsed)
                : inAmount;

            maxAmount = CapAmountByStatLimit(stats, type, maxAmount);

            float c = (maxAmount > 0)
                ? Mathf.Floor(RecursiveHybridCost(baseCost, level, maxAmount))
                : 0f;

            if (PrestigeManager.Instance != null)
                c *= PrestigeManager.Instance.GetUpgradeCostMultiplier(type);

            return Mathf.Max(1f, c);
        }

        private void GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount, Currency currencyUsed = Currency.Scraps)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
            {
                cost = 0f;
                outAmount = 0;
                Debug.LogError($"TurretUpgradeManager: Upgrade type {type} not found.");
                return;
            }

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int moneyBound = GetMaxAmount(baseCost, multiplier, level, currencyUsed);
            outAmount = (inAmount == 9999)
                ? (moneyBound == 0 ? 1 : moneyBound)
                : inAmount;
            outAmount = CapAmountByStatLimit(stats, type, outAmount);
            cost = (outAmount > 0)
                ? Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, outAmount))
                : 0f;

            if (PrestigeManager.Instance != null)
                cost *= PrestigeManager.Instance.GetUpgradeCostMultiplier(type);

            cost = Mathf.Max(1f, cost);
        }

        private float GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, Currency currencyUsed = Currency.Scraps)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int maxAmount = (inAmount == 9999)
                ? GetMaxAmount(baseCost, multiplier, level, currencyUsed)
                : inAmount;

            maxAmount = CapAmountByStatLimit(stats, type, maxAmount);

            float c = (maxAmount > 0)
                ? Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, maxAmount))
                : 0f;

            if (PrestigeManager.Instance != null)
                c *= PrestigeManager.Instance.GetUpgradeCostMultiplier(type);

            return Mathf.Max(1f, c);
        }


        /* Old Cost Calculation Methods
        private void GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount, Currency currencyUsed = Currency.Scraps)
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
            float maxLevel = upgrade.GetMaxValue(stats);
            int maxAmount = GetMaxAmount(baseCost, multiplier, level, currencyUsed);

            outAmount = inAmount == 9999
                ? maxAmount == 0
                    ? 1
                    : maxAmount
                : inAmount;

            if (level + outAmount > maxLevel)
                outAmount = (int)maxLevel - level;

            if (level < outAmount)
                cost = Mathf.Floor(RecursiveHybridCost(baseCost, level, outAmount));

            cost = Mathf.Floor(baseCost * Mathf.Pow(exponentialPower, level));
        }

        private float GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, Currency currencyUsed = Currency.Scraps)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);
            float maxLevel = upgrade.GetMaxValue(stats);

            int maxAmount = inAmount == 9999
                ? GetMaxAmount(baseCost, multiplier, level, currencyUsed)
                : inAmount;

            if (level + maxAmount > maxLevel)
                maxAmount = (int)maxLevel - level;

            return level < maxLevel
                ? Mathf.Floor(RecursiveHybridCost(baseCost, level, maxAmount))
                : baseCost * Mathf.Pow(exponentialPower, level);
        }

        private void GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount, Currency currencyUsed = Currency.Scraps)
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
            int maxAmount = GetMaxAmount(baseCost, multiplier, level, currencyUsed);

            outAmount = inAmount == 9999
                ? maxAmount == 0
                    ? 1
                    : maxAmount
                : inAmount;

            cost = Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, outAmount));
        }

        private float GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, Currency currencyUsed = Currency.Scraps)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int maxAmount = inAmount == 9999
                ? GetMaxAmount(baseCost, multiplier, level, currencyUsed)
                : inAmount;

            return Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, maxAmount));
        }
        */
        private float RecursiveHybridCost(float baseCost, int level, int amount)
        {
            if (amount <= 0)
                return 0f;

            float cost = baseCost * (1f + level * level * quadraticFactor);

            return cost + RecursiveHybridCost(baseCost, level + 1, amount - 1);
        }

        private float RecursiveExponentialCost(float baseCost, float multiplier, int level, int amount)
        {
            if (amount <= 0)
                return 0f;

            float cost = baseCost * Mathf.Pow(multiplier, level);

            return cost + RecursiveExponentialCost(baseCost, multiplier, level + 1, amount - 1);
        }

        private static readonly HashSet<TurretUpgradeType> _hybridTypes = new()
        {
            TurretUpgradeType.ExplosionRadius,
            TurretUpgradeType.SplashDamage,
            TurretUpgradeType.PierceChance,
            TurretUpgradeType.PierceDamageFalloff,
            TurretUpgradeType.PelletCount,
            TurretUpgradeType.DamageFalloffOverDistance,
            TurretUpgradeType.KnockbackStrength,
            TurretUpgradeType.PercentBonusDamagePerSec,
            TurretUpgradeType.SlowEffect,
        };

        public int GetEffectiveBuyAmount(TurretStatsInstance stats, TurretUpgradeType type, int desiredAmount, Currency currencyUsed, out float cost)
        {
            cost = 0f;

            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || upgrade == null || stats == null)
                return 0;

            // If already capped, bail early
            if (upgrade.GetMaxValue != null && upgrade.GetCurrentValue(stats) >= upgrade.GetMaxValue(stats))
                return 0;

            // Determine effective steps (respect stat caps)
            int effective = desiredAmount;
            effective = CapAmountByStatLimit(stats, type, effective);

            // Enforce "minimum 1" only if we can actually change the stat.
            if (effective <= 0)
                return 0;

            if (_hybridTypes.Contains(type))
            {
                GetHybridCost(stats, type, effective, out cost, out effective, currencyUsed);
            }
            else
            {
                GetExponentialCost(stats, type, effective, out cost, out effective, currencyUsed);
            }

            // After cost calculation, effective may still be 0
            if (effective <= 0)
            {
                cost = 0f;
                return 0;
            }

            cost = Mathf.Max(1f, cost);
            return effective;
        }

        public void UpdateUpgradeDisplay(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || turret == null)
                return;

            int amount = MultipleBuyOption.Instance.GetBuyAmount();

            (string value, string bonus, string cost, string count) = upgrade.GetDisplayStrings(turret, amount);

            button.UpdateStats(value, bonus, cost, count);
        }

        /// <summary>
        /// Applies an upgrade to a provided stats instance (caller should pass a CLONE).
        /// Used by UI to simulate the post-upgrade effective value without mutating the real turret.
        /// </summary>
        public void ApplyUpgradeToStats(TurretStatsInstance stats, TurretUpgradeType type, int amount)
        {
            if (stats == null) return;
            if (amount <= 0) return;

            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || upgrade == null)
                return;

            upgrade.UpgradeTurret?.Invoke(stats, amount);
        }


        public List<TurretUpgradeType> GetAvailableUpgrades(TurretStatsInstance stats)
        {
            var list = new List<TurretUpgradeType>();

            foreach (var kvp in _turretUpgrades)
            {
                var type = kvp.Key;
                var up = kvp.Value;


                // Must exist and have a usable step
                if (up.GetUpgradeAmount == null || up.GetCurrentValue == null)
                    continue;

                float step = Mathf.Abs(up.GetUpgradeAmount(stats));

                if (step <= 0f) continue;

                float cur = up.GetCurrentValue(stats);
                float min = up.GetMinValue != null ? up.GetMinValue(stats) : float.NegativeInfinity;
                float max = up.GetMaxValue != null ? up.GetMaxValue(stats) : float.PositiveInfinity;

                // Heuristic: treat as decreasing if applying an upgrade lowers the current value
                // We detect via the delegate we already set in the map:
                bool isDecreasing = false;
                // If you maintain an explicit set for decreasing types, you can replace this with a HashSet check.
                switch (type)
                {
                    case TurretUpgradeType.PierceDamageFalloff:
                    case TurretUpgradeType.DamageFalloffOverDistance:
                    case TurretUpgradeType.BounceDelay:
                    case TurretUpgradeType.BounceDamagePct:
                    case TurretUpgradeType.ExplosionDelay:
                        isDecreasing = true; break;
                }

                bool hasHeadroom;
                if (!isDecreasing)
                    hasHeadroom = cur + step <= max - 1e-6f;
                else
                    hasHeadroom = cur - step >= min + 1e-6f;
                if (hasHeadroom)
                    list.Add(type);
            }

            // Optional: sort for consistent ordering (by enum)
            list.Sort((a, b) => a.CompareTo(b));
            return list;
        }

    }

    public enum TurretUpgradeType
    {
        Damage,
        FireRate,
        RotationSpeed,
        Range,
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
        KnockbackStrength,

        // Bounce Pattern
        BounceCount,
        BounceRange,
        BounceDelay,
        BounceDamagePct,   // % damage lost per bounce (we reduce it with upgrades)

        // Cone AOE
        ConeAngle,

        // Delayed AOE
        ExplosionDelay,    // we reduce delay

        // Trap Pattern
        AheadDistance,
        MaxTrapsActive,

        // Damage/Defense Effects
        ArmorPenetration   // percent, 0..100
    }
}
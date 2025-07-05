using Assets.Scripts.Systems.Audio;
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
                    UpgradeTurret = (t, a) =>
                    {
                        t.DamageLevel += a;
                        float exponent = Mathf.Pow(t.DamageUpgradeAmount, t.DamageLevel);

                        t.Damage = t.BaseDamage * exponent + t.DamageLevel;
                    },
                    GetLevel = t => t.DamageLevel,
                    GetBaseStat = t => t.BaseDamage,
                    GetBaseCost = t => t.DamageUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageUpgradeAmount,
                    GetCostMultiplier = t => t.DamageCostExponentialMultiplier,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.Damage, a),
                    //GetAmount = t => GetMaxAmount(t.DamageUpgradeBaseCost, t.DamageCostExponentialMultiplier, t.DamageLevel),
                    GetDisplayStrings = (t, a) =>
                        {
                            float currentDamage = t.Damage;
                            float currentLevel = t.DamageLevel;
                            float newLevel = currentLevel + a;

                            float projectedExponent = Mathf.Pow(t.DamageUpgradeAmount, newLevel);
                            float projectedDamage = t.BaseDamage * projectedExponent + newLevel;

                            float bonus = projectedDamage - currentDamage;

                            GetExponentialCost(t, TurretUpgradeType.Damage, a, out float cost, out int amount);

                            return (UIManager.AbbreviateNumber(currentDamage),
                                $"+{UIManager.AbbreviateNumber(bonus)}",
                                $"⚙{UIManager.AbbreviateNumber(cost)}",
                                $"{amount}X");
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.FireRate, a),
                    //GetAmount = t => GetMaxAmount(t.FireRateUpgradeBaseCost, t.FireRateCostExponentialMultiplier, t.FireRateLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFireRate = t.FireRate;
                        float bonusFireRate = GetBonusAmount(t, TurretUpgradeType.FireRate);
                        GetExponentialCost(t, TurretUpgradeType.FireRate, a, out float cost, out int amount);

                        return (
                            $"{currentFireRate:F2}/s",
                            $"+{bonusFireRate:F2}/s",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                        t.CriticalChance = Mathf.Min(
                        t.CriticalChance + (t.CriticalChanceUpgradeAmount * a),
                        t.MaxCriticalChance
                        );

                    },
                    GetLevel = t => t.CriticalChanceLevel,
                    GetBaseStat = t => t.BaseCritChance,
                    GetBaseCost = t => t.CriticalChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.CriticalChanceUpgradeAmount,
                    GetCostMultiplier = t => t.CriticalChanceCostExponentialMultiplier,
                    GetMaxValue = t => t.MaxCriticalChance,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.CriticalChance, a),
                    //GetAmount = t => GetMaxAmount(t.CriticalChanceUpgradeBaseCost, t.CriticalChanceCostExponentialMultiplier, t.CriticalChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.CriticalChance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.CriticalChance);
                        GetExponentialCost(t, TurretUpgradeType.CriticalChance, a, out float cost, out int amount);

                        if (current >= 50f)
                            return ($"{(int)current}%", "Max", "", "0X");

                        return (
                            $"{(int)current}%",
                            $"+{(int)bonus}%",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    GetCost = (t, a) => GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a),
                    //GetAmount = t => GetMaxAmount(t.CriticalDamageMultiplierUpgradeBaseCost, t.CriticalDamageMultiplier, t.CriticalDamageMultiplierLevel),
                    GetDisplayStrings = (t, a) =>
                {
                    float current = t.CriticalDamageMultiplier;
                    float bonus = GetBonusAmount(t, TurretUpgradeType.CriticalDamageMultiplier);
                    GetExponentialCost(t, TurretUpgradeType.CriticalDamageMultiplier, a, out float cost, out int amount);

                    return (
                        $"{(int)current}%",
                        $"+{(int)bonus}%",
                        $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                        t.ExplosionRadius = Mathf.Min(t.ExplosionRadius + (t.ExplosionRadiusUpgradeAmount * a), t.MaxExplosionRadius);
                    },

                    GetLevel = t => t.ExplosionRadiusLevel,
                    GetBaseStat = t => t.ExplosionRadius,
                    GetBaseCost = t => t.ExplosionRadiusUpgradeBaseCost,
                    GetUpgradeAmount = t => t.ExplosionRadiusUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => t.MaxExplosionRadius,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.ExplosionRadius, a),
                    //GetAmount = t => GetMaxAmount(t.ExplosionRadiusUpgradeBaseCost, 1.1f, t.ExplosionRadiusLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.ExplosionRadius;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.ExplosionRadius);
                        GetHybridCost(t, TurretUpgradeType.ExplosionRadius, a, out float cost, out int amount);

                        if (t.ExplosionRadius >= 5f)
                            return ($"{current:F1}", "Max", "", "0X");

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.SplashDamage, a),
                    //GetAmount = t => GetMaxAmount(t.SplashDamageUpgradeBaseCost, exponentialPower, t.SplashDamageLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SplashDamage;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.SplashDamage);
                        GetHybridCost(t, TurretUpgradeType.SplashDamage, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                        t.PierceChance = Mathf.Min(t.PierceChance + (t.PierceChanceUpgradeAmount * a), t.MaxPierceChance);
                    },
                    GetLevel = t => t.PierceChanceLevel,
                    GetBaseStat = t => t.PierceChance,
                    GetBaseCost = t => t.PierceChanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceChanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => t.MaxPierceChance,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.PierceChance, a),
                    //GetAmount = t => GetMaxAmount(t.PierceChanceUpgradeBaseCost, exponentialPower, t.PierceChanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PierceChance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PierceChance);
                        GetHybridCost(t, TurretUpgradeType.PierceChance, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    },
                    GetLevel = t => t.PierceDamageFalloffLevel,
                    GetBaseStat = t => t.PierceDamageFalloff,
                    GetBaseCost = t => t.PierceDamageFalloffUpgradeBaseCost,
                    GetUpgradeAmount = t => t.PierceDamageFalloffUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.PierceDamageFalloff, a),
                    //GetAmount = t => GetMaxAmount(t.PierceDamageFalloffUpgradeBaseCost, exponentialPower, t.PierceDamageFalloffLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float currentFalloff = t.PierceDamageFalloff;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PierceDamageFalloff);
                        GetHybridCost(t, TurretUpgradeType.PierceDamageFalloff, a, out float cost, out int amount);

                        return (
                            $"{currentFalloff:F1}%",
                            $"+{bonus:F1}%",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.PelletCount, a),
                    //GetAmount = t => GetMaxAmount(t.PelletCountUpgradeBaseCost, exponentialPower, t.PelletCountLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PelletCount;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PelletCount);
                        GetHybridCost(t, TurretUpgradeType.PelletCount, a, out float cost, out int amount);

                        return (
                            UIManager.AbbreviateNumber(current),
                            $"+{UIManager.AbbreviateNumber(bonus)}",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                        t.DamageFalloffOverDistance = Mathf.Max(t.DamageFalloffOverDistance - (t.DamageFalloffOverDistanceUpgradeAmount * a), t.MinDamageFalloff);
                    },
                    GetLevel = t => t.DamageFalloffOverDistanceLevel,
                    GetBaseStat = t => t.DamageFalloffOverDistance,
                    GetBaseCost = t => t.DamageFalloffOverDistanceUpgradeBaseCost,
                    GetUpgradeAmount = t => t.DamageFalloffOverDistanceUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => float.MaxValue,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.DamageFalloffOverDistance, a),
                    //GetAmount = t => GetMaxAmount(t.DamageFalloffOverDistanceUpgradeBaseCost, exponentialPower, t.DamageFalloffOverDistanceLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.DamageFalloffOverDistance;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.DamageFalloffOverDistance);
                        GetHybridCost(t, TurretUpgradeType.DamageFalloffOverDistance, a, out float cost, out int amount);

                        if (current <= 0f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                                $"{current:F1}%",
                                $"-{bonus:F1}%",
                                $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.KnockbackStrength, a),
                    //GetAmount = t => GetMaxAmount(t.KnockbackStrengthUpgradeBaseCost, t.KnockbackStrengthCostExponentialMultiplier, t.KnockbackStrengthLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.KnockbackStrength;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.KnockbackStrength);
                        GetHybridCost(t, TurretUpgradeType.KnockbackStrength, a, out float cost, out int amount);

                        return (
                            $"{current:F1}",
                            $"+{bonus:F1}",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a),
                    //GetAmount = t => GetMaxAmount(t.PercentBonusDamagePerSecUpgradeBaseCost, exponentialPower, t.PercentBonusDamagePerSecLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.PercentBonusDamagePerSec;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.PercentBonusDamagePerSec);
                        GetHybridCost(t, TurretUpgradeType.PercentBonusDamagePerSec, a, out float cost, out int amount);

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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
                        t.SlowEffect = Mathf.Min(t.SlowEffect + (t.SlowEffectUpgradeAmount * a), t.MaxSlowEffect);
                    },
                    GetLevel = t => t.SlowEffectLevel,
                    GetBaseStat = t => t.SlowEffect,
                    GetBaseCost = t => t.SlowEffectUpgradeBaseCost,
                    GetUpgradeAmount = t => t.SlowEffectUpgradeAmount,
                    GetCostMultiplier = t => 0f,
                    GetMaxValue = t => t.MaxSlowEffect,
                    GetMinValue = t => 0f,
                    GetCost = (t, a) => GetHybridCost(t, TurretUpgradeType.SlowEffect, a),
                    //GetAmount = t => GetMaxAmount(t.SlowEffectUpgradeBaseCost, exponentialPower, t.SlowEffectLevel),
                    GetDisplayStrings = (t, a) =>
                    {
                        float current = t.SlowEffect;
                        float bonus = GetBonusAmount(t, TurretUpgradeType.SlowEffect);
                        GetHybridCost(t, TurretUpgradeType.SlowEffect, a, out float cost, out int amount);

                        if (current >= 100f)
                            return ($"{current:F1}%", "Max", "", "0X");

                        return (
                            $"{current:F1}%",
                            $"+{bonus:F1}%",
                            $"⚙{UIManager.AbbreviateNumber(cost)}",
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

            float current = upgrade.GetCurrentValue(turret);
            float max = upgrade.GetMaxValue?.Invoke(turret) ?? float.MaxValue;
            float step = upgrade.GetUpgradeAmount(turret);

            int allowedAmount = amount;

            // Handle upgrades that increase (e.g. damage, crit) or decrease (e.g. falloff)
            bool increasing = step >= 0;
            while (allowedAmount > 0)
            {
                float projected = increasing
                    ? current + (step * allowedAmount)
                    : current - (Mathf.Abs(step) * allowedAmount);

                if ((increasing && projected > max) || (!increasing && projected < max))
                    allowedAmount--;
                else
                    break;
            }

            if (allowedAmount <= 0)
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            // Recalculate actual stat change with clamped amount
            float projectedValue = upgrade.GetCurrentValue(turret);
            float change = step * allowedAmount;
            float targetValue = increasing
                ? Mathf.Min(projectedValue + change, max)
                : Mathf.Max(projectedValue - Mathf.Abs(change), max);

            // If the projected value is the same as current, skip it
            if (Mathf.Approximately(projectedValue, targetValue))
            {
                UpdateUpgradeDisplay(turret, type, button);
                return;
            }

            float cost = upgrade.GetCost(turret, allowedAmount);

            if (TrySpend(cost))
            {
                upgrade.UpgradeTurret(turret, allowedAmount);
                AudioManager.Instance.Play("Upgrade");
                button._baseTurret.UpdateTurretAppearance();
                UpdateUpgradeDisplay(turret, type, button);
                button.UpdateInteractableState();
                OnAnyTurretUpgraded?.Invoke();
                AnimateBuyButtonClick(button.GetComponent<RectTransform>());
            }
        }

        private int ClampUpgradeAmountToMax(TurretStatsInstance stats, TurretUpgrade upgrade, int requestedAmount)
        {
            float current = upgrade.GetCurrentValue(stats);
            float max = upgrade.GetMaxValue?.Invoke(stats) ?? float.MaxValue;
            float step = upgrade.GetUpgradeAmount(stats);

            int allowed = requestedAmount;
            bool increasing = step >= 0;

            while (allowed > 0)
            {
                float projected = increasing
                    ? current + (step * allowed)
                    : current - (Mathf.Abs(step) * allowed);

                if ((increasing && projected > max) || (!increasing && projected < max))
                    allowed--;
                else
                    break;
            }

            return Mathf.Max(allowed, 0);
        }

        public void AnimateBuyButtonClick(RectTransform button)
        {
            // Cancel any ongoing tweens on this button
            button.DOKill();

            // Scale punch: 1 - 1.15 - 1
            button.DOScale(Vector3.one * 1.15f, 0.1f)
                .SetEase(Ease.OutBack)
                .OnComplete(() => {
                    button.DOScale(Vector3.one, 0.1f).SetEase(Ease.InOutSine);
                });
        }

        private bool TrySpend(float cost) => GameManager.Instance.TrySpend(cost);

        public float GetTurretUpgradeCost(TurretStatsInstance turret, TurretUpgradeType type, int amount) =>
            !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0f : upgrade.GetCost(turret, amount);

        // Used to show Max Amount of upgrades available
        //public int GetTurretAvailableUpgradeAmount(TurretStatsInstance turret, TurretUpgradeType type) =>
        //    !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) ? 0 : upgrade.GetAmount(turret);

        private int GetMaxAmount(float baseCost, float multiplier, int currentLevel)
        {
            int amount = 0;
            float totalCost = 0f;
            float money = GameManager.Instance.Money;

            //while (true)
            //{
            //    float cost = baseCost * Mathf.Pow(multiplier, currentLevel + amount);
            //    if (Mathf.Floor(totalCost + cost) > money)
            //        break;
            //    totalCost += cost;
            //    amount++;
            //}

            return amount;
        }

        private float GetBonusAmount(TurretStatsInstance stats, TurretUpgradeType type)
        {
            int amount = MultipleBuyOption.Instance.GetBuyAmount();

            float upgradeAmount = !_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade)
                ? 0f
                : upgrade.GetUpgradeAmount(stats);

            if (upgrade == null)
                return 1f;

            //if (amount == 9999)
            //    amount = upgrade.GetAmount(stats) == 0
            //        ? 1
            //        : upgrade.GetAmount(stats);

            return upgradeAmount * amount;
        }

        private int ClampPreviewAmount(TurretStatsInstance stats, TurretUpgrade upgrade, int requestedAmount)
        {
            float current = upgrade.GetCurrentValue(stats);
            float max = upgrade.GetMaxValue?.Invoke(stats) ?? float.MaxValue;
            float step = upgrade.GetUpgradeAmount(stats);

            int allowed = requestedAmount;
            bool increasing = step >= 0;

            while (allowed > 0)
            {
                float projected = increasing
                    ? current + (step * allowed)
                    : current - (Mathf.Abs(step) * allowed);

                if ((increasing && projected > max) || (!increasing && projected < max))
                    allowed--;
                else
                    break;
            }

            return Mathf.Max(allowed, 0);
        }


        private void GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount, out float cost, out int outAmount)
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
            int maxAmount = GetMaxAmount(baseCost, multiplier, level);

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

        private float GetHybridCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);
            float maxLevel = upgrade.GetMaxValue(stats);

            int maxAmount = inAmount == 9999
                ? GetMaxAmount(baseCost, multiplier, level)
                : inAmount;

            if (level + maxAmount > maxLevel)
                maxAmount = (int)maxLevel - level;

            return level < maxLevel
                ? Mathf.Floor(RecursiveHybridCost(baseCost, level, maxAmount))
                : baseCost * Mathf.Pow(exponentialPower, level);
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

        private float GetExponentialCost(TurretStatsInstance stats, TurretUpgradeType type, int inAmount)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade))
                return 0f;

            int level = upgrade.GetLevel(stats);
            float baseCost = upgrade.GetBaseCost(stats);
            float multiplier = upgrade.GetCostMultiplier(stats);

            int maxAmount = inAmount == 9999
                ? GetMaxAmount(baseCost, multiplier, level)
                : inAmount;

            return Mathf.Floor(RecursiveExponentialCost(baseCost, multiplier, level, maxAmount));
        }

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

        public void UpdateUpgradeDisplay(TurretStatsInstance turret, TurretUpgradeType type, TurretUpgradeButton button)
        {
            if (!_turretUpgrades.TryGetValue(type, out TurretUpgrade upgrade) || turret == null)
                return;

            int requested = MultipleBuyOption.Instance.GetBuyAmount();
            int rawAmount = MultipleBuyOption.Instance.GetBuyAmount();
            int clampedAmount = ClampPreviewAmount(turret, upgrade, rawAmount);
            (string value, string bonus, string cost, string count) = upgrade.GetDisplayStrings(turret, clampedAmount);
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